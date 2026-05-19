using AlyUp.Application.DTOs.ProfessionalAvailability;
using FluentValidation;

namespace AlyUp.Application.Validators;

public class UpdateProfessionalAvailabilityRequestDtoValidator : AbstractValidator<UpdateProfessionalAvailabilityRequestDto>
{
    public UpdateProfessionalAvailabilityRequestDtoValidator()
    {
        RuleFor(x => x.DayOfWeek)
            .IsInEnum()
            .WithMessage("O dia da semana informado é inválido.");

        RuleFor(x => x)
            .Must(x => x.StartTime < x.EndTime)
            .WithMessage("O horário inicial deve ser menor que o horário final.");
    }
}
