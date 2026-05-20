using AlyUp.Application.DTOs.Auth;
using FluentValidation;
using System.Text.RegularExpressions;

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
            .Must(BeValidEmail).WithMessage("E-mail inválido.");

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
            .WithMessage("Documento do salão não pode conter apenas espaços em branco.")
            .Must(BeValidCpfOrCnpj)
            .WithMessage("Documento do salão deve ser um CPF ou CNPJ válido.");

        RuleFor(x => x.SalonAddress)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Endereço do salão é obrigatório.")
            .Must(address => !string.IsNullOrWhiteSpace(address))
            .WithMessage("Endereço do salão não pode conter apenas espaços em branco.");
    }

    private static bool BeValidCpfOrCnpj(string document)
    {
        var digits = Regex.Replace(document, "\\D", string.Empty);

        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false
        };
    }

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cpf.Select(c => c - '0').ToArray();

        var firstDigit = CalculateCpfDigit(numbers, 9, 10);
        if (numbers[9] != firstDigit)
        {
            return false;
        }

        var secondDigit = CalculateCpfDigit(numbers, 10, 11);
        return numbers[10] == secondDigit;
    }

    private static int CalculateCpfDigit(int[] numbers, int length, int initialWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += numbers[i] * (initialWeight - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Distinct().Count() == 1)
        {
            return false;
        }

        var numbers = cnpj.Select(c => c - '0').ToArray();

        var firstWeights = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var secondWeights = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var firstDigit = CalculateCnpjDigit(numbers, firstWeights);
        if (numbers[12] != firstDigit)
        {
            return false;
        }

        var secondDigit = CalculateCnpjDigit(numbers, secondWeights);
        return numbers[13] == secondDigit;
    }

    private static int CalculateCnpjDigit(int[] numbers, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += numbers[i] * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool BeValidEmail(string email)
    {
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@([A-Za-z0-9-]+\.)+[A-Za-z]{2,}$");
    }
}
