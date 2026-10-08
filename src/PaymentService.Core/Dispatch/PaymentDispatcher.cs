using PaymentService.Core.Abstractions;
using PaymentService.Core.Domain;

namespace PaymentService.Core.Dispatch;

/// <summary>
/// Sends pending payments to their bank and records the outcome.
/// </summary>
public class PaymentDispatcher(IPaymentRepository payments, IBankGateway bank, TimeProvider clock)
{
    public const int MaxAttempts = 3;

    /// <summary>Dispatches up to <paramref name="maxCount"/> pending payments and returns them.</summary>
    public async Task<IReadOnlyList<Payment>> DispatchPendingAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var pending = await payments.GetPendingAsync(maxCount, cancellationToken);

        foreach (var payment in pending)
        {
            await DispatchAsync(payment, cancellationToken);
        }

        return pending;
    }

    private async Task DispatchAsync(Payment payment, CancellationToken cancellationToken)
    {
        // Saved before calling the bank: if we crash during the call, the database shows the
        // payment was being sent, rather than Pending as if nothing had happened.
        payment.StartDispatch(clock.GetUtcNow());
        await payments.SaveAsync(payment, cancellationToken);

        try
        {
            var result = await bank.SubmitAsync(
                new BankInstruction(payment.Id.ToString("N"), payment.Reference, payment.Amount, payment.Beneficiary, payment.BankPartner),
                cancellationToken);

            if (result.Outcome == BankOutcome.Accepted)
            {
                payment.MarkSubmitted(result.BankReference!, clock.GetUtcNow());
            }
            else
            {
                payment.MarkFailed(result.RejectionReason!, clock.GetUtcNow());
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Timeout or network error: the bank may or may not have the payment.
            // Retrying is safe because the idempotency key is the same on every attempt.
            // We never mark this Failed: that could lead to a resubmission and a duplicate payment.
            string reason = $"No reliable response from bank: {ex.Message}";

            if (payment.Attempts >= MaxAttempts)
            {
                payment.MarkNeedsReview($"{reason} (gave up after {payment.Attempts} attempts)", clock.GetUtcNow());
            }
            else
            {
                payment.ScheduleRetry(reason, clock.GetUtcNow());
            }
        }

        await payments.SaveAsync(payment, cancellationToken);
    }
}
