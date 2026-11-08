# MVC migration and backend handoff

## Preserved behavior
All 20 frontend/documentation pages are real MVC routes returning compiled Razor views. The original route names are retained to keep URL-based login returns, identifiers, dashboard hashes and cross-page links intact. Original CSS/JS assets live under wwwroot. Razor @ escaping is reversed by Razor at render time; all initial view responses were compared against the source HTML.

The full demo is explicitly available on 127.0.0.1 or localhost port 5602. It does not seed on startup or overwrite an occupied browser store. The old origin on 5502 and its IndexedDB/localStorage remain independent. Use the setup page to initialize the new origin.

## Migration fixes
The MVC template layout/tag helpers are not injected into the existing forms. Database CSS/JS and the standalone SQL file sync to wwwroot during builds. SQL files have a text/plain static MIME mapping. Local demo activation recognizes the new port. Launchers start dotnet instead of the former Node static host. Tests resolve frontend source paths under wwwroot and verify real MVC routes. A laptop-height registration card no longer sticks behind the fixed navigation.

## Backend foundations
The 27 entity classes and KicksDbContext map the current 230 columns and 68 relationships, including composite tournament links, frozen booking captain shares and platform-only tournament receipts. Context construction and model verification do not contact a database. Startup never calls EnsureCreated or Migrate. Database context registration is opt-in; there are no checked-in connection passwords. User secrets can store the future connection string.

Controllers are currently presentation routes. DashboardPageViewModel describes presentation only and grants no privileges. Services/Contracts is reserved for application services. Browser KicksStore and KicksAuth are the current adapter boundary; their asynchronous method signatures can be replaced progressively with authenticated server calls. Preserve DOM hooks and the asynchronous error/checkout contract while doing so.

## Next backend steps
1. Configure a separate development database from the reviewed SQL schema or a reviewed initial migration; do not create an empty database over existing data.
2. Implement server authentication and roles, owner/captain ownership checks, approved signup and private roster/message queries. Keep password hashes/salts out of responses and use anti-forgery validation on cookie-authenticated mutations.
3. Implement account, venue, team, booking, challenge, tournament, payment and message application services. Use transactions for venue/team overlaps, capacity, duplicate requests, draw locks, hold expiry, refund idempotency and split totals.
4. Replace browser persistence behind KicksStore/KicksAuth with the new endpoints. Keep browser demo fixtures separate from server data; importing legacy string IDs requires a mapping.
5. Add server-side tests for the same workflows before treating the app as a shared multi-user booking service. Payments remain simulations until provider integration is explicitly implemented.

## Checks
Build, EF-model verification, frontend regression results, route parity and responsive/accessibility evidence are in .local/mvc-qa. The tests use isolated contexts/fixtures, not the user's demo store.
