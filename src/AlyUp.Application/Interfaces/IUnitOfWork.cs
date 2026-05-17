namespace AlyUp.Application.Interfaces;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
