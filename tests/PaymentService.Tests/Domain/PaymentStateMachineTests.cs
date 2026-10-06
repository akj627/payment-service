using PaymentService.Core.Domain;

namespace PaymentService.Tests.Domain;

public class PaymentStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Successful_dispatch_goes_Pending_Dispatching_Submitted()
    {
        var payment = NewPayment();

        payment.StartDispatch(Now);
        payment.MarkSubmitted("BANK-REF-1", Now);

        Assert.Equal(PaymentStatus.Submitted, payment.Status);
        Assert.Equal("BANK-REF-1", payment.BankReference);
        Assert.Equal(1, payment.Attempts);
    }

    [Fact]
    public void Retry_returns_payment_to_Pending_and_counts_attempts()
    {
        var payment = NewPayment();

        payment.StartDispatch(Now);
        payment.ScheduleRetry("Bank timed out", Now);
        payment.StartDispatch(Now);

        Assert.Equal(PaymentStatus.Dispatching, payment.Status);
        Assert.Equal(2, payment.Attempts);
        Assert.Equal("Bank timed out", payment.FailureReason);
    }

    [Fact]
    public void Bank_rejection_fails_the_payment()
    {
        var payment = NewPayment();

        payment.StartDispatch(Now);
        payment.MarkFailed("Account closed", Now);

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("Account closed", payment.FailureReason);
    }

    [Fact]
    public void Unknown_outcome_goes_to_NeedsReview()
    {
        var payment = NewPayment();

        payment.StartDispatch(Now);
        payment.MarkNeedsReview("Retries exhausted", Now);

        Assert.Equal(PaymentStatus.NeedsReview, payment.Status);
    }

    [Fact]
    public void Cannot_submit_a_payment_that_was_never_dispatched()
    {
        var payment = NewPayment();

        Assert.Throws<DomainException>(() => payment.MarkSubmitted("BANK-REF-1", Now));
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Final_states_cannot_change()
    {
        var payment = NewPayment();
        payment.StartDispatch(Now);
        payment.MarkSubmitted("BANK-REF-1", Now);

        Assert.Throws<DomainException>(() => payment.StartDispatch(Now));
        Assert.Throws<DomainException>(() => payment.MarkFailed("too late", Now));
        Assert.Equal(PaymentStatus.Submitted, payment.Status);
    }

    [Fact]
    public void Every_status_change_is_audited()
    {
        var payment = NewPayment();

        payment.StartDispatch(Now);
        payment.ScheduleRetry("Bank timed out", Now);
        payment.StartDispatch(Now);
        payment.MarkSubmitted("BANK-REF-1", Now);

        var expected = new List<(PaymentStatus? From, PaymentStatus To)>
        {
            (null, PaymentStatus.Pending),
            (PaymentStatus.Pending, PaymentStatus.Dispatching),
            (PaymentStatus.Dispatching, PaymentStatus.Pending),
            (PaymentStatus.Pending, PaymentStatus.Dispatching),
            (PaymentStatus.Dispatching, PaymentStatus.Submitted),
        };
        var actual = payment.AuditEvents.Select(e => (e.FromStatus, e.ToStatus)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Rejected_change_is_not_audited()
    {
        var payment = NewPayment();

        Assert.Throws<DomainException>(() => payment.MarkFailed("not dispatched", Now));

        Assert.Single(payment.AuditEvents); // only the original "created as Pending" event
    }

    private static Payment NewPayment() =>
        new(
            Guid.NewGuid(),
            new NewPayment("INV-1", Money.FromMinorUnits(10000, Currency.FromCode("USD")), new Beneficiary("Acme Ltd", "GB29NWBK60161331926819"), "BANK-A"),
            Now);
}
