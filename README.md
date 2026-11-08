# RaceDay

An ASP.NET Core MVC event platform for South Africa's running, walking and cycling community. The MVC website consumes the existing Part 2 API for every data operation. SQL Server remains the main database provider; SQLite and Azurite offer a free, self-contained local demo.

## What you can do

| Organiser | Participant |
| --- | --- |
| View owned events, entry totals and upcoming dates | Browse and filter upcoming events by name, city and sport |
| Create, edit and safely delete events | Read full event information and available categories |
| Add venues, upload banners and manage age/distance categories | Enter an event, view entry status and cancel eligible entries |
| View entrants and publish or correct post-race results | View personal finish times, dates, categories and overall positions |
| Update profile details and upload a profile picture | Update profile details and upload a profile picture |

Navigation changes with the logged-in role. The API enforces roles and ownership independently. Statuses include visible text and distinct colours. The responsive interface uses locally bundled Bootstrap, original CSS and local artwork; it has no external UI-service dependency.

## Project structure

```text
RaceDay-Event-Management.API/        Existing API, EF Core, sessions and image storage
RaceDay-Event-Management.Web/        MVC controllers, Razor views, API client and UI
RaceDay-Event-Management.API.Tests/  API integration and weather tests
RaceDay-Event-Management.Web.Tests/ Complete MVC/API journeys and security checks
docs/                              Preserved Part 1 files and submission guidance
scripts/smoke.py                    Real local MVC/API/Blob HTTP smoke check
Dockerfile                         Separate non-root API and MVC runtime targets
compose.yml                        Free local SQLite and Azurite demo stack
compose.sqlserver.yml              Optional SQL Server overlay
```

The original ERD, SQL script, endpoint plan, Part 2 migrations and existing API tests are retained. [Earlier submission notes](docs/part1-part2-readme.md) remain available. The MVC project links only the API's request/response DTO source; it has no EF Core reference or database connection.

## Quick start with Docker

Install Docker Engine/Desktop with Compose v2. From the repository root:

```bash
docker compose up --build -d
```

- Website: http://localhost:5000
- API Swagger: http://localhost:5080/swagger
- API readiness: http://localhost:5080/health

This starts MVC, API, a persistent SQLite demo database and Azurite Blob Storage. It does not contact a live Azure account or provision paid resources. The sample emulator account/key in Compose is intentionally local and is not an Azure credential.

To run the MVC container directly after the stack has built its image:

```bash
docker build --target web -t raceday-web .
docker run --rm -p 5001:8080 --network raceday-part3_default -e Api__BaseUrl=http://api:8080/ raceday-web
```

The direct command expects the Compose API to be running. Open http://localhost:5001. The API and website are separate applications, so the website still requires the API.

```bash
docker compose logs -f
docker compose down
```

Stopping the stack preserves database and Blob volumes. Do not remove volumes if you need to keep your work.

### Use SQL Server instead

SQL Server Developer requires a compatible host, adequate memory and acceptance of Microsoft's development licence. Set a strong local `SQL_PASSWORD` environment variable, then run:

```bash
docker compose -f compose.yml -f compose.sqlserver.yml up --build -d
```

The overlay switches the API back to SQL Server, applies the existing EF migrations and uses a separate SQL data volume. Never point demo seeding at a live database. Do not execute the Part 1 planning SQL script on top of an EF-created database; use a separate clean planning database/instance for that demonstration.

## Run without Docker

Install the .NET 8 SDK. The following demo uses SQLite and explicitly labelled local file storage, not Azure Blob Storage.

From the repository root, in separate terminals:

```bash
dotnet restore RaceDay-Event-Management.sln
dotnet run --project RaceDay-Event-Management.API --no-launch-profile --environment LocalDemo --urls http://localhost:5080
dotnet run --project RaceDay-Event-Management.Web --no-launch-profile --urls http://localhost:5000
```

`LocalDemo` creates `raceday-demo.db` in the API content directory and seeds sample accounts only when the database has no users. SQLite is for local portability; SQL Server is still the default provider outside this explicitly selected demo. The original Visual Studio API launch profiles remain available.

For your own SQL Server database, provide `ConnectionStrings__RaceDayDb`, keep `Database__Provider=SqlServer`, and run the API without `LocalDemo`. Enable `Database__SeedDemo=true` only on a new, disposable development database. Otherwise demo seeding is off outside Development/LocalDemo.

### Sample accounts

All seeded demonstration accounts use `Password123!`. These are disposable coursework examples, not production credentials.

| Email | Role |
| --- | --- |
| thabo@raceday.co.za | Organiser |
| naledi@raceday.co.za | Organiser |
| kabelo@example.com | Participant |
| lerato@example.com | Participant |

Register your own accounts through the website. Both roles are self-selectable for this coursework; a public production service should approve organiser accounts.

## Azure Blob Storage

Uploads follow **MVC form → Part 2 API → Blob Storage → saved database URL**. MVC never receives storage credentials or talks to Azure. Banner URLs are saved in `EventImages`; profile URLs are saved in `Users.ProfilePictureUrl`.

Configure the **API process only** using environment variables:

| Setting | Value |
| --- | --- |
| `Storage__Provider` | `AzureBlob` |
| `Storage__ConnectionString` | Your storage connection string, provided securely at runtime |
| `Storage__Container` | `raceday-images` or your chosen private container |

The API creates a missing container with private access. Images are displayed via API content endpoints, proxied by MVC; no public-container setting or expiring SAS URL is required. Use a private container in a storage account you control. Do not commit connection strings, keys or `.env` files. No live account has been provisioned or modified as part of the implementation.

JPG, PNG, GIF and WebP uploads are limited to 5 MB, checked for matching file signatures and assigned random filenames. SVG uploads are not accepted. General profile updates cannot inject arbitrary image URLs; use the dedicated picture upload endpoint. Old profile images are retained when replaced; lifecycle cleanup is a future maintenance task. This is basic validation, not a malware scanner.

The default Compose stack selects the same Azure Blob SDK provider against **Azurite**, a local emulator. `Local` file storage remains available for Part 2 compatibility but does not count as live Azure verification. A real Azure account upload still needs to be demonstrated before claiming cloud-hosted storage evidence.

## Race-day behaviour

- Entry deadlines, duplicate entries, category capacity and ownership are validated by the API.
- Dashboard data includes completed events, not only the public upcoming catalogue.
- Results open after the event date, because the data model contains a date rather than an exact finish time. Finish times use `HH:mm:ss` and must be greater than zero; positions are unique across the whole event.
- My Results shows the number of **recorded** finishers. This count grows while the organiser is still entering results; it is not a fabricated final field size.
- Current weather comes through the API from [Open-Meteo](https://open-meteo.com/) with CC BY 4.0 attribution. It is labelled as current conditions, not a forecast for a distant event date. Requests have a timeout, caching and a clear unavailable state.
- Venue directions open Google Maps. They are explicitly not a race-course map; official course data has not been supplied.
- MVC server-side session stores user ID, role and its own API session cookie. HTTP clients do not share a cookie jar between users. Logout clears both sessions. Sessions are in memory, so restarting either application ends active sessions; multi-instance hosting would need a shared session store.
- Use HTTPS and appropriate secret management for any real deployment. The local container stack is HTTP-only, bound to loopback host ports, and is not a production deployment. Both application images use Africa/Johannesburg (SAST) for date-based rules.

## Build and test

```bash
dotnet build RaceDay-Event-Management.sln -c Release
dotnet test RaceDay-Event-Management.sln -c Release
```

The verified local suite contains **47 passing tests**: 39 API/weather tests and 8 complete MVC/API workflow tests. Existing Part 2 scenarios remain, with result test setup moved to a completed event to match the final-part requirement.

With the local Blob-enabled stack running:

```bash
python3 scripts/smoke.py
```

This standard-library HTTP check is restricted to local loopback URLs. It creates a disposable organiser account and temporary event, exercises real MVC login, event/banner upload, persisted Blob URLs, image display, profile picture upload and logout, then removes its temporary event and banner. It is an integration check, not a browser screenshot or a replacement for visual testing.

## CI and submission evidence

`.github/workflows/ci.yml` now runs on every branch push and pull request. It builds both projects, runs both test suites, builds both Docker targets through Compose, and runs the actual Blob smoke check. The separate Part 1 document validation workflow is retained.

**Part 3 GitHub CI has not yet run for this local branch.** Docker was not installed in the implementation environment, so container execution must still be confirmed on a Docker-capable machine or by the configured workflow after an authorised push. Local .NET builds, all 47 tests and the real Azurite/MVC smoke test passed; these are not claimed as a green GitHub run or Docker execution.

The existing earlier-part CI screenshot is retained below. It is **not** Part 3 CI evidence:

![Earlier-part CI screenshot](docs/CI.png)

Before submitting, capture a genuine green run for the final Part 3 commit and add that screenshot here. See the [rubric coverage and evidence checklist](docs/part3-submission.md).

### Demonstration video

The student's own-voice, unlisted YouTube recording is still required. No AI voice, invented link or fake video placeholder is included. Record the running MVC application for both roles, explain API session propagation, demonstrate a Blob upload and result publication, and show Docker running. Add the actual unlisted link to this section before submission.

## Credits

Original interface artwork and styling are included in `RaceDay-Event-Management.Web/wwwroot`. Bootstrap 5.3.3 is bundled locally under its MIT licence; its licence notice is retained in the CSS header. Weather data: Open-Meteo, CC BY 4.0. Repository owner and Part 3 commit identity: `djyntuthuko105`.
