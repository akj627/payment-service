using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Domain;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Tests.Infrastructure;

/// <summary>Runs against a real SQLite database held in memory for the duration of each test.</summary>
public class PaymentBatchRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;

    public PaymentBatchRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var db = NewDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Saved_batch_can_be_read_back_with_its_payments()
    {
        var batch = NewBatch("key-1");
        await new PaymentBatchRepository(NewDbContext()).TryAddAsync(batch);

        var loaded = await new PaymentBatchRepository(NewDbContext()).GetAsync(batch.Id);

        Assert.NotNull(loaded);
        Assert.Equal("key-1", loaded.IdempotencyKey);
        Assert.Equal(2, loaded.Payments.Count);

        var payment = loaded.Payments.Single(p => p.Reference == "INV-1");
        Assert.Equal(Money.FromMinorUnits(125050, Currency.FromCode("USD")), payment.Amount);
        Assert.Equal(new Beneficiary("Acme Ltd", "GB29NWBK60161331926819"), payment.Beneficiary);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(BatchStatus.Processing, loaded.Status);
    }

    [Fact]
    public async Task Audit_events_are_saved_with_the_batch()
    {
        var batch = NewBatch("key-1");
        await new PaymentBatchRepository(NewDbContext()).TryAddAsync(batch);

        var auditTrail = await new PaymentBatchRepository(NewDbContext()).GetAuditTrailAsync(batch.Id);

        Assert.Equal(2, auditTrail.Count);
        Assert.All(auditTrail, e => Assert.Equal(PaymentStatus.Pending, e.ToStatus));
    }

    [Fact]
    public async Task Second_batch_with_same_idempotency_key_is_not_saved()
    {
        Assert.True(await new PaymentBatchRepository(NewDbContext()).TryAddAsync(NewBatch("key-1")));

        bool added = await new PaymentBatchRepository(NewDbContext()).TryAddAsync(NewBatch("key-1"));

        Assert.False(added);
        using var db = NewDbContext();
        Assert.Equal(1, await db.Batches.CountAsync());
        Assert.Equal(2, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task Batch_can_be_found_by_idempotency_key()
    {
        var batch = NewBatch("key-1");
        await new PaymentBatchRepository(NewDbContext()).TryAddAsync(batch);

        var found = await new PaymentBatchRepository(NewDbContext()).FindByIdempotencyKeyAsync("key-1");

        Assert.Equal(batch.Id, found?.Id);
    }

    private PaymentsDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<PaymentsDbContext>().UseSqlite(_connection).Options);

    private static PaymentBatch NewBatch(string idempotencyKey)
    {
        var usd = Currency.FromCode("USD");
        var beneficiary = new Beneficiary("Acme Ltd", "GB29NWBK60161331926819");

        return new PaymentBatch(
            idempotencyKey,
            [
                new NewPayment("INV-1", Money.FromMinorUnits(125050, usd), beneficiary, "BANK-A"),
                new NewPayment("INV-2", Money.FromMinorUnits(500, usd), beneficiary, "BANK-B"),
            ],
            Now);
    }
}
