using FluentValidation;
using HRIA.Application.Employees.Dtos;
using HRIA.Domain.Enums;

namespace HRIA.Application.Employees.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Role).IsInEnum().WithMessage("Rol no válido.");
        RuleFor(x => x.InitialPassword)
            .NotEmpty().MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(200);
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Role).IsInEnum().WithMessage("Rol no válido.");
    }
}
