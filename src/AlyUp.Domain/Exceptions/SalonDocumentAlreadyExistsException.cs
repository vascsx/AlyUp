namespace AlyUp.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um documento de salão já existe e um único é necessário.
/// </summary>
public class SalonDocumentAlreadyExistsException : DomainException
{
    public string Document { get; }

    public SalonDocumentAlreadyExistsException(string document) 
        : base($"Documento de salão '{document}' já está cadastrado no sistema.")
    {
        Document = document;
    }
}
