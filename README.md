# Payment Batch Service

Accepts batches of outbound payments from corporate treasury teams, validates them, sends each payment to its bank, and tracks every payment to a final outcome with a full audit trail.

- **Design and decisions:** [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- **Work items delivered and future work:** [docs/BACKLOG.md](docs/BACKLOG.md)

## Run it

Requires the **.NET 10 SDK**. No database or other services to install: the app creates a SQLite file (`payments.db`) on first run.

```bash
dotnet build
dotnet test
dotnet run --project src/PaymentService.Api
```

- API: `http://localhost:5238`
- Swagger UI: `http://localhost:5238/swagger` (opens automatically when you press F5 in Visual Studio)
- Ready-made requests: `src/PaymentService.Api/PaymentService.Api.http` (Visual Studio or VS Code REST Client)

To start with an empty database, stop the app and delete `src/PaymentService.Api/payments.db`.

## Try it

1. Send **"Batch showing every outcome"** from the `.http` file (or paste its body into Swagger, with any `Idempotency-Key`).
2. Copy the `id` from the 201 response and call `GET /batches/{id}`.
3. Call it again about 10 seconds later. The four payments end up:

| Payment | Beneficiary account ends in | Simulated bank | Final status |
|---|---|---|---|
| INV-2001 | (anything else) | accepts | **Submitted** |
| INV-2002 | `REJECT` | rejects | **Failed** |
| INV-2003 | `LOSTREPLY` | accepts but the reply is lost | **Submitted** after a safe retry (executed once) |
| INV-2004 | `TIMEOUT` | never answers | **NeedsReview** after 3 attempts |

The `auditTrail` in the response shows every status change with its reason. Sending the same request again with the same `Idempotency-Key` returns the original batch (200) instead of creating a new one.

## API

| Endpoint | Purpose |
|---|---|
| `POST /batches` | Submit a batch. Requires the `Idempotency-Key` header. Returns **201** (created), **200** (same key seen before: original batch) or **400** (every validation problem, nothing saved). |
| `GET /batches/{id}` | Batch status, payments and audit trail. **404** if not found. |

Amounts are JSON strings (`"1250.50"`), never numbers.

## Tests

```bash
dotnet test
```

60 tests:
- domain (money, state machine, batch rules)
- persistence against real SQLite in memory
- batch submission (idempotency, validation)
- dispatch (every bank outcome, no double payment after a lost reply, audit trail saved)

## Project layout

| Project | Contents |
|---|---|
| `src/PaymentService.Core` | Domain model, state machine (Stateless), validation, submission and dispatch logic, interfaces |
| `src/PaymentService.Infrastructure` | EF Core + SQLite repositories, simulated bank |
| `src/PaymentService.Api` | Minimal API endpoints, background dispatch worker, Swagger |
| `tests/PaymentService.Tests` | xUnit tests |

## Assumptions

- A single instance runs at a time. The SQLite file is local to the process, and dispatch assumes one worker.
- No authentication (see the architecture doc for how it would be added).
- All times are UTC. Payments are sent as soon as they are accepted (no cut-off times or future dates).
- A batch holds 1–1,000 payments and is accepted whole or not at all.
- Supported currencies: USD, EUR, GBP, CHF, CAD, SGD, INR, JPY, BHD.
- **Submitted** means the bank accepted the instruction, not that the money has settled.
- The simulated bank remembers idempotency keys in memory only, so it resets on restart.

## Use of AI

I used Claude as a pair programmer for scaffolding, test cases and reviewing the documentation. The design decisions, scope and trade-offs are mine, and I reviewed and adjusted all generated code.
