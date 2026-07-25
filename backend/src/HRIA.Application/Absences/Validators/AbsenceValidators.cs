using FluentValidation;
using HRIA.Application.Absences.Dtos;

namespace HRIA.Application.Absences.Validators;

public class CreateAbsenceRequestValidator : AbstractValidator<CreateAbsenceRequest>
{
    public CreateAbsenceRequestValidator()
    {
        RuleFor(x => x.AbsenceTypeId).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("La fecha de fin no puede ser anterior a la de inicio.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class DecideAbsenceRequestValidator : AbstractValidator<DecideAbsenceRequest>
{
    public DecideAbsenceRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(500);
    }
}

public class SetVacationAllowanceRequestValidator : AbstractValidator<SetVacationAllowanceRequest>
{
    public SetVacationAllowanceRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Days)
            .InclusiveBetween(0, 365)
            .WithMessage("Los días deben estar entre 0 y 365.");
    }
}
