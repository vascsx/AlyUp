using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class RegisterClientRequestDtoValidator : AbstractValidator<RegisterClientRequestDto>
{
    public RegisterClientRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome é obrigatório.")
            .MaximumLength(150)
            .WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email é obrigatório.")
            .EmailAddress()
            .WithMessage("Email inválido.")
            .MaximumLength(150)
            .WithMessage("Email deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Senha é obrigatória.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Senha deve ter ao menos 8 caracteres, maiúscula, minúscula, número e símbolo.");
    }
}