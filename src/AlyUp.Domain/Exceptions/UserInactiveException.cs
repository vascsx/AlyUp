namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um usuário está inativo e tenta acessar recursos que requerem estar ativo.
/// </summary>
public class UserInactiveException : DomainException
{
    public Guid UserId { get; }

    public UserInactiveException(Guid userId) 
        : base($"Usuário com ID '{userId}' está inativo.")
    {
        UserId = userId;
    }

    public UserInactiveException(string message) 
        : base(message)
    {
    }
}
