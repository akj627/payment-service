using PaymentService.Core.Dispatch;

namespace PaymentService.Api.Dispatch;

/// <summary>
/// Polls for pending payments every few seconds and dispatches them.
/// A single instance is assumed; running several safely needs leases (see the architecture doc).
/// </summary>
public class DispatchWorker(IServiceScopeFactory scopeFactory, ILogger<DispatchWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<PaymentDispatcher>();

                var dispatched = await dispatcher.DispatchPendingAsync(BatchSize, stoppingToken);

                foreach (var payment in dispatched)
                {
                    logger.LogInformation(
                        "Payment {PaymentId} ({Reference}) in batch {BatchId}: {Status} after attempt {Attempts}",
                        payment.Id, payment.Reference, payment.BatchId, payment.Status, payment.Attempts);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Keep the worker alive; the next tick tries again.
                logger.LogError(ex, "Dispatch loop failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
