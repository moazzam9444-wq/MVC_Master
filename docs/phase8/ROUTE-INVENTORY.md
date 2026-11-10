# Phase 8 route and page inventory

Generated from controller route attributes. Role authorization is enforced by the controller/action and Identity; dashboard fragment routes are listed below.

| Method | Route | Controller | Declared access |
| --- | --- | --- | --- |
| GET | `/Pages/Auth/accounts` | AccountsController | admin |
| POST | `/Pages/Auth/accounts/review` | AccountsController | admin |
| POST | `/Pages/Auth/accounts/status` | AccountsController | admin |
| GET | `/Pages/Auth/accounts/documents/{id:int}` | AccountsController | admin |
| GET | `/api/activities/summary` | ActivitiesApiController | player,owner |
| GET | `/api/games` | ActivitiesApiController | Public |
| GET | `/api/games/{id:int}` | ActivitiesApiController | Public |
| POST | `/games/quote` | ActivitiesApiController | owner |
| POST | `/games` | ActivitiesApiController | owner |
| POST | `/games/{id:int}/join` | ActivitiesApiController | player |
| POST | `/games/{id:int}/leave` | ActivitiesApiController | player |
| POST | `/games/{id:int}/cancel` | ActivitiesApiController | owner |
| POST | `/games/{id:int}/edit` | ActivitiesApiController | owner |
| GET | `/api/challenges` | ActivitiesApiController | player |
| GET | `/api/challenges/{id:int}` | ActivitiesApiController | player |
| POST | `/challenges` | ActivitiesApiController | player |
| POST | `/challenges/{id:int}/{decision:regex(^(accept\|reject\|withdraw)$)}` | ActivitiesApiController | player |
| GET | `/api/activity-fields` | ActivitiesApiController | player,owner |
| GET | `/Pages/Auth/login.html` | AuthController | Public |
| GET | `/Pages/Auth/register.html` | AuthController | Public |
| POST | `/Pages/Auth/register.html` | AuthController | Public |
| POST | `/Pages/Auth/login.html` | AuthController | Public |
| POST | `/Pages/Auth/logout` | AuthController | Authenticated |
| GET | `/Pages/Auth/access-denied.html` | AuthController | Public |
| GET | `/api/fields/{id:int}/availability` | BookingsController | Public |
| GET | `/bookings/quote` | BookingsController | player |
| POST | `/bookings` | BookingsController | player |
| GET | `/bookings` | BookingsController | player,owner |
| GET | `/bookings/data` | BookingsController | player,owner |
| GET | `/bookings/{id:int}` | BookingsController | player,owner |
| POST | `/bookings/{id:int}/cancel` | BookingsController | player,owner |
| POST | `/bookings/{id:int}/complete` | BookingsController | owner |
| GET | `/Pages/Challenges/challenges.html` | ChallengesController | Public |
| GET | `/Pages/Dashboard/player.html` | DashboardController | player |
| GET | `/Pages/Dashboard/owner.html` | DashboardController | owner |
| GET | `/Pages/Dashboard/admin.html` | DashboardController | admin |
| GET | `/Pages/Demo/catalogue.html` | DemoController | Public |
| GET | `/Pages/Demo/demo.html` | DemoController | Public |
| GET | `/docs/database/database.html` | DocumentationController | Public |
| GET | `/Pages/Fields/fields.html` | FieldsController | Public |
| GET | `/Pages/Fields/field-details.html` | FieldsController | Public |
| GET | `/api/fields` | FieldsController | Public |
| GET | `/api/fields/{id:int}` | FieldsController | Public |
| GET | `/api/field-cities` | FieldsController | Public |
| GET | `/media/fields/{key}` | FieldsController | Public |
| GET | `/error` | HomeController | Public |
| POST | `/error` | HomeController | Public |
| GET | `/status/{code:int}` | HomeController | Public |
| GET | `/` | HomeController | Public |
| GET | `/index.html` | HomeController | Public |
| GET | `/Pages/About/about.html` | HomeController | Public |
| GET | `/Pages/Contact/contact.html` | HomeController | Public |
| GET | `/Pages/Meetings/meeting.html` | MeetingsController | Public |
| GET | `/Pages/OpenGames/open-games.html` | OpenGamesController | Public |
| GET | `/Pages/OpenGames/game-details.html` | OpenGamesController | Public |
| GET | `/owner/fields` | OwnerFieldsController | owner |
| GET | `/owner/fields-data` | OwnerFieldsController | owner |
| GET | `/owner/fields/{id:int}` | OwnerFieldsController | owner |
| GET | `/owner/field-amenities` | OwnerFieldsController | owner |
| POST | `/owner/fields` | OwnerFieldsController | owner |
| POST | `/owner/fields/{id:int}` | OwnerFieldsController | owner |
| POST | `/owner/fields/{id:int}/archive` | OwnerFieldsController | owner |
| POST | `/owner/fields/{id:int}/restore` | OwnerFieldsController | owner |
| GET | `/payments/history` | PaymentsController | admin |
| GET | `/api/payments/history` | PaymentsController | admin |
| GET | `/payments/booking/{id:int}` | PaymentsController | player |
| GET | `/payments/tournament/{id:int}` | PaymentsController | player |
| GET | `/payments/{id:guid}` | PaymentsController | player |
| GET | `/api/payments/{id:guid}` | PaymentsController | player |
| POST | `/payments/{id:guid}/checkout` | PaymentsController | player |
| POST | `/payments/{id:guid}/cancel` | PaymentsController | player |
| POST | `/payments/stripe/webhook` | PaymentsController | Public |
| GET | `/Pages/Teams/teams.html` | TeamsController | player |
| GET | `/api/teams` | TeamsController | player |
| GET | `/api/teams/{id:int}` | TeamsController | player |
| POST | `/teams` | TeamsController | player |
| POST | `/teams/{id:int}/edit` | TeamsController | player |
| POST | `/teams/{id:int}/join` | TeamsController | player |
| POST | `/teams/{id:int}/requests/{requestId:int}/{decision:regex(^(accept\|reject)$)}` | TeamsController | player |
| POST | `/teams/requests/{requestId:int}/cancel` | TeamsController | player |
| POST | `/teams/{id:int}/members/{userId:int}/remove` | TeamsController | player |
| POST | `/teams/{id:int}/leave` | TeamsController | player |
| POST | `/teams/{id:int}/transfer/{userId:int}` | TeamsController | player |
| POST | `/teams/{id:int}/logo` | TeamsController | player |
| GET | `/media/teams/{key}` | TeamsController | player |
| GET | `/api/tournaments` | TournamentApiController | Public |
| GET | `/api/tournaments/{id:int}` | TournamentApiController | Public |
| GET | `/api/tournaments/summary` | TournamentApiController | owner,player,admin |
| POST | `/tournaments/quote` | TournamentApiController | owner |
| POST | `/tournaments` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/edit` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/cancel` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/register` | TournamentApiController | player |
| POST | `/tournaments/{id:int}/withdraw` | TournamentApiController | player |
| POST | `/tournaments/{id:int}/draw` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/matches/{match:int}/schedule` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/matches/{match:int}/cancel` | TournamentApiController | owner |
| POST | `/tournaments/{id:int}/matches/{match:int}/result` | TournamentApiController | owner |
| GET | `/Pages/Tournaments/tournaments.html` | TournamentsController | Public |
| GET | `/Pages/Tournaments/tournament-details.html` | TournamentsController | Public |
| GET | `/dashboard/overview` | WorkspaceController | player,owner,admin |
| GET | `/dashboard/records/{kind:regex(^(bookings\|feedback\|fields)$)}` | WorkspaceController | admin |
| POST | `/feedback` | WorkspaceController | Public |
| GET | `/api/site-presentation` | WorkspaceController | Public |
| GET | `/dashboard/settings` | WorkspaceController | admin |
| POST | `/dashboard/settings` | WorkspaceController | admin |
| GET | `/api/messages/contacts` | WorkspaceController | player,owner,admin |
| GET | `/api/messages/{other:int}` | WorkspaceController | player,owner,admin |
| POST | `/messages` | WorkspaceController | player,owner,admin |
| POST | `/messages/{other:int}/read` | WorkspaceController | player,owner,admin |

## Razor views

- `Views/Auth/AccessDenied.cshtml`
- `Views/Auth/Login.cshtml`
- `Views/Auth/Register.cshtml`
- `Views/Challenges/Index.cshtml`
- `Views/Dashboard/Admin.cshtml`
- `Views/Dashboard/Owner.cshtml`
- `Views/Dashboard/Player.cshtml`
- `Views/Demo/Catalogue.cshtml`
- `Views/Demo/Simulation.cshtml`
- `Views/Documentation/Database.cshtml`
- `Views/Fields/Details.cshtml`
- `Views/Fields/Index.cshtml`
- `Views/Home/About.cshtml`
- `Views/Home/Contact.cshtml`
- `Views/Home/Index.cshtml`
- `Views/Home/Status.cshtml`
- `Views/Meetings/Legacy.cshtml`
- `Views/OpenGames/Details.cshtml`
- `Views/OpenGames/Index.cshtml`
- `Views/Payments/Review.cshtml`
- `Views/Payments/Unavailable.cshtml`
- `Views/Shared/_Accounts.cshtml`
- `Views/Shared/_Activities.cshtml`
- `Views/Shared/_AuthState.cshtml`
- `Views/Shared/_Bookings.cshtml`
- `Views/Shared/_OwnerFields.cshtml`
- `Views/Shared/_PaymentHistory.cshtml`
- `Views/Shared/_Phase8Head.cshtml`
- `Views/Shared/_SitePresentation.cshtml`
- `Views/Shared/_Tournaments.cshtml`
- `Views/Shared/_WorkspaceOverview.cshtml`
- `Views/Shared/_WorkspaceRecords.cshtml`
- `Views/Teams/Index.cshtml`
- `Views/Tournaments/Details.cshtml`
- `Views/Tournaments/Index.cshtml`
- `Views/_ViewImports.cshtml`
- `Views/_ViewStart.cshtml`

## Dashboard sections

- Player: overview, bookings, games, tournament participation, messages, profile, team and challenge navigation.
- Owner: overview, fields, bookings, Open Games, participants, tournaments, messages, profile.
- Administrator: overview, fields, bookings, games, tournaments, participants, accounts, entries, payment review, feedback, messages, profile, site presentation.

## Routing findings

- No duplicate method/route attributes: verified.
- Account/private document review remains administrator only; owners and players manage only their own resources.
- Legacy meeting route forwards to messages. Demo catalogue/simulation and schema documentation remain reference-only pages.
