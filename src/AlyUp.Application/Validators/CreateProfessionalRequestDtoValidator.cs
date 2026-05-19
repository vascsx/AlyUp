using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class CreateProfessionalRequestDtoValidator : AbstractValidator<CreateProfessionalRequestDto>
{
    public CreateProfessionalRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Nome não pode conter apenas espaços em branco.")
            .MaximumLength(150)
            .WithMessage("O nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("E-mail não pode conter apenas espaços em branco.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(150)
            .WithMessage("O e-mail deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("Senha não pode conter apenas espaços em branco.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("A senha deve ter ao menos 8 caracteres, letra maiúscula, letra minúscula, número e símbolo.");

        RuleFor(x => x.SalonId)
            .Cascade(CascadeMode.Stop)
            .NotEqual(Guid.Empty)
            .WithMessage("O identificador do salão é inválido.")
            .When(x => x.SalonId.HasValue);
    }
}