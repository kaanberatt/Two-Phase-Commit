namespace Coordinator.Services.Abstractions;

public interface ITransactionService
{
    Task<Guid> CreateTransaction();
    Task PrepareService(Guid transactionId);
    Task<bool> CheckReadyService(Guid transactionId);
    Task Commit(Guid transactionId);
    Task<bool> CheckTransactionStateServices(Guid transactionId);
    Task Rollback(Guid transactionId);
}
