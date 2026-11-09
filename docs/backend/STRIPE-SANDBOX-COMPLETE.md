# Phase 7 — Stripe sandbox integration

Completed and verified on 9 November 2026. **988 workflow assertions passed, zero failed.** Release solution build: zero warnings/errors. This is a TEST-only integration; live credentials are deliberately rejected. No Phase 8 work was started.

## Approved payment policy

Stripe's [supported currencies](https://docs.stripe.com/currencies) and a read-only account Country Spec check confirmed that JOD cannot be presented for this account. The user explicitly approved **1 JOD = 1.40 USD for sandbox only**. KICKS prices remain JOD. Every payment stores its original JOD amount, decimal conversion rate and rounded USD cents, and shows both amounts before Checkout. The conversion uses decimal arithmetic and midpoint rounding away from zero. Missing/invalid rate, non-test credentials or disabled SandboxOnly stops paid operations. The accepted USD range is 50–99,999,999 cents.

New paid individual reservations, paid tournament team entries and accepted challenges with a priced field reserve their time/place for 30 minutes and confirm only after verified payment. Checkout must start at least 30 minutes before the booking starts or tournament registration closes. The challenging team's captain pays the entire challenge reservation; no split charges. Free fields, free tournament entries, fieldless challenge agreements, teams and existing free Open Games remain free. Field listing fees remain deferred. Existing confirmed/registered history is grandfathered and is never charged retroactively.

Players retain the three-hour cutoff for cancelling their own confirmed reservations. Owners may cancel future own-field reservations and complete ended confirmed sessions. New full-price paid challenges use these booking rules too. Unpaid holds can be abandoned without a charge. Paid cancellations/withdrawals retain the payment and set FinancialReview, visible in booking details/history and the administrator's SQL payment review interface. There are no automatic refunds, commissions, payouts, settlement or bank-balance claims. Paid withdrawn entries require manual financial review before re-registration, preventing accidental repeated charges.

## Architecture and security

Official Stripe.net **53.1.0**, Stripe-hosted Checkout and card-only eligible methods are used; adaptive pricing is disabled. The original payments table is a legacy demo ledger with demo-only status/distribution constraints, including a 100% tournament allocation. Those records, constraints and relationships remain unchanged. Three additive tables provide a distinct sandbox gateway ledger, durable Checkout attempts and webhook event receipts. No demo payment is fabricated.

Booking/tournament admission and payment obligations are committed together. Attempt IDs are persisted before the network call and become Stripe idempotency keys; retries, simultaneous clicks and a lost API response resume the same session. Network calls do not hold SQL transactions. Cancellation/expiry closes open sessions; failed close calls retry. A card decline leaves Checkout and its Pending hold retryable until cancellation/expiry, and never confirms anything.

The SQL hold is authoritative. Stripe's minimum session lifetime needs API transport margin, so Checkout has a 31-minute outer expiry and the worker explicitly expires it at the SQL 30-minute deadline. Availability/capacity stop counting expired holds even while maintenance is stopped. A payment completed after the SQL deadline is recorded as Paid with FinancialReview and cannot reclaim the released reservation/place. No automatic refund is attempted.

All field writers retain the shared SQL UPDLOCK/HOLDLOCK field-row gate and half-open interval overlap checks. Payment resource mutations follow teams → field → tournament → payment ordering where applicable. Expired Stripe bookings leave the blocking status set inside this same gate before a new booking can reuse their exact start; the existing unique booking index is preserved. Legacy Pending records without a gateway obligation retain their prior conservative blocking behavior.

Only `/payments/stripe/webhook` is exempt from CSRF, because it requires a verified raw-body Stripe signature with timestamp tolerance. All player POSTs retain anti-forgery protection. The signing secret is separate from the API key. Event receipts use a unique event ID plus a transactional application lock. The handler verifies test mode, session/attempt/payment IDs, payer, database scope, client reference, currency and amount against SQL and a fresh official Stripe Session retrieval. Fulfilment and receipt insertion are atomic. Duplicate/out-of-order events cannot overwrite Paid or re-fulfil cancelled history. Unknown-instance events are ignored without inserting records. Redirects, browser storage and signed success-shaped payloads describing an actually unpaid Stripe Session cannot confirm payment.

Identity/account-status enforcement remains unchanged. Payment review/poll/checkout/cancel reads the authenticated payer from Identity. Field/tournament eligibility and relevant current captain/roster eligibility are checked again under the resource gates. Administrator payment history/review is read-only, bounded to 20 rows per page and role-protected. Old browser-based revenue/commission totals are replaced in this migrated section. No card details, SDK payloads or raw Stripe errors are stored or logged.

## Credentials and local startup

The user entered both keys in a separate local PowerShell window using hidden input. `scripts/setup-stripe-test-secrets.ps1` validates test prefixes before networking, checks test mode through a read-only Stripe API call and sends JSON to native Secret Manager over stdin. No credentials are placed in arguments, source, reports, temporary plaintext files or chat. User Secrets stores `Stripe:PublishableKey`, `Stripe:SecretKey` and the distinct `Stripe:WebhookSecret` outside Git. User Secrets is development storage, not a production secret vault.

The local official Stripe CLI **1.55.8** was downloaded from its official GitHub release and verified against both the release SHA-256 digest and published Windows checksums. No login/key command-line argument is used. The explicit Development listener supplies the test API key only in the child environment, consumes CLI output privately, and stores the webhook signing secret through Secret Manager stdin. Its status output contains no key. The isolated listener checks the restored database and forwards only to 5621; the normal local listener checks Kicks/schema and uses the trusted configured loopback return origin.

For ordinary development, open PowerShell in the project folder and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet bin/Release/net10.0/KicksMvc.dll --stripe-local-listener
```

Wait for **READY**, keep that terminal open, then start/restart the MVC app in Visual Studio with the KicksMvc profile. Use `http://127.0.0.1:5602/index.html`. The default webhook URL is `http://127.0.0.1:5602/payments/stripe/webhook`. The listener was verified against Kicks and stopped after verification; it must be started for subsequent local payment tests. If the ignored local CLI binary is absent on another machine, install the [official Stripe CLI](https://docs.stripe.com/cli/install) on PATH. Do not substitute an API key for the signing secret. For public hosting, a separately configured HTTPS webhook and secure deployment secrets would be required; no deployment was performed.

`--verify-stripe-sandbox` is an explicit read-only Development preflight that exits before database registration and reports only safe test/currency booleans. Ordinary startup never migrates, seeds accounts or provisions an administrator.

## Migration and preservation

Migration **20261109124741_StripeSandboxPhase7** adds gateway_payments, stripe_checkout_attempts and stripe_webhook_events, one nullable tournament_registrations.payment_deadline column, seven secondary indexes, four NO ACTION FKs, four new checks and a reviewed Pending extension to the registration status check. Existing table columns/indexes/FKs and the demo payments table are preserved. Down refuses to discard gateway/event history or entry payment deadlines/Pending states; safe empty Down/Up and populated refusal were actually rehearsed.

A fresh COPY_ONLY/CHECKSUM compressed backup was created, verified using RESTORE VERIFYONLY, SHA-256 checked and restored into **Kicks_BookingTest_20261109_1fc528d8** with independent MDF/LDF files. DBCC CHECKDB and before/after fingerprints passed. The rehearsal included nonempty historical booking/tournament/team/registration fixtures; their existing values and relationships survived. Only those fixtures were removed.

Backup: `C:\Users\USER\Desktop\KicksMvc-safety\stripe-phase-20261109_1fc528d8\Kicks-fresh-stripe-phase.bak`.

SHA-256: `7922F9F26D95A76A0FEAACACF17B7A6F2A95533EC2C6E0048B2B68D866719C98`.

The external directory also contains the verified Phase 6 Git bundle, private application files and development configuration checkpoint. Permissions are scoped to the current user and SQL Server service on that new safety directory. Previous checkpoints/backups/restored databases were not overwritten or removed.

Only after the full isolated test run and preservation gates passed was the migration applied to original Kicks. Final audit: **35 → 38 physical tables, 76 → 80 FKs, 18 → 19 rows solely through migration history**. Every existing account/business row and original column fingerprint is unchanged, including password hashes, security stamps, roles, approvals, relationships and historical values. All original identity counters match the preserved Phase 6 checkpoint; no reseeding/resetting occurred. The real administrator and approved player remain unchanged, with no additional administrator created. Original new gateway/attempt/webhook tables contain zero records.

All **4,925** original static frontend files, existing MVC App_Data files and private development configuration hashes match. Mutating tests exclusively used the restored database with the existing C#/PowerShell/JavaScript fail-closed gates. Temporary accounts, fields, bookings, entries, teams and gateway rows were removed by scoped fixture cleanup. The newly introduced isolated receipt rows were inventoried and removed by explicit IDs after all test hosts/listeners stopped. Provider-side Stripe sandbox transaction history is retained; no refund/delete/settlement operation was issued. Both databases passed final DBCC CHECKDB. Twelve anonymous read-only GET/authorization smoke checks passed against the migrated original application, followed by another preservation audit.

For recovery, preserve new financial history and prefer a reviewed forward fix. Do not deploy old booking code over active payment holds. A full recovery should first restore the verified backup to another isolated database and validate it; replacing Kicks or discarding newer history requires a separate recovery decision. No original downgrade/restore/reset was executed.

## Executed verification

| Suite | SQL | Edge | Failed |
| --- | ---: | ---: | ---: |
| Phase 1 Authentication | 84 | 56 | 0 |
| Phase 2 Fields | 52 | 33 | 0 |
| Phase 3 Bookings | 111 | 46 | 0 |
| Phase 4 Teams | 91 | 30 | 0 |
| Phase 5 Games/challenges | 109 | 47 | 0 |
| Phase 6 Tournaments | 153 | 45 | 0 |
| Phase 7 Stripe | 77 | 39 | 0 |
| Isolation guards | 15 | — | 0 |
| **Total** | **692** | **296** | **0** |

Total: **988**. Legacy immediate-confirmation booking/challenge/tournament fixtures now exercise free flows, with no production payment bypass. Priced flows are covered by the separate Stripe suites. No test claims an unexecuted payment succeeded.

Stripe verification executed actual hosted TEST Checkout for booking, paid tournament entry and the challenging captain's full two-hour reservation; actual signed CLI webhooks; the official declining card; cancellation/expiry; SQL reload; server cents/FX snapshots; forged price/payer; unauthorized/CSRF/history access; duplicate submissions/events; tampered amount/currency/references; concurrent Checkout and reservation requests; recovery after loss of an actual API response; paid last-place registration races; and capacity/availability while maintenance is stopped. Successful payments were never manufactured in SQL.

The delayed-payment browser test paused only the dedicated isolated CLI listener, completed an actual Stripe payment, aged only its isolated unpaid hold, let another player reserve the released exact start, then re-delivered actual provider events through signature verification. The late payment went to review without creating an overlap; duplicates did not re-fulfil it. The SQL-only aging fixture changes deadlines, never payment success. Administrator UI testing used the existing isolated test-host principal over its loopback bridge; no real administrator password/account change was needed, and the ordinary MVC app rejected that principal's test credential.

Payment review and administrator history layouts passed 1440/768/390 overflow checks and screenshot inspection. All six prior browser suites passed their responsive checks. Final browser tests reported no JavaScript runtime errors. Additional checks outside the 988 count include Release build, no pending EF model changes, offline SQL/model parity with 22 behavior checks, backup/restore/bundle verification, migration/recovery rehearsal, raw schema/data/counter/file audits, DBCC and 12 original read-only smoke checks.

Development verification found and fixed an expired-hold/unique-index mismatch: availability was free before maintenance while the old Pending row still occupied IX_bookings_0. Expiration now happens under the shared field gate before reuse. Further hardening preserves durable idempotency parameters, reloads state after network calls, isolates database-scoped events, rechecks captain/roster eligibility, and removes misleading prototype financial totals. Intermediate harness timing/expectation failures were corrected before the final full run; final counts above are the executed final results.

## Files

Added:

- Controllers/PaymentsController.cs; Models/Entities/GatewayPayment.cs; Data/GatewayPaymentMapping.cs.
- Services/Payments/{SandboxSettings,PaymentReservations,StripePaymentService,PaymentExpiryWorker,StripeSandboxPreflight,StripeTestListener}.cs.
- ViewModels/PaymentHistoryViewModels.cs; Views/Payments/Review.cshtml; Views/Shared/_PaymentHistory.cshtml.
- Migration 20261109124741_StripeSandboxPhase7.cs and Designer; wwwroot/css/server-payments.css; wwwroot/js/server-payments.js.
- scripts/setup-stripe-test-secrets.ps1; tests/Auth.Integration/StripeTests.cs; tests/stripe-browser.cjs; this report.

Modified:

- KicksMvc.csproj, appsettings.json, Program.cs, README.md; KicksDbContext/model snapshot; TournamentRegistration.
- BookingService/FieldOccupancy; ChallengeService; TournamentService/RegistrationService/ReadService; BookingsController/TournamentApiController; BookingViewModels.
- Fields Details and shared Bookings Razor; server-bookings/server-activities/server-tournaments JavaScript; dashboard/dashboard-overview JavaScript.
- Integration entry point and booking/phase5/tournament fixtures; booking/activity/tournament browser tests; field browser diagnostics; offline EF parity test.

Evidence is under **.local/stripe-phase/**, ignored by Git: setup status (no values), preflights, SDK/CLI verification, safety.json, backup/restore fingerprints, reviewed migration.sql, rehearsal/Down/refusal/re-up logs, final SQL/browser/isolation logs, screenshots, test-summary.json, before/after audits, original smoke results, build/model checks, receipt cleanup inventory and Git-bundle verification.

Branch: **codex/stripe-sandbox-payments**, based on **aa0da7b**. The final local commit hash is recorded in the local verification summary and user report after commit. No push, merge or history rewrite. Existing phases and original frontend are preserved. Local webhook forwarding must remain running for interactive sandbox testing. Financial review is a persisted flag/read-only queue, not an implemented refund or settlement workflow; live deployment, payouts, split payments and production financial processing remain outside this phase.
