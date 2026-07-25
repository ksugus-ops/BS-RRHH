using FluentValidation;
using HRIA.Application.Auth.Dtos;

namespace HRIA.Application.Auth;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Indica tu contraseña actual.")
            .MaximumLength(200);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Indica la nueva contraseña.")
            .MinimumLength(AuthService.MinPasswordLength)
                .WithMessage($"La contraseña debe tener al menos {AuthService.MinPasswordLength} caracteres.")
            .MaximumLength(200);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        // Si se deja vacía, el servicio genera una temporal; solo se valida
        // cuando el administrador escribe una a mano.
        RuleFor(x => x.NewPassword)
            .MinimumLength(AuthService.MinPasswordLength)
                .WithMessage($"La contraseña debe tener al menos {AuthService.MinPasswordLength} caracteres.")
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
    }
}
