using FluentValidation;
using HRIA.Application.WorkCalendars.Dtos;

namespace HRIA.Application.WorkCalendars.Validators;

public class CreateWorkCalendarRequestValidator : AbstractValidator<CreateWorkCalendarRequest>
{
    public CreateWorkCalendarRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("El año debe estar entre 2000 y 2100.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.NonWorkingWeekDays).IsInEnum().WithMessage("Día de la semana no válido.");
    }
}

public class UpdateWorkCalendarRequestValidator : AbstractValidator<UpdateWorkCalendarRequest>
{
    public UpdateWorkCalendarRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleForEach(x => x.NonWorkingWeekDays).IsInEnum().WithMessage("Día de la semana no válido.");
    }
}

public class HolidayInputValidator : AbstractValidator<HolidayInput>
{
    public HolidayInputValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Tipo de festivo no válido.");
    }
}
