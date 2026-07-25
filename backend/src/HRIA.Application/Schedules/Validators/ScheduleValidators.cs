using FluentValidation;
using HRIA.Application.Schedules.Dtos;

namespace HRIA.Application.Schedules.Validators;

public class ScheduleSlotInputValidator : AbstractValidator<ScheduleSlotInput>
{
    public ScheduleSlotInputValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum().WithMessage("Día de la semana no válido.");
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("La hora de fin debe ser posterior a la de inicio.");
    }
}

public class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.Slots).NotEmpty().WithMessage("El horario debe tener al menos un tramo.");
        RuleForEach(x => x.Slots).SetValidator(new ScheduleSlotInputValidator());
    }
}

public class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
        RuleFor(x => x.Slots).NotEmpty().WithMessage("El horario debe tener al menos un tramo.");
        RuleForEach(x => x.Slots).SetValidator(new ScheduleSlotInputValidator());
    }
}

public class CreateScheduleAssignmentRequestValidator : AbstractValidator<CreateScheduleAssignmentRequest>
{
    public CreateScheduleAssignmentRequestValidator()
    {
        RuleFor(x => x.ScheduleId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate is not null)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
    }
}

public class UpdateScheduleAssignmentRequestValidator : AbstractValidator<UpdateScheduleAssignmentRequest>
{
    public UpdateScheduleAssignmentRequestValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate is not null)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
    }
}
