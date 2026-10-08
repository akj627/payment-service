using PaymentService.Core.Domain;

namespace PaymentService.Api.Batches;

public sealed record BatchResponse(
    Guid Id,
    BatchStatus Status,
    IReadOnlyList<PaymentResponse> Payments,
    IReadOnlyList<AuditEventResponse> AuditTrail)
{
    public static BatchResponse From(PaymentBatch batch, IReadOnlyList<AuditEvent> auditTrail) =>
        new(
            batch.Id,
            batch.Status,
            batch.Payments.Select(PaymentResponse.From).ToList(),
            auditTrail.Select(AuditEventResponse.From).ToList());
}

public sealed record PaymentResponse(
    Guid Id,
    string Reference,
    string Amount,
    string Currency,
    string BeneficiaryName,
    string BankPartner,
    PaymentStatus Status,
    int Attempts,
    string? BankReference,
    string? FailureReason)
{
    public static PaymentResponse From(Payment payment) =>
        new(
            payment.Id,
            payment.Reference,
            payment.Amount.ToAmountString(),
            payment.Amount.Currency.Code,
            payment.Beneficiary.Name,
            payment.BankPartner,
            payment.Status,
            payment.Attempts,
            payment.BankReference,
            payment.FailureReason);
}

public sealed record AuditEventResponse(
    Guid PaymentId,
    PaymentStatus? FromStatus,
    PaymentStatus ToStatus,
    string? Note,
    DateTimeOffset OccurredAt)
{
    public static AuditEventResponse From(AuditEvent auditEvent) =>
        new(auditEvent.PaymentId, auditEvent.FromStatus, auditEvent.ToStatus, auditEvent.Note, auditEvent.OccurredAt);
}
