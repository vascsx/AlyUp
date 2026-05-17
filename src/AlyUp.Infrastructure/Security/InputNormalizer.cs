using AlyUp.Application.Interfaces;

namespace AlyUp.Infrastructure.Security;

public class InputNormalizer : IInputNormalizer
{
    public string NormalizeEmail(string email) => NormalizeText(email).ToLowerInvariant();

    public string NormalizeText(string value) => value.Trim();

    public string NormalizeNullableText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
