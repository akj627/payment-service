using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Core.Abstractions;
using PaymentService.Infrastructure.Banking;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPaymentBatchRepository, PaymentBatchRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Singleton so it remembers idempotency keys between calls, like a real bank.
        services.AddSingleton<IBankGateway>(new SimulatedBank(responseDelay: TimeSpan.FromMilliseconds(200)));

        return services;
    }
}
