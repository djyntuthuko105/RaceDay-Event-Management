# Part 3 requirement coverage and final evidence

## Architecture

```text
Browser -> MVC controllers and server-side session -> RaceDayApi HttpClient
        -> Part 2 API session and role/ownership checks -> EF Core database
                                                       -> Azure Blob SDK
```

Only DTO definitions are linked into MVC. There is no direct database or Azure SDK dependency in the MVC project. API cookies are held server-side per MVC session rather than in a shared HttpClient cookie jar. All state-changing MVC forms have antiforgery validation.

## Rubric map

| Criterion | Implementation and check |
| --- | --- |
| Correct submission (5) | Root README, unchanged Part 1 artifacts, API, MVC, two test projects, native smoke verifier, Dockerfile, Compose and CI. A genuine historical screenshot and verified Part 3 run are linked from the README. |
| GitHub and CI (10) | At least twenty meaningful owner-authored Part 3 commits under the owner's identity; workflow builds both applications, runs tests and a container smoke check. The initial twenty commits were pushed to main. Follow-up cleanup uses additional commits without rewriting that history. |
| Authentication/navigation (10) | Account forms call API; MVC session stores UserId, Role, display name and API cookie. Role menus, redirect/403 handling and two-session logout; covered by MVC tests. |
| Organiser (15) | Owned dashboard with all event dates and entry totals; event CRUD, venues, categories with optional age/distance/capacity, banner upload/removal, entrant list with statuses, post-race result publication/correction. |
| Participant (15) | Filtered upcoming catalogue, full details, category entry, My Events and cancellation, My Results with dates/times/categories/positions and recorded finisher totals, editable Profile and image upload. |
| Azure Blob (10) | API uploads through Azure.Storage.Blobs and saves the actual URL. Private images stream via API and MVC. Actual emulator round-trip passed; live Azure account evidence remains pending. |
| Docker (10) | Multi-stage SDK build, non-root API/MVC targets; required SQL Server stack and lightweight SQLite demo. Both complete stacks passed actual CI smoke checks, including Azurite uploads. Fresh SQL migrations/seed and the original planning script also passed on SQL Server 2022. |
| UI (10) | Original forest/lime sporting theme, locally bundled Bootstrap, responsive navigation/cards/forms/tables, visible status text and colour, validation summaries, confirmation pages, empty states, focus states and reduced-motion support. Actual rendered public pages were inspected through offline exports. The local GUI browser currently refuses live loopback connections despite successful shell HTTP checks; no claim of live GUI verification is made. |

## Part 2 API extensions

The original endpoint plan remains preserved. Part 3 adds:

| Method | Route | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/events/managed` | Organiser | Own events, including past races, with entry totals |
| POST | `/api/users/me/picture` | Both roles | Multipart profile image upload and saved URL |
| GET | `/api/users/me/picture` | Both roles | Own private stored profile image |
| GET | `/api/event-images/{id}/content` | Public | Published event banner bytes from private storage |
| GET | `/api/events/{id}/conditions` | Public | Current Open-Meteo weather and venue directions |
| GET | `/health` | Public | Process readiness after database startup |

Additional response fields: `EventDate` and `TotalFinishers` on result histories. General profile updates no longer accept arbitrary replacement image URLs; the existing DTO property is retained for compatibility, but the API assigns URLs only from uploaded files. Results are now rejected before the event has concluded. Existing API test scenarios are retained with corresponding date setup.

No schema migration was necessary: `EventImages.ImageUrl` and `Users.ProfilePictureUrl` already existed. SQL Server migrations and original planning files are unchanged. SQLite is an explicitly selected demo/test provider, not a replacement of the Part 2 SQL Server design.

## Verification actually completed

[Run 42535805542](https://github.com/djyntuthuko105/RaceDay-Event-Management/actions/runs/42535805542) completed successfully on 9 November 2026 (SAST):

- Release API/MVC build and all 47 then-existing tests passed.
- Both Docker matrix jobs, SQL Server and SQLite, passed the same real HTTP/Blob workflow.
- SQL Server 2022 started healthy; the API applied `20261008125827_InitialCreate` to a fresh database.
- Native C# smoke verified MVC/API sessions, role rejection, category creation, participant entry, organiser enrolments, post-race results/finisher counts, event CRUD, banner/profile Blob URL persistence, downloaded bytes and logout.
- SQL assertions confirmed the EF migration history, nine mapped tables and seeded organiser/participant/event data.
- The preserved Part 1 SQL script ran successfully in a separate disposable planning database with realistic categories and enrolments.
- All 43 planned endpoint/method pairs exist in the running Swagger document (49 operations including extensions).
- An actual Open-Meteo response and Google Maps venue link were inspected; automated weather tests also cover unavailable/missing-coordinate cases.

The completion audit added three additional API/MVC role and ownership regressions: **41 API/weather plus 9 MVC tests, all 50 passing locally**. Subsequent CI status is available in Actions. No test was removed or weakened.

## Evidence boundaries

- The README includes the genuine historical Part 1 green-build screenshot, clearly labelled as historical, and a direct successful Part 3 SQL Server run link. No screenshot was fabricated or replaced with a badge.
- Azurite proves the actual Azure SDK round-trip, not a live Azure upload. No project-specific Azure account or connection string has been supplied. Real-cloud evidence requires an existing approved RaceDay development account/container and securely provided API-only configuration before an approved test upload.
- The GUI live-preview failure remains unresolved; its root cause has not been established. Offline captures are real rendered app output but do not establish live browser navigation.
- No live database/cloud resource was modified or provisioned. Local and CI databases are disposable coursework fixtures.
