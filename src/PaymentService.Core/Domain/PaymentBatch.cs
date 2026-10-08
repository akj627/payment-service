namespace PaymentService.Core.Domain;

/// <summary>
/// Payments submitted together. Accepted as a whole or not at all;
/// after that each payment is sent and tracked on its own.
/// </summary>
public class PaymentBatch
{
    private readonly List<Payment> _payments = new();

    // Used by EF Core when loading from the database.
#pragma warning disable CS8618
    private PaymentBatch()
    {
    }
#pragma warning restore CS8618

    public PaymentBatch(string idempotencyKey, IReadOnlyList<NewPayment> payments, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(payments);

        if (payments.Count == 0)
        {
            throw new DomainException("A batch must contain at least one payment.");
        }

        var duplicate = payments.GroupBy(p => p.Reference).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new DomainException($"Reference '{duplicate.Key}' appears more than once in the batch.");
        }

        Id = Guid.NewGuid();
        IdempotencyKey = idempotencyKey;
        CreatedAt = now;

        foreach (var details in payments)
        {
            _payments.Add(new Payment(Id, details, now));
        }
    }

    public Guid Id { get; private set; }

    /// <summary>Supplied by the client so a resent request does not create a second batch.</summary>
    public string IdempotencyKey { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Payment> Payments => _payments;

    /// <summary>
    /// Worked out from the payments, never stored, so it can never disagree with them.
    /// </summary>
    public BatchStatus Status
    {
        get
        {
            if (_payments.Any(p => !p.IsFinished))
            {
                return BatchStatus.Processing;
            }

            return _payments.All(p => p.Status == PaymentStatus.Submitted)
                ? BatchStatus.Completed
                : BatchStatus.CompletedWithFailures;
        }
    }
}
