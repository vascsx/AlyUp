using AlyUp.Application.DTOs.Services;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class UpdateServiceRequestDtoValidator : AbstractValidator<UpdateServiceRequestDto>
{
    public UpdateServiceRequestDtoValidator()
    {
        Include(new CreateServiceRequestDtoValidatorAdapter());
    }

    private sealed class CreateServiceRequestDtoValidatorAdapter : AbstractValidator<UpdateServiceRequestDto>
    {
        public CreateServiceRequestDtoValidatorAdapter()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Nome é obrigatório.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Nome não pode conter apenas espaços em branco.")
                .MaximumLength(150)
                .WithMessage("O nome deve ter no máximo 150 caracteres.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("A descrição deve ter no máximo 500 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));

            RuleFor(x => x.DurationInMinutes)
                .GreaterThan(0)
                .WithMessage("A duração deve ser maior que zero.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O preço não pode ser negativo.");
        }
    }
}
