using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class CreateSalonOwnerRequestDtoValidator : AbstractValidator<CreateSalonOwnerRequestDto>
{
    public CreateSalonOwnerRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Nome não pode conter apenas espaços em branco.");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("E-mail não pode conter apenas espaços em branco.")
            .EmailAddress().WithMessage("E-mail inválido.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("Senha não pode conter apenas espaços em branco.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("A senha deve ter ao menos 8 caracteres, letra maiúscula, letra minúscula, número e símbolo.");

        RuleFor(x => x.SalonName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nome do salão é obrigatório.")
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Nome do salão não pode conter apenas espaços em branco.");

        RuleFor(x => x.SalonDocument)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Documento do salão é obrigatório.")
            .Must(document => !string.IsNullOrWhiteSpace(document))
            .WithMessage("Documento do salão não pode conter apenas espaços em branco.");

        RuleFor(x => x.SalonAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Endereço do salão é obrigatório.")
            .Must(address => !string.IsNullOrWhiteSpace(address))
            .WithMessage("Endereço do salão não pode conter apenas espaços em branco.");
    }
}