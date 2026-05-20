using AlyUp.Application.DTOs.Auth;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AlyUp.Application.Validators;

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .Must(email => !string.IsNullOrWhiteSpace(email))
            .WithMessage("E-mail não pode conter apenas espaços em branco.")
            .Must(BeValidEmail).WithMessage("E-mail inválido.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .WithMessage("Senha não pode conter apenas espaços em branco.")
            .MinimumLength(8)
            .WithMessage("A senha deve ter no mínimo 8 caracteres.");
    }

    private static bool BeValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$");
    }
}
