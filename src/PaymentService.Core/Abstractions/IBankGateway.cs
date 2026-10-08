using PaymentService.Core.Domain;

namespace PaymentService.Core.Abstractions;

/// <summary>
/// Sends a payment instruction to a banking partner. A real implementation would translate this into
/// the bank's format (ISO 20022 pain.001, SWIFT, or the bank's API).
///
/// A definitive answer from the bank is returned as a <see cref="BankResult"/>.
/// A timeout or network error is thrown: in that case we do not know whether the bank has the payment.
/// </summary>
public interface IBankGateway
{
    Task<BankResult> SubmitAsync(BankInstruction instruction, CancellationToken cancellationToken = default);
}

/// <param name="IdempotencyKey">The same for every attempt of a payment, so the bank never executes it twice.</param>
public sealed record BankInstruction(
    string IdempotencyKey,
    string Reference,
    Money Amount,
    Beneficiary Beneficiary,
    string BankPartner);

public enum BankOutcome
{
    Accepted,
    Rejected,
}

public sealed record BankResult(BankOutcome Outcome, string? BankReference, string? RejectionReason)
{
    public static BankResult Accepted(string bankReference) => new(BankOutcome.Accepted, bankReference, null);

    public static BankResult Rejected(string reason) => new(BankOutcome.Rejected, null, reason);
}
