using PaymentService.Core.Domain;

namespace PaymentService.Tests.Domain;

public class PaymentBatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_batch_has_pending_payments_and_is_processing()
    {
        var batch = new PaymentBatch("key-1", [Payment("INV-1", 10000), Payment("INV-2", 2550)], Now);

        Assert.Equal(BatchStatus.Processing, batch.Status);
        Assert.Equal(2, batch.Payments.Count);
        Assert.All(batch.Payments, p => Assert.Equal(PaymentStatus.Pending, p.Status));
        Assert.All(batch.Payments, p => Assert.Equal(batch.Id, p.BatchId));
    }

    [Fact]
    public void New_payment_records_an_audit_event()
    {
        var batch = new PaymentBatch("key-1", [Payment("INV-1", 10000)], Now);

        var auditEvent = Assert.Single(batch.Payments[0].AuditEvents);
        Assert.Null(auditEvent.FromStatus);
        Assert.Equal(PaymentStatus.Pending, auditEvent.ToStatus);
    }

    [Fact]
    public void Empty_batch_is_rejected()
    {
        Assert.Throws<DomainException>(() => new PaymentBatch("key-1", [], Now));
    }

    [Fact]
    public void Duplicate_reference_is_rejected()
    {
        Assert.Throws<DomainException>(() => new PaymentBatch("key-1", [Payment("INV-1", 100), Payment("INV-1", 200)], Now));
    }

    [Fact]
    public void Zero_amount_is_rejected()
    {
        Assert.Throws<DomainException>(() => new PaymentBatch("key-1", [Payment("INV-1", 0)], Now));
    }

    private static NewPayment Payment(string reference, long cents) =>
        new(reference, Money.FromMinorUnits(cents, Currency.FromCode("USD")), new Beneficiary("Acme Ltd", "GB29NWBK60161331926819"), "BANK-A");
}
