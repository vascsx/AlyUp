namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um email já existe no sistema e um único é necessário.
/// </summary>
public class EmailAlreadyExistsException : DomainException
{
    public string Email { get; }

    public EmailAlreadyExistsException(string email) 
        : base($"Email '{email}' já está cadastrado no sistema.")
    {
        Email = email;
    }
}
