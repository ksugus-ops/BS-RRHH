using FluentValidation;
using HRIA.Application.Absences;
using HRIA.Application.Ai;
using HRIA.Application.Audit;
using HRIA.Application.Auth;
using HRIA.Application.Dashboard;
using HRIA.Application.Employees;
using HRIA.Application.Schedules;
using HRIA.Application.TimeTracking;
using HRIA.Application.WorkCalendars;
using Microsoft.Extensions.DependencyInjection;

namespace HRIA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Servicios de caso de uso.
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ITimeTrackingService, TimeTrackingService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IWorkCalendarService, WorkCalendarService>();
        services.AddScoped<IWorkingDayCalculator, WorkingDayCalculator>();
        services.AddScoped<IExpectedMinutesCalculator, ExpectedMinutesCalculator>();
        services.AddScoped<IAbsenceService, AbsenceService>();

        services.AddScoped<IAuditService, AuditService>();

        // Asistente de IA.
        services.AddScoped<AiToolRegistry>();
        services.AddScoped<IAiAssistantService, AiAssistantService>();
        services.AddScoped<IAiAssistant, DemoAssistant>();

        // Validadores (FluentValidation).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
