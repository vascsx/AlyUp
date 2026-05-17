namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um recurso solicitado não foi encontrado.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId) 
        : base($"{entityName} com ID '{entityId}' não foi encontrado.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public EntityNotFoundException(string message) 
        : base(message)
    {
    }
}
