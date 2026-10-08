using System.Collections.Concurrent;
using PaymentService.Core.Abstractions;

namespace PaymentService.Infrastructure.Banking;

/// <summary>
/// Stands in for real banking partners. The outcome is chosen by the end of the beneficiary account
/// number, so every case can be triggered on purpose:
///
///   ...REJECT     the bank rejects the payment
///   ...TIMEOUT    the bank never answers
///   ...LOSTREPLY  the bank accepts the payment, but the first reply is lost (we see a timeout)
///   anything else the bank accepts the payment
///
/// Like a real bank, it remembers idempotency keys: sending the same key again returns the original
/// reference instead of creating a second payment. That is what makes retries safe.
/// State is in memory only, so it is reset when the app restarts.
/// </summary>
public class SimulatedBank(TimeSpan responseDelay) : IBankGateway
{
    private readonly ConcurrentDictionary<string, string> _acceptedPayments = new();

    /// <summary>How many distinct payments the bank has accepted (used by tests).</summary>
    public int AcceptedCount => _acceptedPayments.Count;

    public async Task<BankResult> SubmitAsync(BankInstruction instruction, CancellationToken cancellationToken = default)
    {
        await Task.Delay(responseDelay, cancellationToken);

        // Seen this payment before: return the original answer, never execute it twice.
        if (_acceptedPayments.TryGetValue(instruction.IdempotencyKey, out var existingReference))
        {
            return BankResult.Accepted(existingReference);
        }

        string account = instruction.Beneficiary.AccountNumber;

        if (account.EndsWith("REJECT", StringComparison.OrdinalIgnoreCase))
        {
            return BankResult.Rejected("Beneficiary account is closed.");
        }

        if (account.EndsWith("TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            throw new TimeoutException("Bank did not respond.");
        }

        string bankReference = $"{instruction.BankPartner}-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        _acceptedPayments[instruction.IdempotencyKey] = bankReference;

        if (account.EndsWith("LOSTREPLY", StringComparison.OrdinalIgnoreCase))
        {
            // The bank has the payment, but the reply never reaches us.
            throw new TimeoutException("Bank accepted the payment but the reply was lost.");
        }

        return BankResult.Accepted(bankReference);
    }
}
