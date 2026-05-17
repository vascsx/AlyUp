namespace AlyUp.Application.Interfaces;

public interface IInputNormalizer
{
    string NormalizeEmail(string email);
    string NormalizeText(string value);
    string NormalizeNullableText(string? value);
}
