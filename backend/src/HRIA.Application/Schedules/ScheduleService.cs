using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Schedules.Dtos;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Schedules;

public class ScheduleService : IScheduleService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ScheduleService(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ------------------------------------------------------------------
    // Plantillas de horario
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<ScheduleListItemDto>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var q = _db.Schedules.Include(s => s.Slots).AsQueryable();
        if (!includeInactive)
            q = q.Where(s => s.IsActive);

        var schedules = await q.OrderBy(s => s.Name).ToListAsync(ct);
        var scheduleIds = schedules.Select(s => s.Id).ToList();

        // Empleados con la asignación vigente hoy, por horario.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assigned = await _db.ScheduleAssignments
            .Where(a => scheduleIds.Contains(a.ScheduleId)
                        && a.StartDate <= today
                        && (a.EndDate == null || today <= a.EndDate))
            .GroupBy(a => a.ScheduleId)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ScheduleId, x => x.Count, ct);

        return schedules.Select(s => new ScheduleListItemDto(
            s.Id,
            s.Name,
            s.Description,
            s.IsActive,
            s.WeeklyMinutes,
            s.Slots.Count,
            assigned.TryGetValue(s.Id, out var n) ? n : 0)).ToList();
    }

    public async Task<ScheduleDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.Schedules
            .Include(x => x.Slots)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw AppException.NotFound("Horario no encontrado.");

        return MapDetail(s);
    }

    public async Task<ScheduleDetailDto> CreateAsync(CreateScheduleRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();

        if (await _db.Schedules.AnyAsync(s => s.Name == name, ct))
            throw AppException.Conflict("Ya existe un horario con ese nombre.");

        var slots = BuildSlots(request.Slots);

        var now = DateTime.UtcNow;
        var schedule = new Schedule
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            Slots = slots
        };

        _db.Schedules.Add(schedule);
        Audit("CreateSchedule", nameof(Schedule), name, $"Alta de horario con {slots.Count} tramos.");
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(schedule.Id, ct);
    }

    public async Task<ScheduleDetailDto> UpdateAsync(int id, UpdateScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("Horario no encontrado.");

        var name = request.Name.Trim();

        if (await _db.Schedules.AnyAsync(s => s.Name == name && s.Id != id, ct))
            throw AppException.Conflict("Ya existe un horario con ese nombre.");

        var slots = BuildSlots(request.Slots);

        schedule.Name = name;
        schedule.Description = request.Description?.Trim();
        schedule.IsActive = request.IsActive;
        schedule.UpdatedAt = DateTime.UtcNow;

        // Los tramos se reemplazan en bloque: es más simple y predecible que
        // conciliar altas, bajas y modificaciones tramo a tramo.
        _db.ScheduleSlots.RemoveRange(schedule.Slots);
        schedule.Slots = slots;

        Audit("UpdateSchedule", nameof(Schedule), id.ToString(), $"Modificación de horario ({slots.Count} tramos).");
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw AppException.NotFound("Horario no encontrado.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var vigentes = await _db.ScheduleAssignments
            .CountAsync(a => a.ScheduleId == id
                             && (a.EndDate == null || today <= a.EndDate), ct);

        if (vigentes > 0)
            throw AppException.Conflict(
                $"No se puede desactivar: {vigentes} asignación(es) siguen vigentes. Finalízalas primero.");

        schedule.IsActive = false;
        schedule.UpdatedAt = DateTime.UtcNow;

        Audit("DeactivateSchedule", nameof(Schedule), id.ToString(), "Baja lógica de horario.");
        await _db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------
    // Asignaciones
    // ------------------------------------------------------------------

    public async Task<IReadOnlyList<ScheduleAssignmentDto>> GetAssignmentsAsync(int? employeeId, int? scheduleId, CancellationToken ct = default)
    {
        // Protección horizontal: un empleado solo ve sus propias asignaciones.
        if (_currentUser.Role == Role.Employee)
        {
            if (employeeId is not null && employeeId != _currentUser.EmployeeId)
                throw AppException.Forbidden();
            employeeId = _currentUser.EmployeeId;
        }

        var q = _db.ScheduleAssignments
            .Include(a => a.Schedule)
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId is > 0) q = q.Where(a => a.EmployeeId == employeeId);
        if (scheduleId is > 0) q = q.Where(a => a.ScheduleId == scheduleId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await q
            .OrderByDescending(a => a.StartDate)
            .Select(a => new ScheduleAssignmentDto(
                a.Id,
                a.ScheduleId,
                a.Schedule!.Name,
                a.EmployeeId,
                a.Employee!.FirstName + " " + a.Employee.LastName,
                a.StartDate,
                a.EndDate,
                a.StartDate <= today && (a.EndDate == null || today <= a.EndDate)))
            .ToListAsync(ct);
    }

    public async Task<ScheduleAssignmentDto> AssignAsync(CreateScheduleAssignmentRequest request, CancellationToken ct = default)
    {
        if (request.EndDate is not null && request.EndDate < request.StartDate)
            throw AppException.BadRequest("La fecha de fin no puede ser anterior a la de inicio.");

        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == request.ScheduleId, ct)
            ?? throw AppException.BadRequest("El horario indicado no existe.");

        if (!schedule.IsActive)
            throw AppException.BadRequest("No se puede asignar un horario desactivado.");

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw AppException.BadRequest("El empleado indicado no existe.");

        if (!employee.IsActive)
            throw AppException.BadRequest("No se puede asignar un horario a un empleado dado de baja.");

        await EnsureNoOverlapAsync(request.EmployeeId, request.StartDate, request.EndDate, null, ct);

        var now = DateTime.UtcNow;
        var assignment = new ScheduleAssignment
        {
            ScheduleId = request.ScheduleId,
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.ScheduleAssignments.Add(assignment);
        Audit("AssignSchedule", nameof(ScheduleAssignment), request.EmployeeId.ToString(),
            $"Horario '{schedule.Name}' asignado desde {request.StartDate:yyyy-MM-dd}.");
        await _db.SaveChangesAsync(ct);

        return await GetAssignmentAsync(assignment.Id, ct);
    }

    public async Task<ScheduleAssignmentDto> UpdateAssignmentAsync(int id, UpdateScheduleAssignmentRequest request, CancellationToken ct = default)
    {
        var assignment = await _db.ScheduleAssignments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("Asignación no encontrada.");

        if (request.EndDate is not null && request.EndDate < request.StartDate)
            throw AppException.BadRequest("La fecha de fin no puede ser anterior a la de inicio.");

        await EnsureNoOverlapAsync(assignment.EmployeeId, request.StartDate, request.EndDate, id, ct);

        assignment.StartDate = request.StartDate;
        assignment.EndDate = request.EndDate;
        assignment.UpdatedAt = DateTime.UtcNow;

        Audit("UpdateScheduleAssignment", nameof(ScheduleAssignment), id.ToString(), "Modificación de asignación de horario.");
        await _db.SaveChangesAsync(ct);

        return await GetAssignmentAsync(id, ct);
    }

    public async Task RemoveAssignmentAsync(int id, CancellationToken ct = default)
    {
        var assignment = await _db.ScheduleAssignments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("Asignación no encontrada.");

        _db.ScheduleAssignments.Remove(assignment);
        Audit("RemoveScheduleAssignment", nameof(ScheduleAssignment), id.ToString(), "Eliminación de asignación de horario.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ScheduleDetailDto?> GetEffectiveScheduleAsync(int employeeId, DateOnly date, CancellationToken ct = default)
    {
        if (_currentUser.Role == Role.Employee && _currentUser.EmployeeId != employeeId)
            throw AppException.Forbidden();

        var assignment = await _db.ScheduleAssignments
            .Include(a => a.Schedule)!
                .ThenInclude(s => s!.Slots)
            .Where(a => a.EmployeeId == employeeId
                        && a.StartDate <= date
                        && (a.EndDate == null || date <= a.EndDate))
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync(ct);

        return assignment?.Schedule is null ? null : MapDetail(assignment.Schedule);
    }

    // ------------------------------------------------------------------
    // Apoyo
    // ------------------------------------------------------------------

    /// <summary>
    /// Valida los tramos recibidos: fin posterior al inicio y sin solapamientos
    /// dentro del mismo día. El solapamiento no se puede expresar como
    /// restricción en base de datos, así que se comprueba aquí.
    /// </summary>
    private static List<ScheduleSlot> BuildSlots(IReadOnlyList<ScheduleSlotInput> inputs)
    {
        if (inputs is null || inputs.Count == 0)
            throw AppException.BadRequest("El horario debe tener al menos un tramo.");

        var slots = inputs
            .Select(i => new ScheduleSlot
            {
                DayOfWeek = i.DayOfWeek,
                StartTime = i.StartTime,
                EndTime = i.EndTime
            })
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .ToList();

        foreach (var slot in slots)
        {
            if (!Enum.IsDefined(slot.DayOfWeek))
                throw AppException.BadRequest("Día de la semana no válido.");

            if (slot.EndTime <= slot.StartTime)
                throw AppException.BadRequest(
                    $"El tramo de {Spanish(slot.DayOfWeek)} debe terminar después de empezar.");
        }

        for (var i = 1; i < slots.Count; i++)
        {
            if (slots[i].OverlapsWith(slots[i - 1]))
                throw AppException.BadRequest(
                    $"Los tramos de {Spanish(slots[i].DayOfWeek)} se solapan.");
        }

        return slots;
    }

    private async Task EnsureNoOverlapAsync(int employeeId, DateOnly start, DateOnly? end, int? excludeId, CancellationToken ct)
    {
        // Un empleado no puede tener dos horarios vigentes a la vez: si no,
        // no habría forma de saber cuál se le aplica en una fecha dada.
        var existing = await _db.ScheduleAssignments
            .Where(a => a.EmployeeId == employeeId && (excludeId == null || a.Id != excludeId))
            .Select(a => new { a.Id, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        var overlapping = existing.FirstOrDefault(a =>
            a.StartDate <= (end ?? DateOnly.MaxValue) && start <= (a.EndDate ?? DateOnly.MaxValue));

        if (overlapping is not null)
            throw AppException.Conflict(
                $"El empleado ya tiene un horario asignado que se solapa con ese periodo " +
                $"({overlapping.StartDate:yyyy-MM-dd} – {(overlapping.EndDate is null ? "indefinido" : overlapping.EndDate.Value.ToString("yyyy-MM-dd"))}).");
    }

    private async Task<ScheduleAssignmentDto> GetAssignmentAsync(int id, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.ScheduleAssignments
            .Include(a => a.Schedule)
            .Include(a => a.Employee)
            .Where(a => a.Id == id)
            .Select(a => new ScheduleAssignmentDto(
                a.Id,
                a.ScheduleId,
                a.Schedule!.Name,
                a.EmployeeId,
                a.Employee!.FirstName + " " + a.Employee.LastName,
                a.StartDate,
                a.EndDate,
                a.StartDate <= today && (a.EndDate == null || today <= a.EndDate)))
            .FirstAsync(ct);
    }

    private static ScheduleDetailDto MapDetail(Schedule s) => new(
        s.Id,
        s.Name,
        s.Description,
        s.IsActive,
        s.WeeklyMinutes,
        s.Slots
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .Select(x => new ScheduleSlotDto(x.Id, x.DayOfWeek, x.StartTime, x.EndTime, x.DurationMinutes))
            .ToList(),
        s.CreatedAt,
        s.UpdatedAt);

    private static string Spanish(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        _ => "domingo",
    };

    private void Audit(string action, string entity, string entityId, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUser.UserId ?? 0,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }
}
