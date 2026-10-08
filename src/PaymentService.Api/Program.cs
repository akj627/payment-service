using System.Text.Json.Serialization;
using PaymentService.Api.Batches;
using PaymentService.Core.Batches;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Payments")!);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BatchSubmissionService>();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Creates the SQLite database on first run. Production would use EF Core migrations instead.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.EnsureCreated();
}

app.UseExceptionHandler();

app.MapGet("/", () => "Payment Batch Service");
app.MapBatchEndpoints();

app.Run();
