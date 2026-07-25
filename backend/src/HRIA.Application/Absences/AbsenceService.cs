using HRIA.Application.Absences.Dtos;
using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Common.Models;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Absences;

public class AbsenceService : IAbsenceService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkingDayCalculator _workingDays;

    public AbsenceService(IAppDbContext db, ICurrentUser currentUser, IWorkingDayCalculator workingDays)
    {
        _db = db;
        _currentUser = currentUser;
        _workingDays = workingDays;
    }

    private bool IsAdmin => _currentUser.Role == Role.Admin;

    public async Task<IReadOnlyList<AbsenceTypeDto>> GetTypesAsync(CancellationToken ct = default) =>
        await _db.AbsenceTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new AbsenceTypeDto(t.Id, t.Code, t.Name, t.ConsumesVacationBalance, t.RequiresApproval, t.ColorHex))
            .ToListAsync(ct);

    // ------------------------------------------------------------------
    // Consulta
    // ------------------------------------------------------------------

    public async Task<PagedResult<AbsenceRequestDto>> GetPagedAsync(AbsenceQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var employeeId = ResolveEmployeeFilter(query.EmployeeId);

        var q = _db.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType)
            // El empleado del usuario que resolvió hace falta para mostrar su
            // nombre; sin este ThenInclude se caería al correo electrónico.
            .Include(a => a.DecidedByUser)!
                .ThenInclude(u => u!.Employee)
            .AsQueryable();

        if (employeeId is > 0) q = q.Where(a => a.EmployeeId == employeeId);
        if (query.AbsenceTypeId is > 0) q = q.Where(a => a.AbsenceTypeId == query.AbsenceTypeId);
        if (query.Status is not null) q = q.Where(a => a.Status == query.Status);
        if (query.From is not null) q = q.Where(a => a.EndDate >= query.From);
        if (query.To is not null) q = q.Where(a => a.StartDate <= query.To);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(a => a.StartDate).ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AbsenceRequestDto>(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<AbsenceRequestDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct);

        if (!IsAdmin && request.EmployeeId != _currentUser.EmployeeId)
            throw AppException.Forbidden();

        return Map(request);
    }

    // ------------------------------------------------------------------
    // Alta y resolución
    // ------------------------------------------------------------------

    public async Task<AbsenceRequestDto> CreateAsync(CreateAbsenceRequest request, CancellationToken ct = default)
    {
        // El empleado solo puede solicitar para sí mismo: el id se toma del
        // token y jamás del cuerpo de la petición.
        var employeeId = IsAdmin
            ? request.EmployeeId ?? _currentUser.EmployeeId ?? throw AppException.BadRequest("Indica el empleado.")
            : _currentUser.EmployeeId ?? throw AppException.Forbidden();

        if (request.EndDate < request.StartDate)
            throw AppException.BadRequest("La fecha de fin no puede ser anterior a la de inicio.");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw AppException.BadRequest("El empleado indicado no existe.");

        if (!employee.IsActive)
            throw AppException.BadRequest("El empleado está dado de baja.");

        var type = await _db.AbsenceTypes.FirstOrDefaultAsync(t => t.Id == request.AbsenceTypeId && t.IsActive, ct)
            ?? throw AppException.BadRequest("El tipo de ausencia indicado no existe.");

        // Solapamiento con otras solicitudes vivas del mismo empleado.
        var overlapping = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => a.EmployeeId == employeeId
                        && (a.Status == AbsenceStatus.Pending || a.Status == AbsenceStatus.Approved)
                        && a.StartDate <= request.EndDate
                        && request.StartDate <= a.EndDate)
            .FirstOrDefaultAsync(ct);

        if (overlapping is not null)
            throw AppException.Conflict(
                $"Ya hay una ausencia de tipo '{overlapping.AbsenceType!.Name}' entre " +
                $"{overlapping.StartDate:yyyy-MM-dd} y {overlapping.EndDate:yyyy-MM-dd}.");

        var workingDays = await _workingDays.CountAsync(employeeId, request.StartDate, request.EndDate, ct);

        if (workingDays <= 0)
            throw AppException.BadRequest(
                "El periodo indicado no contiene ningún día laborable para este empleado.");

        if (type.ConsumesVacationBalance)
        {
            // Una solicitud que cruza el fin de año repartiría días entre dos
            // saldos anuales distintos. En lugar de repartirlos con reglas
            // discutibles, se pide partirla en dos solicitudes.
            if (request.StartDate.Year != request.EndDate.Year)
                throw AppException.BadRequest(
                    "Las vacaciones no pueden abarcar dos años naturales. Divide la solicitud.");

            var balance = await GetBalanceInternalAsync(employeeId, request.StartDate.Year, ct);
            if (workingDays > balance.AvailableDays)
                throw AppException.Conflict(
                    $"Saldo insuficiente: quedan {balance.AvailableDays:0.##} día(s) disponibles y se solicitan {workingDays:0.##}.");
        }

        var now = DateTime.UtcNow;
        // Los tipos que no requieren aprobación (p. ej. una baja justificada)
        // nacen ya aprobados: obligar a un visto bueno no aporta nada.
        var autoApproved = !type.RequiresApproval;

        var absence = new AbsenceRequest
        {
            EmployeeId = employeeId,
            AbsenceTypeId = type.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDays = workingDays,
            Status = autoApproved ? AbsenceStatus.Approved : AbsenceStatus.Pending,
            Reason = request.Reason?.Trim(),
            RequestedAt = now,
            DecidedAt = autoApproved ? now : null,
            DecidedByUserId = autoApproved ? _currentUser.UserId : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.AbsenceRequests.Add(absence);
        Audit("CreateAbsenceRequest", employeeId.ToString(),
            $"{type.Name} del {request.StartDate:yyyy-MM-dd} al {request.EndDate:yyyy-MM-dd} ({workingDays:0.##} días).");
        await _db.SaveChangesAsync(ct);

        return Map(await LoadAsync(absence.Id, ct));
    }

    public async Task<AbsenceRequestDto> ApproveAsync(int id, DecideAbsenceRequest request, CancellationToken ct = default)
    {
        var absence = await LoadAsync(id, ct);
        EnsurePending(absence);

        // El saldo se vuelve a comprobar al aprobar: entre la solicitud y la
        // decisión pueden haberse aprobado otras o haber cambiado la asignación.
        if (absence.AbsenceType!.ConsumesVacationBalance)
        {
            var balance = await GetBalanceInternalAsync(absence.EmployeeId, absence.StartDate.Year, ct, excludeRequestId: absence.Id);
            if (absence.WorkingDays > balance.AvailableDays)
                throw AppException.Conflict(
                    $"Saldo insuficiente: quedan {balance.AvailableDays:0.##} día(s) y la solicitud consume {absence.WorkingDays:0.##}.");
        }

        Decide(absence, AbsenceStatus.Approved, request.Comment);
        Audit("ApproveAbsenceRequest", id.ToString(), $"Aprobada la ausencia {id}.");
        await _db.SaveChangesAsync(ct);

        return Map(await LoadAsync(id, ct));
    }

    public async Task<AbsenceRequestDto> RejectAsync(int id, DecideAbsenceRequest request, CancellationToken ct = default)
    {
        var absence = await LoadAsync(id, ct);
        EnsurePending(absence);

        Decide(absence, AbsenceStatus.Rejected, request.Comment);
        Audit("RejectAbsenceRequest", id.ToString(), $"Rechazada la ausencia {id}.");
        await _db.SaveChangesAsync(ct);

        return Map(await LoadAsync(id, ct));
    }

    public async Task<AbsenceRequestDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var absence = await LoadAsync(id, ct);

        if (!IsAdmin && absence.EmployeeId != _currentUser.EmployeeId)
            throw AppException.Forbidden();

        if (absence.Status != AbsenceStatus.Pending)
            throw AppException.Conflict("Solo se pueden retirar solicitudes pendientes.");

        absence.Status = AbsenceStatus.Cancelled;
        absence.UpdatedAt = DateTime.UtcNow;

        Audit("CancelAbsenceRequest", id.ToString(), $"Retirada la solicitud {id}.");
        await _db.SaveChangesAsync(ct);

        return Map(await LoadAsync(id, ct));
    }

    // ------------------------------------------------------------------
    // Vacaciones
    // ------------------------------------------------------------------

    public async Task<VacationBalanceDto> GetBalanceAsync(int employeeId, int year, CancellationToken ct = default)
    {
        if (!IsAdmin && employeeId != _currentUser.EmployeeId)
            throw AppException.Forbidden();

        return await GetBalanceInternalAsync(employeeId, year, ct);
    }

    public async Task<IReadOnlyList<VacationBalanceDto>> GetAllBalancesAsync(int year, CancellationToken ct = default)
    {
        var employees = await _db.Employees
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var result = new List<VacationBalanceDto>(employees.Count);
        foreach (var id in employees)
            result.Add(await GetBalanceInternalAsync(id, year, ct));

        return result;
    }

    public async Task<VacationBalanceDto> SetAllowanceAsync(SetVacationAllowanceRequest request, CancellationToken ct = default)
    {
        if (request.Days < 0)
            throw AppException.BadRequest("Los días no pueden ser negativos.");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw AppException.BadRequest("El empleado indicado no existe.");

        var allowance = await _db.VacationAllowances
            .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.Year == request.Year, ct);

        var now = DateTime.UtcNow;
        if (allowance is null)
        {
            allowance = new VacationAllowance
            {
                EmployeeId = request.EmployeeId,
                Year = request.Year,
                Days = request.Days,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.VacationAllowances.Add(allowance);
        }
        else
        {
            allowance.Days = request.Days;
            allowance.UpdatedAt = now;
        }

        Audit("SetVacationAllowance", request.EmployeeId.ToString(),
            $"{request.Days:0.##} días de vacaciones para {request.Year} ({employee.FullName}).");
        await _db.SaveChangesAsync(ct);

        return await GetBalanceInternalAsync(request.EmployeeId, request.Year, ct);
    }

    public async Task<VacationCalendarDto> GetVacationCalendarAsync(int year, CancellationToken ct = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        var employees = await _db.Employees
            .Include(e => e.Department)
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(ct);

        // Se muestran aprobadas y pendientes: el administrador necesita ver lo
        // que está por decidir para detectar solapamientos entre compañeros
        // antes de aprobar.
        var absences = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => (a.Status == AbsenceStatus.Approved || a.Status == AbsenceStatus.Pending)
                        && a.StartDate <= to && from <= a.EndDate)
            .OrderBy(a => a.StartDate)
            .ToListAsync(ct);

        var byEmployee = absences.GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = employees.Select(e => new EmployeeYearAbsencesDto(
            e.Id,
            e.FullName,
            e.Department?.Name ?? string.Empty,
            byEmployee.TryGetValue(e.Id, out var list)
                ? list.Select(a => new CalendarAbsenceDto(
                    a.Id,
                    a.StartDate,
                    a.EndDate,
                    a.AbsenceType!.Name,
                    a.AbsenceType.Code,
                    a.AbsenceType.ColorHex,
                    a.Status,
                    a.WorkingDays)).ToList()
                : new List<CalendarAbsenceDto>())).ToList();

        return new VacationCalendarDto(year, rows);
    }

    // ------------------------------------------------------------------
    // Apoyo
    // ------------------------------------------------------------------

    private async Task<VacationBalanceDto> GetBalanceInternalAsync(int employeeId, int year, CancellationToken ct, int? excludeRequestId = null)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw AppException.NotFound("Empleado no encontrado.");

        var allowance = await _db.VacationAllowances
            .Where(a => a.EmployeeId == employeeId && a.Year == year)
            .Select(a => (decimal?)a.Days)
            .FirstOrDefaultAsync(ct) ?? 0m;

        // Se imputan al año de la fecha de inicio; las vacaciones no pueden
        // abarcar dos años naturales, así que no hay ambigüedad.
        var relevant = await _db.AbsenceRequests
            .Include(a => a.AbsenceType)
            .Where(a => a.EmployeeId == employeeId
                        && a.AbsenceType!.ConsumesVacationBalance
                        && a.StartDate.Year == year
                        && (excludeRequestId == null || a.Id != excludeRequestId))
            .Select(a => new { a.Status, a.WorkingDays })
            .ToListAsync(ct);

        var approved = relevant.Where(a => a.Status == AbsenceStatus.Approved).Sum(a => a.WorkingDays);
        var pending = relevant.Where(a => a.Status == AbsenceStatus.Pending).Sum(a => a.WorkingDays);

        return new VacationBalanceDto(
            employeeId,
            employee.FullName,
            year,
            allowance,
            approved,
            pending,
            allowance - approved - pending);
    }

    private int? ResolveEmployeeFilter(int? requested)
    {
        if (IsAdmin) return requested;

        if (requested is not null && requested != _currentUser.EmployeeId)
            throw AppException.Forbidden();

        return _currentUser.EmployeeId;
    }

    private async Task<AbsenceRequest> LoadAsync(int id, CancellationToken ct) =>
        await _db.AbsenceRequests
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType)
            .Include(a => a.DecidedByUser)!
                .ThenInclude(u => u!.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
        ?? throw AppException.NotFound("Solicitud no encontrada.");

    private static void EnsurePending(AbsenceRequest absence)
    {
        if (absence.Status != AbsenceStatus.Pending)
            throw AppException.Conflict($"La solicitud ya está en estado {absence.Status}.");
    }

    private void Decide(AbsenceRequest absence, AbsenceStatus status, string? comment)
    {
        absence.Status = status;
        absence.DecidedAt = DateTime.UtcNow;
        absence.DecidedByUserId = _currentUser.UserId;
        absence.DecisionComment = comment?.Trim();
        absence.UpdatedAt = DateTime.UtcNow;
    }

    private static AbsenceRequestDto Map(AbsenceRequest a) => new(
        a.Id,
        a.EmployeeId,
        a.Employee is null ? string.Empty : a.Employee.FullName,
        a.AbsenceTypeId,
        a.AbsenceType?.Name ?? string.Empty,
        a.AbsenceType?.Code ?? string.Empty,
        a.AbsenceType?.ColorHex,
        a.StartDate,
        a.EndDate,
        a.WorkingDays,
        a.Status,
        a.Reason,
        a.RequestedAt,
        a.DecidedAt,
        a.DecidedByUser?.Employee?.FullName ?? a.DecidedByUser?.Email,
        a.DecisionComment);

    private void Audit(string action, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId ?? 0,
            Action = action,
            Entity = nameof(AbsenceRequest),
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }
}
