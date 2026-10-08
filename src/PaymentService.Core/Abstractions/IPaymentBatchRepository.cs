using PaymentService.Core.Domain;

namespace PaymentService.Core.Abstractions;

/// <summary>
/// Storage for batches. Core only knows this interface, so the database can be swapped
/// (SQLite here, Azure SQL in production) without touching business code.
/// </summary>
public interface IPaymentBatchRepository
{
    /// <summary>
    /// Saves the batch, its payments and their audit events in one transaction.
    /// Returns false, and saves nothing, if a batch with the same idempotency key already exists.
    /// </summary>
    Task<bool> TryAddAsync(PaymentBatch batch, CancellationToken cancellationToken = default);

    Task<PaymentBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<PaymentBatch?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>All audit events for the batch, oldest first.</summary>
    Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(Guid batchId, CancellationToken cancellationToken = default);
}
