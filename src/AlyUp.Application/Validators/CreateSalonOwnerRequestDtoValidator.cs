using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class CreateSalonOwnerRequestDtoValidator : AbstractValidator<CreateSalonOwnerRequestDto>
{
    public CreateSalonOwnerRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome e obrigatorio.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Nome nao pode conter apenas espacos em branco.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email e obrigatorio.")
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("Email nao pode conter apenas espacos em branco.")
            .EmailAddress().WithMessage("Email invalido.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha e obrigatoria.")
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("Senha nao pode conter apenas espacos em branco.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Senha deve ter ao menos 8 caracteres, maiuscula, minuscula, numero e simbolo.");

        RuleFor(x => x.SalonName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome do salao e obrigatorio.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Nome do salao nao pode conter apenas espacos em branco.");

        RuleFor(x => x.SalonDocument)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Documento do salao e obrigatorio.")
            .Must(document => !string.IsNullOrWhiteSpace(document))
            .WithMessage("Documento do salao nao pode conter apenas espacos em branco.");

        RuleFor(x => x.SalonAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Endereco do salao e obrigatorio.")
            .Must(address => !string.IsNullOrWhiteSpace(address))
            .WithMessage("Endereco do salao nao pode conter apenas espacos em branco.");
    }
}
