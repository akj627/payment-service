# Payment Batch Service

Accepts batches of outbound payments from corporate treasury teams and dispatches them to banking partners.

Work in progress. Full documentation will be in `docs/ARCHITECTURE.md`.

## Projects

| Project | Purpose |
|---|---|
| `src/PaymentService.Api` | HTTP API and background worker host |
| `src/PaymentService.Core` | Domain models, business rules, service interfaces |
| `src/PaymentService.Infrastructure` | Database access and bank adapters |
| `tests/PaymentService.Tests` | Unit and integration tests |

## Build and run

Requires the .NET 10 SDK.

```bash
dotnet build
dotnet test
dotnet run --project src/PaymentService.Api
```
