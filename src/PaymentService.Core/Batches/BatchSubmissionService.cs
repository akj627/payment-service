using PaymentService.Core.Abstractions;
using PaymentService.Core.Domain;

namespace PaymentService.Core.Batches;

public class BatchSubmissionService(IPaymentBatchRepository repository, TimeProvider clock)
{
    /// <summary>
    /// Accepts a batch at most once per idempotency key. Sending the same key again returns the
    /// batch that was already created, so a client can safely retry after a timeout.
    /// </summary>
    public async Task<SubmitBatchResult> SubmitAsync(
        string idempotencyKey,
        SubmitBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing != null)
        {
            return SubmitBatchResult.Replayed(existing);
        }

        var errors = BatchRequestValidator.Validate(request, out var payments);
        if (errors.Count > 0)
        {
            return SubmitBatchResult.Invalid(errors);
        }

        var batch = new PaymentBatch(idempotencyKey, payments, clock.GetUtcNow());

        if (await repository.TryAddAsync(batch, cancellationToken))
        {
            return SubmitBatchResult.Created(batch);
        }

        // Another request with the same key was saved between our check and our insert.
        var winner = await repository.FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        return SubmitBatchResult.Replayed(winner!);
    }
}

public enum SubmitBatchOutcome
{
    Created,
    Replayed,
    Invalid,
}

public sealed record SubmitBatchResult(
    SubmitBatchOutcome Outcome,
    PaymentBatch? Batch,
    IReadOnlyDictionary<string, string[]>? Errors)
{
    public static SubmitBatchResult Created(PaymentBatch batch) => new(SubmitBatchOutcome.Created, batch, null);

    public static SubmitBatchResult Replayed(PaymentBatch batch) => new(SubmitBatchOutcome.Replayed, batch, null);

    public static SubmitBatchResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(SubmitBatchOutcome.Invalid, null, errors);
}
