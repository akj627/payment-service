namespace PaymentService.Core.Domain;

/// <summary>
/// One outbound payment. Status can only change through methods on this class
/// (added with the state machine), so every change is checked and audited in one place.
/// </summary>
public class Payment
{
    private readonly List<AuditEvent> _auditEvents = new();

    public Payment(Guid batchId, NewPayment details, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (!details.Amount.IsPositive)
        {
            throw new DomainException($"Payment '{details.Reference}' must have an amount greater than zero.");
        }

        Id = Guid.NewGuid();
        BatchId = batchId;
        Reference = details.Reference;
        Amount = details.Amount;
        Beneficiary = details.Beneficiary;
        BankPartner = details.BankPartner;
        Status = PaymentStatus.Pending;
        CreatedAt = now;
        UpdatedAt = now;

        _auditEvents.Add(new AuditEvent(BatchId, Id, fromStatus: null, PaymentStatus.Pending, note: null, now));
    }

    public Guid Id { get; private set; }

    public Guid BatchId { get; private set; }

    /// <summary>The client's own reference, such as an invoice number. Unique within a batch.</summary>
    public string Reference { get; private set; }

    public Money Amount { get; private set; }

    public Beneficiary Beneficiary { get; private set; }

    /// <summary>Which banking partner sends this payment.</summary>
    public string BankPartner { get; private set; }

    public PaymentStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public string? BankReference { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents;

    public bool IsFinished =>
        Status is PaymentStatus.Submitted or PaymentStatus.Failed or PaymentStatus.NeedsReview;
}

public sealed record NewPayment(string Reference, Money Amount, Beneficiary Beneficiary, string BankPartner);
