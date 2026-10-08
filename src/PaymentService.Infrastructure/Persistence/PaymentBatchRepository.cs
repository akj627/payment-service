using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Abstractions;
using PaymentService.Core.Domain;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentBatchRepository(PaymentsDbContext db) : IPaymentBatchRepository
{
    private const int SqliteConstraintViolation = 19;

    public async Task<bool> TryAddAsync(PaymentBatch batch, CancellationToken cancellationToken = default)
    {
        db.Batches.Add(batch);

        try
        {
            // One SaveChanges = one transaction: batch, payments and audit events together, or nothing.
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation })
        {
            // Another request with the same idempotency key won the race.
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public Task<PaymentBatch?> GetAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        db.Batches
            .Include(b => b.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);

    public Task<PaymentBatch?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        db.Batches
            .Include(b => b.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> GetAuditTrailAsync(Guid batchId, CancellationToken cancellationToken = default) =>
        await db.AuditEvents
            .Where(e => e.BatchId == batchId)
            .OrderBy(e => e.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
