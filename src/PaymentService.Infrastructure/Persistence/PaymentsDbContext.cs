using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PaymentService.Core.Domain;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<PaymentBatch> Batches => Set<PaymentBatch>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot compare or sort DateTimeOffset, so store it as a sortable number.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentBatch>(batch =>
        {
            batch.ToTable("Batches");
            batch.HasKey(b => b.Id);
            batch.Property(b => b.IdempotencyKey).IsRequired().HasMaxLength(100);

            // The database, not a check-then-insert in code, guarantees one batch per key.
            batch.HasIndex(b => b.IdempotencyKey).IsUnique();

            batch.HasMany(b => b.Payments).WithOne().HasForeignKey(p => p.BatchId);
            batch.Navigation(b => b.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

            batch.Ignore(b => b.Status); // calculated from the payments, never stored
        });

        modelBuilder.Entity<Payment>(payment =>
        {
            payment.ToTable("Payments");
            payment.HasKey(p => p.Id);
            payment.Property(p => p.Reference).IsRequired().HasMaxLength(35);
            payment.Property(p => p.BankPartner).IsRequired().HasMaxLength(50);
            payment.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

            payment.ComplexProperty(p => p.Amount, amount =>
            {
                amount.Property(m => m.AmountInMinorUnits).HasColumnName("AmountMinorUnits");
                amount.Property(m => m.Currency)
                    .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                    .HasColumnName("Currency")
                    .HasMaxLength(3);
            });

            payment.ComplexProperty(p => p.Beneficiary, beneficiary =>
            {
                beneficiary.Property(b => b.Name).HasColumnName("BeneficiaryName").HasMaxLength(140);
                beneficiary.Property(b => b.AccountNumber).HasColumnName("BeneficiaryAccount").HasMaxLength(34);
            });

            payment.HasMany(p => p.AuditEvents).WithOne().HasForeignKey(e => e.PaymentId);
            payment.Navigation(p => p.AuditEvents).UsePropertyAccessMode(PropertyAccessMode.Field);

            payment.Ignore(p => p.IsFinished);
        });

        modelBuilder.Entity<AuditEvent>(auditEvent =>
        {
            auditEvent.ToTable("AuditEvents");
            auditEvent.HasKey(e => e.Id);
            auditEvent.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
            auditEvent.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20);
            auditEvent.HasIndex(e => e.BatchId);
        });
    }
}
