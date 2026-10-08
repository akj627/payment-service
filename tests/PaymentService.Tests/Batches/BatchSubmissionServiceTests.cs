using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Batches;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Tests.Batches;

public class BatchSubmissionServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public BatchSubmissionServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var db = NewDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Valid_batch_is_created_and_saved()
    {
        var result = await NewService().SubmitAsync("key-1", Request(Payment("INV-1", "1250.50")));

        Assert.Equal(SubmitBatchOutcome.Created, result.Outcome);
        using var db = NewDbContext();
        Assert.Equal(1, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task Same_idempotency_key_returns_the_original_batch_without_creating_another()
    {
        var first = await NewService().SubmitAsync("key-1", Request(Payment("INV-1", "1250.50")));

        var second = await NewService().SubmitAsync("key-1", Request(Payment("INV-1", "1250.50")));

        Assert.Equal(SubmitBatchOutcome.Replayed, second.Outcome);
        Assert.Equal(first.Batch!.Id, second.Batch!.Id);
        using var db = NewDbContext();
        Assert.Equal(1, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task Invalid_batch_reports_every_problem_and_saves_nothing()
    {
        var request = Request(
            Payment("INV-1", "10.005"),                      // too many decimals for USD
            Payment("INV-1", "5") with { Currency = "USX" }); // duplicate reference, unknown currency

        var result = await NewService().SubmitAsync("key-1", request);

        Assert.Equal(SubmitBatchOutcome.Invalid, result.Outcome);
        Assert.Contains("payments[0].amount", result.Errors!.Keys);
        Assert.Contains("payments[1].reference", result.Errors.Keys);
        Assert.Contains("payments[1].currency", result.Errors.Keys);

        using var db = NewDbContext();
        Assert.Equal(0, await db.Batches.CountAsync());
    }

    [Fact]
    public async Task Empty_batch_is_invalid()
    {
        var result = await NewService().SubmitAsync("key-1", new SubmitBatchRequest([]));

        Assert.Equal(SubmitBatchOutcome.Invalid, result.Outcome);
        Assert.Contains("payments", result.Errors!.Keys);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Payment_with_bad_amount_is_invalid(string amount)
    {
        var result = await NewService().SubmitAsync("key-1", Request(Payment("INV-1", amount)));

        Assert.Equal(SubmitBatchOutcome.Invalid, result.Outcome);
        Assert.Contains("payments[0].amount", result.Errors!.Keys);
    }

    [Fact]
    public async Task Missing_fields_are_reported_by_name()
    {
        var result = await NewService().SubmitAsync("key-1", Request(new PaymentRequest(null, "10", "USD", null, null, null)));

        Assert.Equal(
            ["payments[0].bankPartner", "payments[0].beneficiaryAccount", "payments[0].beneficiaryName", "payments[0].reference"],
            result.Errors!.Keys.Order().ToArray());
    }

    private BatchSubmissionService NewService() =>
        new(new PaymentBatchRepository(NewDbContext()), TimeProvider.System);

    private PaymentsDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<PaymentsDbContext>().UseSqlite(_connection).Options);

    private static SubmitBatchRequest Request(params PaymentRequest[] payments) => new(payments);

    private static PaymentRequest Payment(string reference, string amount) =>
        new(reference, amount, "USD", "Acme Ltd", "GB29NWBK60161331926819", "BANK-A");
}
