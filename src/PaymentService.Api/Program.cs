using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Payments")!);

var app = builder.Build();

// Creates the SQLite database on first run. Production would use EF Core migrations instead.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.EnsureCreated();
}

app.MapGet("/", () => "Payment Batch Service");

app.Run();
