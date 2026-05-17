namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando as credenciais fornecidas (email/senha) são inválidas.
/// </summary>
public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("Email ou senha inválidos.")
    {
    }

    public InvalidCredentialsException(string message) 
        : base(message)
    {
    }
}
