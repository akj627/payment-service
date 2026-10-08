using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentService.Core.Dispatch;
using PaymentService.Core.Domain;
using PaymentService.Infrastructure.Banking;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Tests.Dispatch;

/// <summary>Dispatcher + real SQLite (in memory) + simulated bank with no delay.</summary>
public class PaymentDispatcherTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;
    private readonly SimulatedBank _bank = new(TimeSpan.Zero);

    public PaymentDispatcherTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var db = NewDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task Accepted_payment_is_Submitted_with_the_bank_reference()
    {
        var paymentId = await SubmitPayment("GB29NWBK60161331926819");

        await RunDispatcher();

        var payment = await LoadPayment(paymentId);
        Assert.Equal(PaymentStatus.Submitted, payment.Status);
        Assert.StartsWith("BANK-A-", payment.BankReference);
        Assert.Equal(1, payment.Attempts);
    }

    [Fact]
    public async Task Rejected_payment_is_Failed_with_the_reason()
    {
        var paymentId = await SubmitPayment("GB00ACCOUNT-REJECT");

        await RunDispatcher();

        var payment = await LoadPayment(paymentId);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Beneficiary account is closed.", payment.FailureReason);
    }

    [Fact]
    public async Task Timeout_is_retried_and_then_goes_to_NeedsReview_never_Failed()
    {
        var paymentId = await SubmitPayment("GB00ACCOUNT-TIMEOUT");

        await RunDispatcher();
        Assert.Equal(PaymentStatus.Pending, (await LoadPayment(paymentId)).Status); // retry scheduled

        await RunDispatcher();
        await RunDispatcher();

        var payment = await LoadPayment(paymentId);
        Assert.Equal(PaymentStatus.NeedsReview, payment.Status);
        Assert.Equal(PaymentDispatcher.MaxAttempts, payment.Attempts);
    }

    [Fact]
    public async Task Retry_after_a_lost_reply_does_not_send_the_payment_twice()
    {
        var paymentId = await SubmitPayment("GB00ACCOUNT-LOSTREPLY");

        await RunDispatcher(); // bank accepts, but we only see a timeout
        await RunDispatcher(); // retry with the same idempotency key

        var payment = await LoadPayment(paymentId);
        Assert.Equal(PaymentStatus.Submitted, payment.Status);
        Assert.Equal(2, payment.Attempts);
        Assert.Equal(1, _bank.AcceptedCount); // the bank executed it once
    }

    [Fact]
    public async Task Finished_payments_are_not_sent_again()
    {
        var paymentId = await SubmitPayment("GB29NWBK60161331926819");

        await RunDispatcher();
        await RunDispatcher();

        Assert.Equal(1, (await LoadPayment(paymentId)).Attempts);
    }

    [Fact]
    public async Task Every_status_change_is_saved_to_the_audit_trail()
    {
        var paymentId = await SubmitPayment("GB00ACCOUNT-LOSTREPLY");

        await RunDispatcher();
        await RunDispatcher();

        using var db = NewDbContext();
        var trail = await db.AuditEvents
            .Where(e => e.PaymentId == paymentId)
            .OrderBy(e => e.Id)
            .Select(e => e.ToStatus)
            .ToListAsync();

        Assert.Equal(
            [PaymentStatus.Pending, PaymentStatus.Dispatching, PaymentStatus.Pending, PaymentStatus.Dispatching, PaymentStatus.Submitted],
            trail);
    }

    private async Task<Guid> SubmitPayment(string beneficiaryAccount)
    {
        var batch = new PaymentBatch(
            Guid.NewGuid().ToString(),
            [new NewPayment("INV-1", Money.FromMinorUnits(10000, Currency.FromCode("USD")), new Beneficiary("Acme Ltd", beneficiaryAccount), "BANK-A")],
            Now);

        await new PaymentBatchRepository(NewDbContext()).TryAddAsync(batch);
        return batch.Payments[0].Id;
    }

    // Each run uses a fresh DbContext, like each tick of the background worker.
    private Task RunDispatcher() =>
        new PaymentDispatcher(new PaymentRepository(NewDbContext()), _bank, TimeProvider.System).DispatchPendingAsync(10);

    private async Task<Payment> LoadPayment(Guid paymentId)
    {
        using var db = NewDbContext();
        return await db.Payments.SingleAsync(p => p.Id == paymentId);
    }

    private PaymentsDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<PaymentsDbContext>().UseSqlite(_connection).Options);
}
