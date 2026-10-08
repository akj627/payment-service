namespace PaymentService.Core.Batches;

/// <summary>
/// The batch exactly as the client sent it. Fields are nullable so validation, not JSON parsing,
/// reports what is missing, with a path the client can act on (e.g. "payments[2].amount").
/// Amounts are strings, never JSON numbers, so they are never read as floating point.
/// </summary>
public sealed record SubmitBatchRequest(IReadOnlyList<PaymentRequest>? Payments);

public sealed record PaymentRequest(
    string? Reference,
    string? Amount,
    string? Currency,
    string? BeneficiaryName,
    string? BeneficiaryAccount,
    string? BankPartner);
