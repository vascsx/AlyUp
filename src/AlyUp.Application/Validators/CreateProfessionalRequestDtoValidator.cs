using AlyUp.Application.DTOs.Auth;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class CreateProfessionalRequestDtoValidator : AbstractValidator<CreateProfessionalRequestDto>
{
    public CreateProfessionalRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("A senha deve ter ao menos 8 caracteres, letra maiúscula, letra minúscula, número e símbolo.");

        RuleFor(x => x.SalonId)
            .NotEqual(Guid.Empty)
            .When(x => x.SalonId.HasValue);
    }
}