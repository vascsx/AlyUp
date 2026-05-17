namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção base para todas as exceções de domínio.
/// Representa violações de regras de negócio do domínio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
