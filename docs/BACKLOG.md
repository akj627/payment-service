# Backlog

Work was planned as small Jira-style stories and committed one story at a time, so the git history follows this list.

Numbers were assigned when a story was created, so the order of delivery differs from the numbering. **PAY-16** (state machine with the Stateless NuGet package) and **PAY-17** (Swagger) were added mid-sprint as design decisions were made, and were delivered between earlier-numbered stories.

## Delivered

| Item | Summary | Commit |
|---|---|---|
| PAY-1 | Solution with Api, Core, Infrastructure and Tests projects | `PAY-1: Create solution with Api, Core, Infrastructure and Tests projects` |
| PAY-2 | `Money` and `Currency`: exact minor-unit amounts, no rounding, no mixing currencies | `PAY-2: Add Money and Currency with exact minor-unit amounts` · `PAY-2: Make Currency a record; remove implementation-detail test` |
| PAY-3 | Minimal domain models: `PaymentBatch`, `Payment`, `Beneficiary`, `AuditEvent`; derived batch status | `PAY-3: Add minimal Payment, PaymentBatch, Beneficiary and AuditEvent models` |
| PAY-16 | Payment state machine with the Stateless NuGet package; every transition audited | `PAY-16: Payment state machine using Stateless` |
| PAY-4 | EF Core + SQLite behind repository interfaces; unique idempotency key; audit saved in the same transaction | `PAY-4: EF Core SQLite persistence with repository and audit trail` |
| PAY-5 | `POST /batches` (idempotent, whole-batch validation) and `GET /batches/{id}` | `PAY-5: Batch submission and status API with idempotency and validation` |
| PAY-6 | Background dispatcher, simulated bank, safe retries with the same key, NeedsReview after 3 attempts | `PAY-6: Background dispatcher with simulated bank, safe retries and NeedsReview` |
| PAY-17 | Swagger UI for local development | `PAY-17: Swagger UI for local development` |
| PAY-7 | Architecture document, backlog and README | `PAY-7: Architecture document, backlog and README` |

## Future work

### Must have before handling real money

| Item | Summary | Why |
|---|---|---|
| PAY-18 | **Leases for multiple workers.** Atomic claim (`LeaseOwner`, `LeaseExpiresAt`); reclaim payments stuck in Dispatching. | Today only one worker can run, and a crash leaves a payment in Dispatching until someone looks. |
| PAY-19 | **Optimistic concurrency.** `Version` column on payments. | A worker whose lease expired must not overwrite a newer result. |
| PAY-20 | **Azure SQL and EF Core migrations.** Replace SQLite and `EnsureCreated`. | Concurrent writers, high availability, backups, controlled schema changes. |
| PAY-21 | **Authentication and authorisation.** Entra ID or Okta, client credentials for ERPs, roles, caller recorded as the audit actor. | Nobody should be able to submit or view payments anonymously. |
| PAY-22 | **Request hash on idempotency keys.** Same key with a different body returns 422. | A reused key must not silently return an unrelated batch. |
| PAY-23 | **Observability.** OpenTelemetry traces, correlation ids, metrics, health checks, alerts, runbooks. | Operators need to see stuck, failing and NeedsReview payments before customers do. |
| PAY-31 | **Sanctions and compliance screening** before dispatch (OFAC, EU, UK and UN lists); possible matches held for compliance review. | Paying a sanctioned party is a legal violation and usually can't be undone. The bank's own screening is a second line, not a substitute. |
| PAY-34 | **Dispatch settings from configuration.** Polling interval, payments per tick and retry limit in a `Dispatch` section of `appsettings.json` (per environment), bound to an options class instead of constants. | Operations must be able to tune dispatch per environment and bank load without a code change and redeploy. |

### Needed to run at scale

| Item | Summary | Why |
|---|---|---|
| PAY-24 | **Outbox + Azure Service Bus; separate dispatcher deployable.** | Scale API and dispatch independently; keep bank credentials out of the public API. |
| PAY-25 | **Real bank adapters.** ISO 20022 pain.001 / host-to-host / bank APIs; per-bank configuration and throughput limits. | Replace the simulated bank. |
| PAY-26 | **Settlement tracking and reconciliation.** pain.002 status reports and webhooks; `Submitted → Settled / Returned`; camt.053 statement matching. | "Submitted" only means the bank accepted the instruction. |
| PAY-27 | **Retry backoff and scheduling.** Exponential backoff with `NextAttemptAt`, per-bank retry policy, cut-off times, holiday calendars, requested execution date. | Avoid hammering a struggling bank; send payments on the right business day. |

### Product growth

| Item | Summary | Why |
|---|---|---|
| PAY-29 | **Payment operations UI.** Dashboard for batches and payments, search and filter by status, payment detail with audit history, and a NeedsReview workflow. | Operators need to see and resolve payments, especially NeedsReview, without calling the API. |
