# Testing Plan (initial draft)

This file tracks how we verify the Blazor app stays working as features are added.

## Environment and run commands
- Requires .NET 9 SDK.
- Run from `DroidGames/BlazorApp1`.
- Restore and launch: `dotnet restore` then `dotnet run --urls http://localhost:5109`.
- The app writes JSON data under `BlazorApp1/Data` (also used at runtime) and keeps backups in `BlazorApp1/Data/backups`.

## Quick smoke checklist (manual for now)
- Home (`/`) loads without errors; leaderboard renders rows from `Data/final-round-scores.json` and team names from `Data/teams.json`.
- Current/next teams and round info reflect `Data/settings.json` values.
- Timer section shows a formatted countdown and updates status badges without exceptions.
- Navigation shows only public items when not authenticated; role-based items appear after session initialization.
- Quiz page (`/team/quiz`) is protected by `AuthGuard` and loads question stats for the signed-in team.

## Home page: real-time expectations and current gaps
- Expected: Game status, current/next teams, round number, leaderboard, and "your turn" modal update via SignalR without refresh.
- Current code only updates Home when `CompetitionNotificationService` / `GameStatusService` events fire. These services are only invoked from HeadReferee Control (not from `/admin/referee-approval`) and are not wired to any SignalR client listeners.
- Timer: Home reads `TimerService.GetRemainingSecondsAsync()` once on load; there is no subscription to `TimerHub` ticks. TimerHub is never called from `TimerService`/background service, so no live countdown reaches the UI.
- Leaderboard: Home listens to `OnLeaderboardUpdated` but nothing calls `NotifyLeaderboardUpdatedAsync()` from approval flow except `ScoreNotificationService.NotifyRoundCompleted` after approval. If approvals aren’t emitting, the UI stays stale until refresh.

### Targeted automated tests (bUnit/xUnit) to add
1) Home initial render shows round/status from settings
   - Mock `IRepository<CompetitionSettings>` to return a settings object (e.g., round 2, status InProgress).
   - Mock `IFinalScoreService` to return a small leaderboard.
   - Assert markup contains `Kolo 2` and badge text `Probíhá`, and leaderboard rows count matches mocked data.
2) Home responds to game status change event
   - Use a fake `IGameStatusService` that raises `OnGameStatusChanged`.
   - After rendering, trigger `InProgress` -> `Paused` and assert status badge text switches to `Pauza` without re-render.
3) Home reloads leaderboard on notification
   - Fake `ICompetitionNotificationService`; after first render, swap leaderboard data in fake service and invoke `OnLeaderboardUpdated`. Assert the new score appears.
4) Timer live updates (currently failing expectation)
   - Fake a `TimerService` that exposes a tick event and invoke it; assert Home shows updated `timer-value`. This will highlight the missing subscription in production code (red test to drive wiring TimerHub -> Home).
5) “Your turn” modal shows only for matching team
   - Fake notification service, set `UserSession` to a team user, trigger `OnYourTurn(teamId, name, pos)`, assert modal renders; trigger with different teamId -> no modal.

### Service-level tests (xUnit) to back SignalR wiring
- `GameStatusService.SetGameStatusAsync` broadcasts via `ScoreboardHub` and raises `OnGameStatusChanged`.
- `CompetitionNotificationService.Notify*` methods invoke hub clients for round/status/teams/leaderboard so UI can refresh.
- `TimerService` + `TimerBackgroundService` should emit ticks to `TimerHub` (currently not implemented—test will pin the requirement).

### Event/handler testing style (for websocket flows)
- U příjmu (Home a další stránky) preferuj volání handlerů přímo (`HandleRoundChanged`, `HandleCompetitionStatusChanged`, `HandleCurrentTeamsChanged`…) a ověř interní stav (privátní pole `_settings`, `_currentTeam*`, `_nextTeam*`, `_timerRemaining`). Nepotřebujeme plný render ani skutečný SignalR hub.
- U odesílání (HeadRef Control apod.) nastav hodnoty `_settings`, zavolej příslušný handler (`OnStatusChanged`, `OnRoundChanged`, `OnCurrentTeamsChanged`, `OnNextTeamsChanged`) a ověř, že fake `ICompetitionNotificationService` obdržel správnou hodnotu (ulož si ji ve faku).
- Sdílej pomocné metody: `GetPrivate<T>(instance, fieldName)` pro čtení privátních stavů a `InvokePrivateAsync(instance, methodName, args…)` pro volání handlerů v testech.
- Nepoužívej reálné hub připojení v bUnit testech; nahrad jej fake službou nebo přímo voláním handlerů. Tím se vyhneš závislosti na JS/SignalR a testy zůstanou rychlé a stabilní.

### Implementation notes for the test project
- Create `BlazorApp1.Tests` (xUnit + bUnit) targeting net9.0; add package refs `bunit` and `Moq`.
- Register fakes for services and a lightweight `UserSession` test double (to avoid `ProtectedSessionStorage` dependency).
- Keep JSON fixtures small and deterministic; reuse the existing `BlazorApp1/Data` files as inputs where possible.

## Automated test backlog
1) Unit/integration (xUnit)
   - `JsonRepository` creates missing files, persists items, and rejects invalid payloads.
   - `TeamService` returns known seed data and handles missing teams gracefully.
   - `FinalScoreService.GetDetailedLeaderboardAsync` combines final scores with team metadata and sorts deterministically.
   - `TimerService` state transitions (start/stop/reset) emit expected status values.
2) Component (bUnit) - after wiring DI for tests
   - `NavMenu` hides/shows links based on `UserSession` role and authentication flag.
   - `Home` renders leaderboard entries when services return data; shows loading/error states otherwise.
   - `Quiz` modal flow: selecting a question calls `SubmitAttemptAsync` and refreshes progress.
3) End-to-end (future Playwright)
   - Smoke: load Home, assert header text and at least one leaderboard row.
   - Quiz happy path: navigate to `/team/quiz` with seeded session cookie and answer a question.

## Data and fixtures
- Seed JSON files live in `BlazorApp1/Data`; keep them small and deterministic for test fixtures.
- Backups under `BlazorApp1/Data/backups` are created by `BackupService` during runtime; ignore in tests.

## Next coordination topics
- Which user roles and flows should be stabilized first (public leaderboard vs team/quiz vs referee)?
- Whether to add a dedicated `BlazorApp1.Tests` project now or after picking the first feature to lock down.
