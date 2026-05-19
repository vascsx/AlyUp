using AlyUp.Application.Interfaces;
using System.Text.RegularExpressions;

namespace AlyUp.Infrastructure.Security;

public class InputNormalizer : IInputNormalizer
{
    public string NormalizeEmail(string email) => NormalizeText(email).ToLowerInvariant();

    public string NormalizeText(string value) => CollapseWhitespace(value.Trim());

    public string NormalizeDocument(string value)
    {
        var digits = Regex.Replace(value, "\\D", string.Empty);
        return digits;
    }

    public string NormalizeNullableText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeText(value);

    private static string CollapseWhitespace(string value) =>
        Regex.Replace(value, "\\s+", " ");
}
