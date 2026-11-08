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
| Correct submission (5) | Root README, unchanged Part 1 artifacts, API, MVC, two test projects, Dockerfile, Compose and CI. Genuine CI results are linked from the README. |
| GitHub and CI (10) | Twenty meaningful local Part 3 commits under the owner's identity; workflow builds both applications, runs tests and a container smoke check. The initial twenty commits were pushed to main. Follow-up cleanup uses additional commits without rewriting that history. |
| Authentication/navigation (10) | Account forms call API; MVC session stores UserId, Role, display name and API cookie. Role menus, redirect/403 handling and two-session logout; covered by MVC tests. |
| Organiser (15) | Owned dashboard with all event dates and entry totals; event CRUD, venues, categories with optional age/distance/capacity, banner upload/removal, entrant list with statuses, post-race result publication/correction. |
| Participant (15) | Filtered upcoming catalogue, full details, category entry, My Events and cancellation, My Results with dates/times/categories/positions and recorded finisher totals, editable Profile and image upload. |
| Azure Blob (10) | API uploads through Azure.Storage.Blobs and saves the actual URL. Private images stream via API and MVC. Actual emulator round-trip passed; live Azure account evidence remains pending. |
| Docker (10) | Multi-stage SDK build; separate non-root API/MVC targets; default free SQLite/Azurite stack and optional SQL Server overlay. The original GitHub run built and started both images. Its startup smoke check failed on a connection reset; the native replacement retries transient startup errors. |
| UI (10) | Original forest/lime sporting theme, locally bundled Bootstrap, responsive navigation/cards/forms/tables, visible status text and colour, validation summaries, confirmation pages, empty states, focus states and reduced-motion support. Browser visual inspection still recommended. |

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

Local verification on 9 November 2026 (SAST):

- .NET 8 Release build: succeeded.
- xUnit: 39 API/weather tests and 8 MVC workflow tests passed; none skipped.
- API and MVC processes started against a fresh local SQLite database.
- Azure SDK used against a real local Azurite process: banner and profile uploads saved Blob URLs; downloaded bytes matched uploaded files.
- `RaceDay-Event-Management.SmokeChecks`: actual MVC forms, API cookies, Blob banner, profile upload, persisted URL, image display and logout passed over HTTP.
- Live Open-Meteo request returned current conditions with a South African observation time. Automated tests use explicit stubbed provider data and also test unavailable/missing-coordinate states.
- GitHub built and started both Docker images. The original readiness runner failed immediately on ConnectionResetError before readiness; the C# replacement catches HTTP startup failures and retries.
- No live database, Azure account or deployment was modified. The original twenty Part 3 commits were subsequently pushed to main with explicit approval.

## Final submission checklist

- Review the running UI at desktop and mobile widths; correct any visual issues found.
- Review follow-up changes before authorising another commit/push; retain all twenty pushed commits.
- Confirm both GitHub workflow jobs turn green for the final commit; inspect Docker logs and smoke output.
- Add a genuine fresh Part 3 green-build screenshot to the README. The previous `docs/CI.png` remains earlier-part evidence.
- Configure an actual private Azure storage account securely if live cloud evidence is required, upload through MVC and verify the saved URL. Never commit the connection string.
- Submit the repository/branch link on ARC only after this evidence is complete.


## Follow-up CI correction

Run [42534077364](https://github.com/djyntuthuko105/RaceDay-Event-Management/actions/runs/42534077364) passed the .NET build/test job and built/started both Docker images. The old readiness check failed with a connection reset while the API container was still starting. The repository now uses a C# console verifier that retries transient HTTP failures before running the same session and image checks. Check the follow-up GitHub Actions run for the actual verification result; local checks alone do not establish a green CI run.
