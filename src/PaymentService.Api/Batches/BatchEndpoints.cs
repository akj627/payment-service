using Microsoft.AspNetCore.Mvc;
using PaymentService.Core.Abstractions;
using PaymentService.Core.Batches;

namespace PaymentService.Api.Batches;

public static class BatchEndpoints
{
    private const int MaxIdempotencyKeyLength = 100;

    public static void MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/batches", SubmitBatch);
        app.MapGet("/batches/{id:guid}", GetBatch).WithName(nameof(GetBatch));
    }

    private static async Task<IResult> SubmitBatch(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        SubmitBatchRequest request,
        BatchSubmissionService submissionService,
        IPaymentBatchRepository repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > MaxIdempotencyKeyLength)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Idempotency-Key"] = [$"The Idempotency-Key header is required (at most {MaxIdempotencyKeyLength} characters)."],
            });
        }

        var result = await submissionService.SubmitAsync(idempotencyKey, request, cancellationToken);

        if (result.Outcome == SubmitBatchOutcome.Invalid)
        {
            return Results.ValidationProblem(result.Errors!.ToDictionary());
        }

        var batch = result.Batch!;
        var auditTrail = await repository.GetAuditTrailAsync(batch.Id, cancellationToken);
        var response = BatchResponse.From(batch, auditTrail);

        // A replay returns 200 with the original batch; a new batch returns 201.
        return result.Outcome == SubmitBatchOutcome.Created
            ? Results.CreatedAtRoute(nameof(GetBatch), new { id = batch.Id }, response)
            : Results.Ok(response);
    }

    private static async Task<IResult> GetBatch(
        Guid id,
        IPaymentBatchRepository repository,
        CancellationToken cancellationToken)
    {
        var batch = await repository.GetAsync(id, cancellationToken);
        if (batch == null)
        {
            return Results.NotFound();
        }

        var auditTrail = await repository.GetAuditTrailAsync(id, cancellationToken);
        return Results.Ok(BatchResponse.From(batch, auditTrail));
    }
}
