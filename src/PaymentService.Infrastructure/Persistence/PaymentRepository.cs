using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Abstractions;
using PaymentService.Core.Domain;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentRepository(PaymentsDbContext db) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetPendingAsync(int maxCount, CancellationToken cancellationToken = default) =>
        await db.Payments
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public Task SaveAsync(Payment payment, CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken); // the payment is tracked, so its changes and new audit events are saved together
}
