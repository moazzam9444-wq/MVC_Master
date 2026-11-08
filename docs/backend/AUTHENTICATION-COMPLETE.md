# KICKS authentication and authorization completion report

Date: 7 November 2026, Asia/Amman. Project: `C:/Users/USER/Desktop/KicksMvc`.

Implemented SQL Server authentication, account approval and role-based dashboard access using ASP.NET Core Identity, while retaining the KICKS Razor/CSS design. No schema migration, database reset or administrator account creation was performed. The original `C:/Users/USER/Desktop/Kicks` frontend was preserved.

## Architecture and SQL integration

- `UserManager<User>` creates accounts and hashes passwords using the standard Identity password hasher. `SignInManager<User>` authenticates and signs out through the Identity application cookie. `KicksSignInManager` additionally requires Active status and exactly one recognized Identity role. `KicksClaimsPrincipalFactory` emits authorization role claims exclusively from Identity role membership, removing any role-shaped custom claims.
- The existing `KicksDbContext`, integer-key `dbo.users`, Identity support tables, seeded role definitions and `DefaultConnection` remain authoritative. Development connects to `localhost/Kicks` using Windows Authentication. No entity, mapping, migration, FK or CHECK constraint was changed.
- Dedicated registration/login/review/status ViewModels prevent mass assignment. Submitted IDs, approval stamps, account status and administrator-role requests cannot determine registration state or permissions.
- The existing unique email/normalized-email indexes and Identity validation prevent duplicates, including case variants. SQL unique-key conflicts are handled safely for concurrent registration attempts.
- MVC unsafe actions require anti-forgery validation globally. Native multipart forms carry request tokens. POST-only logout and review/status forms are protected. Redirects accept only MVC-local return URLs; authorization still applies to the destination.
- Passwords require 8–128 characters with an ASCII letter and a digit, matching the existing frontend policy. Passwords are never written to browser storage or application logs. HTML receives an explicitly selected profile DTO and anti-forgery token, never an entity, hash, security stamp or login token.
- Authentication uses an HttpOnly, SameSite=Lax session cookie with a two-hour sliding lifetime. Production requires Secure cookies and HTTPS; Development permits the existing localhost HTTP workflow. Anti-forgery cookies use HttpOnly and SameSite=Strict.
- Identity security stamps and role claims refresh on every authenticated request. Active status, exact role membership and lockout are checked each time, so deactivation, role removal or stamp revocation affects an existing cookie on its next request.
- Five failed attempts lock an eligible account for fifteen minutes. Registration/login POSTs also have a shared per-remote-IP fixed-window limit of thirty attempts per minute, with HTTP 429 on rejection. Login failures use a generic message for incorrect credentials, pending/rejected/inactive accounts and lockout.
- No automatic migration or account seed runs at startup. Authentication/review HTML is marked no-store. Browser back/forward cache restores reload authentication presentation.

## Registration and approval policy

The inspected `account.js`, `auth.js`, registration notice and admin review code consistently required approval for **both** account types. That policy was preserved; no product-rule ambiguity required a new decision.

| Registration | Stored state | Identity role | Sign-in |
|---|---|---|---|
| Player | Pending; requested_role=player; review fields null | None | Blocked until approved |
| Field owner | Pending; requested_role=owner; private proof required; review fields null | None | Blocked until approved |
| Administrator | Public registration rejected | Cannot be assigned by public registration/review | Controlled provisioning only |

The administrator dashboard's existing Users tab now fetches a protected Razor account-review panel from SQL. The reviewer explicitly approves/rejects requests and chooses player or owner. Assigning owner requires a stored proof. Approval, one-role assignment, reviewer ID/time and security-stamp update commit atomically. Rejection records the reviewer/time and assigns no role. Pending review only accepts unassigned accounts. Existing approved player/owner accounts may be deactivated/reactivated; administrator access cannot be changed through this screen.

Review commands use the existing Identity concurrency stamp plus a serializable transaction and update lock on the target user. Stale commands return a review notice without applying changes. The existing unique `AspNetUserRoles.UserId` index remains the database guard against multiple global roles. This phase does not implement general reassignment of already-approved accounts.

SQL permits Pending, Active, Rejected and Inactive. Suspension is represented by Inactive; all states other than Active are denied. Requested role remains an audit of the registration request, independently of the administrator's selected Identity role.

## Ownership documents

Owners must upload PDF, JPG/JPEG, PNG or WebP proof up to 1 MB. The server checks the size, filename, extension, MIME and file signature; request/form body limits also apply. Files use generated opaque keys under `App_Data/ownership`, outside `wwwroot`. SQL `ownership_documents` stores the user FK, original safe filename, MIME, size and private storage key. Registration and metadata commit together, with file cleanup if the operation fails.

Only authenticated administrators can download proof through `/Pages/Auth/accounts/documents/{id}`. Downloads use attachment disposition, application/octet-stream, nosniff and no-store. Private files are excluded from source control and publishing. Back up this directory together with SQL data. Signature checks are not a malware scanner; production document scanning and a durable managed storage provider remain deployment enhancements.

## Login, navigation and authorization

- Existing `/Pages/Auth/login.html` and `/Pages/Auth/register.html` serve real GET/POST MVC actions. Validation summaries use the existing card and error classes. The animated mode switch, password reveal, role selector, upload widget and responsive layout remain.
- Successful player login defaults to Home; owner/admin login defaults to the matching dashboard. Safe local return URLs are honored; cross-role destinations still deny access.
- `DashboardController` uses separate `[Authorize(Roles=...)]` attributes for player, owner and admin. Anonymous visits challenge to login; wrong-role visits reach a 403 access-denied page. Account review/status/proof endpoints require admin.
- Public catalogue and information pages remain public. Business action persistence is outside this phase and has no newly exposed server endpoints.
- Each existing page using `account.js` embeds a small server-authenticated profile snapshot. `KicksAuth.current()` reads that snapshot; browser users, passwords, role values and sessionStorage cannot authenticate or authorize a request. Logout submits a token-protected POST.
- Dashboard profile display reads SQL-backed profile data. Browser-only profile/password editing is disabled pending its dedicated backend implementation. Other domain prototypes retain their storage and presentation behavior; their data is not a server authorization source.

## First administrator provisioning — prepared, not executed

There are no administrator accounts in SQL after this phase. Pending registrations cannot be approved through the real application until the owner explicitly authorizes and provisions the first administrator.

After obtaining that approval, open an **interactive local PowerShell terminal** in `C:/Users/USER/Desktop/KicksMvc` and run:

```powershell
dotnet run --project KicksMvc.csproj --launch-profile KicksMvc -- --provision-admin
```

The command requires Development and an interactive console. It prompts for name, email, a hidden password and confirmation, then requires the exact text `CREATE ADMIN`. It uses the same validators/hasher and commits the new Active account and admin Identity membership in one transaction. It refuses when an administrator already exists, does not promote an existing account, and does not accept a password on the command line or from source/configuration. It exits without starting the web server. Ordinary F5/startup never runs this operation.

The inherited `scripts/create-admin.cjs` belongs to the old browser demo; it cannot provision SQL Identity accounts. Do not use demo credentials as the first administrator's password.

## Verification results

| Verification | Result and limits |
|---|---|
| Entire solution Release build, including integration harness | Passed, zero warnings/errors |
| SQL-backed integration assertions | 82 passed, including final cleanup/preservation assertion |
| Actual browser workflow assertions | 13 passed in headless Microsoft Edge |
| Existing MVC routes | All 17 public routes rendered; all three dashboards challenged anonymously; player/owner rendered with real cookies and admin rendered with an isolated test principal |
| Registration | Player/owner, proof requirement, missing fields, password/confirmation, normalized duplicate email, admin injection and ignored ID/status/approval input exercised |
| Documents | Missing, spoofed and oversized proof rejected; valid private proof persisted and downloaded by test reviewer; public static retrieval denied |
| Login/logout | Valid credentials, wrong password, Pending/Rejected/Inactive, lockout, local/external returns, cookie logout and post-logout denial exercised |
| Authorization | Both player/owner own dashboards allowed; cross-role dashboards and admin account endpoints denied; forged local/session storage rejected in real browser |
| Approval and status | Approve both roles, reject, proof guard, admin-assignment rejection, stale command, deactivate/reactivate, reviewer audit and session revocation exercised |
| CSRF and throttling | Missing tokens rejected for registration, login, logout and review; authentication throttle produced HTTP 429 |
| Password persistence | Real SQL hash verified through Identity password verification; no credential/stamp fields in rendered profile |
| Database preservation | Test cleanup restored exact table counts and checksums; final catalog comparison preserved 35 tables, 76 FKs and existing schema; no unrelated domain rows changed |
| Frontend preservation | Original Kicks project hash/file inventory unchanged; desktop/mobile auth screenshots inspected; no mobile horizontal overflow or authentication-flow JS errors |
| EF checks | Offline model regression, existing live schema verification and no-pending-model-change check passed |

The integration harness uses TestServer with real SQL and Identity stores/cookies. A synthetic administrator principal exists **only inside that test host**, referencing an isolated test user for reviewer FK integrity; no administrator account or admin role membership is inserted. Real administrator-cookie login and interactive provisioning were **not** exercised, in accordance with the instruction not to create an administrator. Approval controller/database behavior and protected admin rendering/downloads were exercised with that test-only principal. No test authentication handler is compiled into the MVC application.

Browser tests create only uniquely marked player/owner records, approve those records through a test-fixture helper, and remove them and their private files in `finally`. The integration suite exercises the actual review endpoints. Tests do not reseed identity counters, so normal identity gaps from deleted isolated test records remain. The old browser-auth regression suites are historical prototype tests and were not claimed as passing for the new SQL authentication model.

Evidence: `.local/auth-phase/integration.log`, `browser.log`, desktop/mobile PNGs, database-before/after snapshots and preservation records. The directory is ignored and may contain test-only verification artifacts.

### Reproduce verification

```powershell
dotnet build KicksMvc.sln --configuration Release
dotnet run --project tests/Auth.Integration/Auth.Integration.csproj --configuration Release
node tests/ef-model-preparation-check.cjs
node tests/database-setup-check.cjs
dotnet ef migrations has-pending-model-changes --context KicksDbContext --configuration Release --no-build
```

Run browser checks with the development app on port 5612:

```powershell
dotnet run --project KicksMvc.csproj --launch-profile KicksMvc --urls http://127.0.0.1:5612
# In another terminal:
node tests/auth-browser.cjs
```

`KICKS_TEST_URL` may select another origin. Set `KICKS_PLAYWRIGHT_PATH` to a Playwright module path if the bundled local runtime path differs; Microsoft Edge must be installed. Run tests against the local development database. The historical database-setup check specifically asserts the original empty user/domain-table setup and will need a new baseline once real accounts/domain records are intentionally added.

## Manual end-to-end walkthrough

1. Start the application with the existing KicksMvc launch profile and browse `http://127.0.0.1:5602/index.html`.
2. Register a player through Create account. Verify the pending notice and that login is blocked before approval.
3. Register a field owner with a PDF/image license up to 1 MB. Verify missing/invalid proof and password mismatch errors.
4. After separately authorizing and provisioning the first administrator as described above, sign in with that account and open Dashboard → Users.
5. Download the owner's proof, select the assigned player/owner role and approve. Try rejecting another isolated request. Check the review status and audit display.
6. Sign out, then sign in with each approved account. Player lands on Home; owner lands on the owner dashboard. Use the account navigation to open its dashboard/profile.
7. Try another role's dashboard URL; it must deny access. In a private/anonymous window it must redirect to login. Browser-storage edits must not grant access.
8. With an owner logged in in a separate browser, deactivate it from admin Users. Its next request must return to login. Reactivate it and confirm a fresh login works.
9. Enter an incorrect password five times for an approved isolated test account. Correct credentials must also fail during the fifteen-minute lockout. Use separate accounts or wait for expiry while testing further flows.
10. Log out and reload the dashboard URL. Confirm access requires login again. Check `dbo.users`/`AspNetUserRoles` in SQL Server for status, single role, normalized email and an Identity hash; do not copy hashes into screenshots or client code.

## Remaining scope and next phase

No email confirmation, password recovery, profile editing, MFA, production malware scanning, business CRUD or production deployment was requested or implemented. Email is the login identifier, with product approval as the gate; email ownership is not verified yet. The first real administrator remains intentionally unprovisioned. Production should supply its connection string, HTTPS hosting and appropriate durable Data Protection/file storage settings.

Recommended next backend phase: field-owner field management with SQL-backed owner scoping, server validation and private/public image storage rules. Establish owner/resource authorization and concurrency practices there before migrating bookings and payments.

## Maintained file inventory

New files:

- `Controllers/AccountsController.cs`.
- `ViewModels/AuthenticationViewModels.cs`.
- `Services/Authentication/AccountAccess.cs`, `KicksClaimsPrincipalFactory.cs`, `LetterPasswordValidator.cs`, `OwnershipDocumentStore.cs`, `DevelopmentAdminProvisioning.cs`.
- `Views/Shared/_AuthState.cshtml`, `_Accounts.cshtml`; `Views/Auth/AccessDenied.cshtml`.
- `tests/Auth.Integration/Auth.Integration.csproj`, `tests/Auth.Integration/Program.cs`, `tests/auth-browser.cjs`, `tests/auth-browser-data.ps1`.
- This completion report.

Modified files:

- `.gitignore`, `KicksMvc.csproj`, `KicksMvc.sln`, `Program.cs`, `README.md`.
- `Controllers/AuthController.cs`, `Controllers/DashboardController.cs`, `ViewModels/DashboardPageViewModel.cs`.
- `Views/Auth/Login.cshtml`, `Register.cshtml`.
- Server-authentication snapshot/script-version integration in `Views/Home/{Index,About,Contact}.cshtml`, `Views/Fields/{Index,Details}.cshtml`, `Views/OpenGames/{Index,Details}.cshtml`, `Views/Tournaments/{Index,Details}.cshtml`, `Views/Teams/Index.cshtml`, `Views/Challenges/Index.cshtml`, `Views/Dashboard/{Admin,Owner,Player}.cshtml`, `Views/Demo/{Catalogue,Simulation}.cshtml`, `Views/Meetings/Legacy.cshtml`.
- `wwwroot/js/account.js`, `auth.js`, `dashboard.js`, `dashboard-overview.js`.

No existing CSS, assets, schema SQL, migration, entity, DbContext or original frontend files changed. Build outputs and ignored evidence are excluded from this inventory.
