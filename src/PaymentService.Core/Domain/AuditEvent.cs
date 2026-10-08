namespace PaymentService.Core.Domain;

/// <summary>
/// One status change of a payment. Only ever added, never updated or deleted,
/// and saved in the same transaction as the change itself.
/// The Id is assigned by the database in insert order, which gives the audit trail its order.
/// </summary>
public class AuditEvent
{
    public AuditEvent(Guid batchId, Guid paymentId, PaymentStatus? fromStatus, PaymentStatus toStatus, string? note, DateTimeOffset occurredAt)
    {
        BatchId = batchId;
        PaymentId = paymentId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
        OccurredAt = occurredAt;
    }

    public long Id { get; private set; }

    public Guid BatchId { get; private set; }

    public Guid PaymentId { get; private set; }

    public PaymentStatus? FromStatus { get; private set; }

    public PaymentStatus ToStatus { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
