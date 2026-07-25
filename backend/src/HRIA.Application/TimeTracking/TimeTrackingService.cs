using HRIA.Application.Common.Exceptions;
using HRIA.Application.Common.Interfaces;
using HRIA.Application.Schedules;
using HRIA.Application.TimeTracking.Dtos;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.TimeTracking;

public class TimeTrackingService : ITimeTrackingService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IExpectedMinutesCalculator _expectedMinutes;

    public TimeTrackingService(IAppDbContext db, ICurrentUser currentUser, IExpectedMinutesCalculator expectedMinutes)
    {
        _db = db;
        _currentUser = currentUser;
        _expectedMinutes = expectedMinutes;
    }

    public async Task<TimeStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var empId = RequireEmployee();
        await NormalizeStaleAsync(empId, ct);

        var open = await GetOpenWorkdayAsync(empId, ct);
        if (open is null)
            return new TimeStatusDto(TimeState.NotStarted, null);

        var state = open.HasOpenBreak ? TimeState.OnBreak : TimeState.Working;
        return new TimeStatusDto(state, await MapWithExpectedAsync(open, ct));
    }

    public async Task<TimeStatusDto> CheckInAsync(CancellationToken ct = default)
    {
        var empId = RequireEmployee();
        await NormalizeStaleAsync(empId, ct);

        if (await GetOpenWorkdayAsync(empId, ct) is not null)
            throw AppException.Conflict("Ya existe una jornada abierta."); // BR-01

        var now = DateTime.UtcNow;
        var workday = new Workday
        {
            EmployeeId = empId,
            Date = DateOnly.FromDateTime(now),
            CheckIn = now,
            CheckOut = null,
            Status = WorkdayStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Workdays.Add(workday);
        await _db.SaveChangesAsync(ct);

        return new TimeStatusDto(TimeState.Working, await MapWithExpectedAsync(workday, ct));
    }

    public async Task<TimeStatusDto> StartBreakAsync(CancellationToken ct = default)
    {
        var empId = RequireEmployee();
        var open = await GetOpenWorkdayAsync(empId, ct)
            ?? throw AppException.Conflict("No hay una jornada abierta."); // BR-02

        if (open.HasOpenBreak)
            throw AppException.Conflict("Ya hay un descanso en curso."); // BR-03

        var now = DateTime.UtcNow;
        open.Breaks.Add(new Break { StartTime = now });
        open.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return new TimeStatusDto(TimeState.OnBreak, await MapWithExpectedAsync(open, ct));
    }

    public async Task<TimeStatusDto> EndBreakAsync(CancellationToken ct = default)
    {
        var empId = RequireEmployee();
        var open = await GetOpenWorkdayAsync(empId, ct)
            ?? throw AppException.Conflict("No hay una jornada abierta.");

        var openBreak = open.Breaks.FirstOrDefault(b => b.EndTime is null)
            ?? throw AppException.Conflict("No hay un descanso en curso."); // BR-04

        var now = DateTime.UtcNow;
        openBreak.EndTime = now;
        open.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return new TimeStatusDto(TimeState.Working, await MapWithExpectedAsync(open, ct));
    }

    public async Task<WorkdayDto> CheckOutAsync(CancellationToken ct = default)
    {
        var empId = RequireEmployee();
        var open = await GetOpenWorkdayAsync(empId, ct)
            ?? throw AppException.Conflict("No hay una jornada abierta."); // BR-06

        if (open.HasOpenBreak)
            throw AppException.Conflict("Finaliza el descanso antes de registrar la salida."); // BR-05

        var now = DateTime.UtcNow;
        open.CheckOut = now;
        open.Status = WorkdayStatus.Completed; // BR-07: total se calcula al mapear
        open.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return await MapWithExpectedAsync(open, ct);
    }

    public async Task<IReadOnlyList<WorkdayDto>> GetWorkdaysAsync(WorkdayQuery query, CancellationToken ct = default)
    {
        var own = RequireEmployee();

        // Protección horizontal: el empleado solo ve sus jornadas; el administrador puede
        // filtrar por un empleado concreto o ver todas.
        int? targetEmployeeId;
        if (_currentUser.Role == Role.Admin)
            targetEmployeeId = query.EmployeeId; // null = todos
        else
            targetEmployeeId = own;

        var q = _db.Workdays.Include(w => w.Breaks).AsQueryable();

        if (targetEmployeeId is not null)
            q = q.Where(w => w.EmployeeId == targetEmployeeId);

        if (query.From is not null)
            q = q.Where(w => w.Date >= query.From);
        if (query.To is not null)
            q = q.Where(w => w.Date <= query.To);

        var list = await q
            .OrderByDescending(w => w.CheckIn)
            .Take(500)
            .ToListAsync(ct);

        return await WithExpectedAsync(list, ct);
    }

    private async Task<WorkdayDto> MapWithExpectedAsync(Workday workday, CancellationToken ct)
    {
        var dto = Map(workday);
        var expected = await _expectedMinutes.GetAsync(workday.EmployeeId, workday.Date, ct);
        return expected is null
            ? dto
            : dto with { ExpectedMinutes = expected, DeviationMinutes = dto.WorkedMinutes - expected.Value };
    }

    /// <summary>
    /// Añade a cada jornada los minutos previstos por el horario y la desviación.
    /// Se resuelve en una sola consulta para todo el conjunto, no una por jornada.
    /// </summary>
    private async Task<IReadOnlyList<WorkdayDto>> WithExpectedAsync(List<Workday> workdays, CancellationToken ct)
    {
        if (workdays.Count == 0) return Array.Empty<WorkdayDto>();

        var employeeIds = workdays.Select(w => w.EmployeeId).Distinct().ToList();
        var from = workdays.Min(w => w.Date);
        var to = workdays.Max(w => w.Date);

        var expected = await _expectedMinutes.GetAsync(employeeIds, from, to, ct);

        return workdays.Select(w =>
        {
            var dto = Map(w);
            return expected.TryGetValue((w.EmployeeId, w.Date), out var minutes)
                ? dto with { ExpectedMinutes = minutes, DeviationMinutes = dto.WorkedMinutes - minutes }
                : dto;
        }).ToList();
    }

    // --- Helpers ---

    private int RequireEmployee() =>
        _currentUser.EmployeeId ?? throw AppException.Unauthorized("Sesión no válida.");

    private Task<Workday?> GetOpenWorkdayAsync(int empId, CancellationToken ct) =>
        _db.Workdays
            .Include(w => w.Breaks)
            .FirstOrDefaultAsync(w => w.EmployeeId == empId
                                   && w.CheckOut == null
                                   && w.Status == WorkdayStatus.Open, ct);

    /// <summary>Marca como incompletas las jornadas abiertas de días anteriores (BR-08).</summary>
    private async Task NormalizeStaleAsync(int empId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stale = await _db.Workdays
            .Where(w => w.EmployeeId == empId
                     && w.CheckOut == null
                     && w.Status == WorkdayStatus.Open
                     && w.Date < today)
            .ToListAsync(ct);

        if (stale.Count == 0) return;

        foreach (var w in stale)
        {
            w.Status = WorkdayStatus.Incomplete;
            w.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static WorkdayDto Map(Workday w)
    {
        var now = DateTime.UtcNow;
        var breaks = w.Breaks
            .OrderBy(b => b.StartTime)
            .Select(b => new BreakDto(
                b.Id, b.StartTime, b.EndTime,
                (int)((b.EndTime ?? now) - b.StartTime).TotalMinutes))
            .ToList();

        return new WorkdayDto(
            w.Id,
            w.EmployeeId,
            w.Date,
            w.CheckIn,
            w.CheckOut,
            w.Status,
            (int)w.WorkedDuration(now).TotalMinutes,
            breaks);
    }
}
