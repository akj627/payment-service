using Stateless;

namespace PaymentService.Core.Domain;

/// <summary>
/// One outbound payment. Status can only change through the methods below. Each one fires a
/// trigger on <see cref="PaymentStateMachine"/>, which rejects illegal changes, and records an audit event.
/// </summary>
public class Payment
{
    private readonly List<AuditEvent> _auditEvents = new();
    private StateMachine<PaymentStatus, PaymentTrigger>? _stateMachine;

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

    /// <summary>The bank's own id for the payment, returned when it accepts it.</summary>
    public string? BankReference { get; private set; }

    /// <summary>Why the last attempt did not succeed.</summary>
    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<AuditEvent> AuditEvents => _auditEvents;

    public bool IsFinished =>
        Status is PaymentStatus.Submitted or PaymentStatus.Failed or PaymentStatus.NeedsReview;

    // Created on first use, so it also works for payments loaded from the database.
    private StateMachine<PaymentStatus, PaymentTrigger> StateMachine =>
        _stateMachine ??= PaymentStateMachine.Create(() => Status, status => Status = status);

    public void StartDispatch(DateTimeOffset now)
    {
        Fire(PaymentTrigger.StartDispatch, $"Attempt {Attempts + 1}", now);
        Attempts++;
    }

    public void MarkSubmitted(string bankReference, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankReference);
        Fire(PaymentTrigger.BankAccepted, $"Bank reference {bankReference}", now);
        BankReference = bankReference;
        FailureReason = null;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Fire(PaymentTrigger.BankRejected, reason, now);
        FailureReason = reason;
    }

    public void ScheduleRetry(string reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Fire(PaymentTrigger.ScheduleRetry, reason, now);
        FailureReason = reason;
    }

    public void MarkNeedsReview(string reason, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Fire(PaymentTrigger.EscalateForReview, reason, now);
        FailureReason = reason;
    }

    // Every status change goes through here, so every change is audited.
    private void Fire(PaymentTrigger trigger, string note, DateTimeOffset now)
    {
        var fromStatus = Status;
        StateMachine.Fire(trigger); // throws DomainException if the change is not allowed
        UpdatedAt = now;
        _auditEvents.Add(new AuditEvent(BatchId, Id, fromStatus, Status, note, now));
    }
}

public sealed record NewPayment(string Reference, Money Amount, Beneficiary Beneficiary, string BankPartner);
