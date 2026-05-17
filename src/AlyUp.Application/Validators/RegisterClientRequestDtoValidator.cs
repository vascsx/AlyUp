using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class RegisterClientRequestDtoValidator : AbstractValidator<RegisterClientRequestDto>
{
    public RegisterClientRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Nome e obrigatorio.")
            .MaximumLength(150)
            .WithMessage("Nome deve ter no maximo 150 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email e obrigatorio.")
            .EmailAddress()
            .WithMessage("Email invalido.")
            .MaximumLength(150)
            .WithMessage("Email deve ter no maximo 150 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Senha e obrigatoria.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Senha deve ter ao menos 8 caracteres, maiuscula, minuscula, numero e simbolo.");
    }
}
