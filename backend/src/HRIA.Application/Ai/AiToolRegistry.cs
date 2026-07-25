using System.Text.Json;
using HRIA.Application.Common.Interfaces;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HRIA.Application.Ai;

/// <summary>
/// Construye el catálogo de herramientas AUTORIZADAS para el usuario actual.
/// Las herramientas de administrador solo se incluyen si el rol es Admin.
/// Cada ejecutor valida los argumentos, aplica los filtros de permiso (p. ej. un
/// empleado solo consulta sus propios datos) y ejecuta consultas parametrizadas
/// con resultados limitados.
/// </summary>
public sealed class AiToolRegistry
{
    private const int MaxRows = 50;
    private readonly IAppDbContext _db;

    public AiToolRegistry(IAppDbContext db) => _db = db;

    public IReadOnlyList<AiTool> BuildTools(Role role, int currentEmployeeId)
    {
        var tools = new List<AiTool>();

        if (role == Role.Admin)
        {
            tools.Add(CurrentWorkingEmployees());
            tools.Add(OpenTimeEntries());
            tools.Add(IncompleteWorkdays());
            tools.Add(DepartmentHoursSummary());
        }

        // Disponible para ambos; el empleado queda forzado a su propio id.
        tools.Add(EmployeeHoursSummary(role, currentEmployeeId));

        return tools;
    }

    // --- Herramientas ---

    private AiTool CurrentWorkingEmployees() => new(
        "get_current_working_employees",
        "Devuelve los empleados que están trabajando ahora mismo (jornada abierta y sin descanso en curso).",
        new[] { "trabajando", "ahora", "activos", "quién trabaja" },
        EmptySchema(),
        async (_, ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var open = await OpenTodayQuery(today).ToListAsync(ct);
            var working = open.Where(w => !w.HasOpenBreak).Take(MaxRows).ToList();
            var names = working.Select(w => $"{w.Employee!.FullName} ({w.Employee.Department?.Name})").ToList();
            var summary = working.Count == 0
                ? "Ahora mismo no hay ningún empleado trabajando."
                : $"Hay {working.Count} empleado(s) trabajando ahora: {string.Join(", ", names)}.";
            return new AiToolResult(ToJson(new { count = working.Count, employees = names }), summary);
        });

    private AiTool OpenTimeEntries() => new(
        "get_open_time_entries",
        "Devuelve todas las jornadas abiertas actualmente (empleados trabajando o en descanso).",
        new[] { "jornada abierta", "abierta", "quién tiene", "fichajes abiertos" },
        EmptySchema(),
        async (_, ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var open = await OpenTodayQuery(today).Take(MaxRows).ToListAsync(ct);
            var items = open.Select(w => new
            {
                employee = w.Employee!.FullName,
                department = w.Employee.Department?.Name,
                state = w.HasOpenBreak ? "En descanso" : "Trabajando"
            }).ToList();
            var summary = open.Count == 0
                ? "No hay jornadas abiertas en este momento."
                : $"Hay {open.Count} jornada(s) abierta(s): " +
                  string.Join("; ", items.Select(i => $"{i.employee} — {i.state}")) + ".";
            return new AiToolResult(ToJson(items), summary);
        });

    private AiTool IncompleteWorkdays() => new(
        "get_incomplete_workdays",
        "Devuelve las jornadas marcadas como incompletas (entrada sin salida válida). Acepta rango de fechas opcional 'from' y 'to' (YYYY-MM-DD).",
        new[] { "incompleta", "incompletas", "sin salida", "sin fichar salida" },
        RangeSchema(),
        async (args, ct) =>
        {
            var (from, to) = ParseRange(args, defaultDays: 30);
            var list = await _db.Workdays
                .Include(w => w.Employee)!.ThenInclude(e => e!.Department)
                .Where(w => w.Status == WorkdayStatus.Incomplete && w.Date >= from && w.Date <= to)
                .OrderByDescending(w => w.Date)
                .Take(MaxRows)
                .ToListAsync(ct);
            var items = list.Select(w => new { employee = w.Employee!.FullName, date = w.Date.ToString("yyyy-MM-dd") }).ToList();
            var summary = list.Count == 0
                ? "No hay jornadas incompletas en el rango indicado."
                : $"Hay {list.Count} jornada(s) incompleta(s): " +
                  string.Join(", ", items.Select(i => $"{i.employee} ({i.date})")) + ".";
            return new AiToolResult(
                ToJson(new { from = from.ToString("yyyy-MM-dd"), to = to.ToString("yyyy-MM-dd"), items }),
                summary);
        });

    private AiTool EmployeeHoursSummary(Role role, int currentEmployeeId) => new(
        "get_employee_hours_summary",
        "Resume las horas trabajadas por un empleado en un rango de fechas. Parámetros: 'employeeId' (opcional; ignorado para empleados), 'from' y 'to' (YYYY-MM-DD).",
        new[] { "horas", "resumen", "cuántas horas", "esta semana", "mis horas" },
        EmployeeSummarySchema(),
        async (args, ct) =>
        {
            var (from, to) = ParseRange(args, defaultDays: 7);

            // Protección horizontal: el empleado SIEMPRE consulta sus propios datos.
            int employeeId = currentEmployeeId;
            if (role == Role.Admin && TryGetInt(args, "employeeId", out var requested) && requested > 0)
                employeeId = requested;

            var employee = await _db.Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
            if (employee is null)
                return new AiToolResult("{}", "No se encontró el empleado indicado.");

            var workdays = await _db.Workdays.Include(w => w.Breaks)
                .Where(w => w.EmployeeId == employeeId && w.Date >= from && w.Date <= to)
                .ToListAsync(ct);

            var minutes = workdays.Sum(WorkedMinutes);
            var hours = Math.Round(minutes / 60.0, 1);
            var summary = $"{employee.FullName} ha trabajado {hours} horas entre {from:yyyy-MM-dd} y {to:yyyy-MM-dd} " +
                          $"({workdays.Count} jornada(s)).";
            // El rango viaja EN el resultado: si el modelo omite 'from'/'to' se aplica el
            // valor por defecto, y sin decírselo respondía "este mes" con datos de la semana.
            return new AiToolResult(
                ToJson(new
                {
                    employee = employee.FullName,
                    from = from.ToString("yyyy-MM-dd"),
                    to = to.ToString("yyyy-MM-dd"),
                    hours,
                    workdays = workdays.Count
                }),
                summary);
        });

    private AiTool DepartmentHoursSummary() => new(
        "get_department_hours_summary",
        "Resume las horas trabajadas por un departamento en un rango de fechas. Parámetros: 'departmentId' o 'departmentName', 'from' y 'to' (YYYY-MM-DD).",
        new[] { "departamento", "desarrollo", "ventas", "operaciones", "por departamento" },
        DepartmentSummarySchema(),
        async (args, ct) =>
        {
            var (from, to) = ParseRange(args, defaultDays: 7);

            Department? dept = null;
            if (TryGetInt(args, "departmentId", out var deptId) && deptId > 0)
                dept = await _db.Departments.FirstOrDefaultAsync(d => d.Id == deptId, ct);
            else if (TryGetString(args, "departmentName", out var deptName) && !string.IsNullOrWhiteSpace(deptName))
                dept = await _db.Departments.FirstOrDefaultAsync(d => d.Name.ToLower() == deptName!.ToLower(), ct);

            if (dept is null)
                return new AiToolResult("{}", "No se encontró el departamento indicado.");

            var workdays = await _db.Workdays.Include(w => w.Breaks)
                .Where(w => w.Employee!.DepartmentId == dept.Id && w.Date >= from && w.Date <= to)
                .ToListAsync(ct);

            var hours = Math.Round(workdays.Sum(WorkedMinutes) / 60.0, 1);
            var summary = $"El departamento de {dept.Name} ha trabajado {hours} horas entre " +
                          $"{from:yyyy-MM-dd} y {to:yyyy-MM-dd} ({workdays.Count} jornada(s)).";
            return new AiToolResult(
                ToJson(new
                {
                    department = dept.Name,
                    from = from.ToString("yyyy-MM-dd"),
                    to = to.ToString("yyyy-MM-dd"),
                    hours,
                    workdays = workdays.Count
                }),
                summary);
        });

    // --- Helpers de consulta ---

    private IQueryable<Workday> OpenTodayQuery(DateOnly today) =>
        _db.Workdays
            .Include(w => w.Breaks)
            .Include(w => w.Employee)!.ThenInclude(e => e!.Department)
            .Where(w => w.CheckOut == null && w.Status == WorkdayStatus.Open && w.Date == today);

    private static double WorkedMinutes(Workday w)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        if (w.CheckOut is not null) return w.WorkedDuration(now).TotalMinutes;
        return w.Date == today ? w.WorkedDuration(now).TotalMinutes : 0d;
    }

    // --- Validación / parseo de argumentos ---

    private static (DateOnly from, DateOnly to) ParseRange(JsonElement? args, int defaultDays)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var to = TryGetDate(args, "to") ?? today;
        var from = TryGetDate(args, "from") ?? to.AddDays(-defaultDays);
        if (from > to) (from, to) = (to, from); // saneamiento
        return (from, to);
    }

    private static DateOnly? TryGetDate(JsonElement? args, string name)
    {
        if (TryGetString(args, name, out var s) && DateOnly.TryParse(s, out var d)) return d;
        return null;
    }

    private static bool TryGetInt(JsonElement? args, string name, out int value)
    {
        value = 0;
        if (args is null || args.Value.ValueKind != JsonValueKind.Object) return false;
        if (!args.Value.TryGetProperty(name, out var prop)) return false;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out value)) return true;
        if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value)) return true;
        return false;
    }

    private static bool TryGetString(JsonElement? args, string name, out string? value)
    {
        value = null;
        if (args is null || args.Value.ValueKind != JsonValueKind.Object) return false;
        if (!args.Value.TryGetProperty(name, out var prop)) return false;
        if (prop.ValueKind == JsonValueKind.String) { value = prop.GetString(); return true; }
        return false;
    }

    private static string ToJson(object o) => JsonSerializer.Serialize(o);

    // --- Esquemas JSON (para function calling) ---

    private static object EmptySchema() => new { type = "object", properties = new { } };

    private static object RangeSchema() => new
    {
        type = "object",
        properties = new
        {
            from = new { type = "string", description = "Fecha inicial YYYY-MM-DD" },
            to = new { type = "string", description = "Fecha final YYYY-MM-DD" }
        }
    };

    private static object EmployeeSummarySchema() => new
    {
        type = "object",
        properties = new
        {
            employeeId = new { type = "integer", description = "Id del empleado (solo administrador)" },
            from = new { type = "string", description = "Fecha inicial YYYY-MM-DD" },
            to = new { type = "string", description = "Fecha final YYYY-MM-DD" }
        }
    };

    private static object DepartmentSummarySchema() => new
    {
        type = "object",
        properties = new
        {
            departmentId = new { type = "integer" },
            departmentName = new { type = "string" },
            from = new { type = "string", description = "Fecha inicial YYYY-MM-DD" },
            to = new { type = "string", description = "Fecha final YYYY-MM-DD" }
        }
    };
}
