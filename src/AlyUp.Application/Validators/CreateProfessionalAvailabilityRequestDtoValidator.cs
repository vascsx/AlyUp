using AlyUp.Application.DTOs.ProfessionalAvailability;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class CreateProfessionalAvailabilityRequestDtoValidator : AbstractValidator<CreateProfessionalAvailabilityRequestDto>
{
    public CreateProfessionalAvailabilityRequestDtoValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum()
            .WithMessage("O dia da semana informado é inválido.");

        RuleFor(x => x)
            .Must(x => x.StartTime < x.EndTime)
            .WithMessage("O horário inicial deve ser menor que o horário final.");
    }
}
