using PaymentService.Core.Domain;

namespace PaymentService.Core.Abstractions;

/// <summary>Storage used by the dispatcher to find payments to send and record what happened.</summary>
public interface IPaymentRepository
{
    /// <summary>Pending payments, oldest first.</summary>
    Task<IReadOnlyList<Payment>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default);

    /// <summary>Saves the payment's status and its new audit events in one transaction.</summary>
    Task SaveAsync(Payment payment, CancellationToken cancellationToken = default);
}
