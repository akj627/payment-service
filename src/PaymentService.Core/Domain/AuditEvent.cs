namespace PaymentService.Core.Domain;

/// <summary>
/// One status change of a payment. Only ever added, never updated or deleted,
/// and saved in the same transaction as the change itself.
/// </summary>
public class AuditEvent
{
    public AuditEvent(Guid batchId, Guid paymentId, PaymentStatus? fromStatus, PaymentStatus toStatus, string? note, DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        BatchId = batchId;
        PaymentId = paymentId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public Guid BatchId { get; private set; }

    public Guid PaymentId { get; private set; }

    public PaymentStatus? FromStatus { get; private set; }

    public PaymentStatus ToStatus { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
