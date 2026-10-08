using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Core.Abstractions;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IPaymentBatchRepository, PaymentBatchRepository>();
        return services;
    }
}
