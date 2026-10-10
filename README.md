# RaceDay

RaceDay is a web-based event management system designed for the South African road running, walking and cycling community.

The system allows Event Organisers to manage sporting events, categories, participant enrolments and results. Participants can browse events, select categories, enrol for events and track their results.

---

## System Roles

### Organiser

Organisers can:

- Create events
- Edit events
- Delete events
- Manage event categories
- View participant enrolments
- Capture participant results

### Participant

Participants can:

- Create an account
- Browse available events
- View event information
- Select an event category
- Enrol for events
- View their enrolments
- View their race results

---



## Repository Structure

```text
RaceDay-Event-Management/
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── part1-repocheck.yml
│
├── docs/
│   ├── api_endpoint_plan.md
│   ├── erd.png
│   ├── RaceDayDatabase.sql
│   └── CI.png
│
├── RaceDay-Event-Management.API/
├── RaceDay-Event-Management.API.Tests/
│
└── README.md
```

---



## Part 1 - System Planning and Database

Part 1 contains the planning and database components for the RaceDay system.

The following deliverables have been completed:

- Entity Relationship Diagram (ERD)
- REST API Endpoint Plan
- SQL Database Script
- GitHub repository setup
- GitHub Actions validation workflow

---



## Entity Relationship Diagram

The RaceDay database contains the following entities:

- Users
- EventTypes
- Locations
- Events
- Categories
- Enrolments
- Results
- EventImages
- WeatherSnapshots

The ERD includes the entity attributes, primary keys, foreign keys and relationships between the entities.


**Below is the RaceDay ERD:**



![](docs/erd.png)

---



## API Endpoint Plan

The API Endpoint Plan defines the REST API for the RaceDay system.

The plan includes:

- HTTP Method
- Route
- Description
- Role Required
- Request Body
- Expected Response

The endpoint plan covers the main system functionality, including authentication, user profiles, events, categories, enrolments and results.

The completed API Endpoint Plan is available at:

```text
docs/api_endpoint_plan.md
```

---



## SQL Database

The RaceDay database was created using Microsoft SQL Server.

The SQL script contains:

- Database creation
- Table creation
- Primary keys
- Foreign keys
- NOT NULL constraints
- UNIQUE constraints
- DEFAULT constraints
- CHECK constraints
- Sample data

The database contains the following tables:


| Table            | Purpose                                        |
| ---------------- | ---------------------------------------------- |
| Users            | Stores Organiser and Participant information   |
| EventTypes       | Stores event types such as Run, Walk and Cycle |
| Locations        | Stores event location information              |
| Events           | Stores RaceDay event information               |
| Categories       | Stores categories for each event               |
| Enrolments       | Links Participants to events and categories    |
| Results          | Stores participant finish times and positions  |
| EventImages      | Stores event image information                 |
| WeatherSnapshots | Stores weather information related to events   |


The database includes sample data for:

- 2 Organisers
- 2 Participants
- 3 Events
- Categories for each event
- Sample enrolments
- Sample results
- Event images
- Weather information

The completed SQL script is available at:

```text
docs/RaceDayDatabase.sql
```

---



## Running the Database

The database script can be executed using Microsoft SQL Server Management Studio (SSMS).

### Requirements

- Microsoft SQL Server
- SQL Server Management Studio (SSMS)



### Steps

1. Open SQL Server Management Studio.
2. Connect to a SQL Server instance.
3. Open:
  ```text
   docs/RaceDayDatabase.sql
  ```
4. Execute the complete script.
5. The `RaceDayDB` database will be created.
6. The tables and sample data will be inserted.

The completed database can be viewed under:

```text
Databases
└── RaceDayDB
```

---



## GitHub and Version Control

GitHub is used to manage the RaceDay project and track development progress.

The repository contains more than 20 meaningful commits covering areas such as:

- ERD development
- Database development
- API endpoint planning
- Database relationships
- Constraints
- Sample data
- Repository configuration
- GitHub Actions

---



## GitHub Actions

GitHub Actions is used to validate the Part 1 repository structure.

The workflow checks that the required documentation files are present:

```text
docs/
├── api_endpoint_plan.md
├── erd.png
└── RaceDayDatabase.sql
```

The workflow is located at:

```text
.github/workflows/part1-repocheck.yml
```

The workflow completed successfully and produced a green build.

### CI/CD Build

![](docs/CI.png)

---



## Project Status


| Part 1 Component        | Status   |
| ----------------------- | -------- |
| ERD                     | Complete |
| API Endpoint Plan       | Complete |
| SQL Database Script     | Complete |
| Repository Structure    | Complete |
| GitHub Actions Workflow | Complete |
| CI/CD Validation        | Complete |


---



## Conclusion

RaceDay Part 1 has been completed with the required system planning, database design and repository setup.

The ERD, API Endpoint Plan and SQL Database Script provide the foundation for the RaceDay system and are stored together in the `docs` folder.

GitHub is used for version control, while GitHub Actions provides automated validation of the required Part 1 repository structure.

---

---

## Part 2 - RESTful API Development

Part 2 implements the RESTful API for the RaceDay system using ASP.NET Core 8 Web API. The implementation follows the API specification defined in `docs/api_endpoint_plan.md` and connects to Microsoft SQL Server using Entity Framework Core Code-First.

### Project Locations

- **API Project:** `RaceDay-Event-Management.API/`
- **Unit Test Project:** `RaceDay-Event-Management.API.Tests/`
- **Solution File:** `RaceDay-Event-Management.sln`

---

### Features and Implementation

1. **Database Integration & EF Core**
   - Built using ASP.NET Core 8 Web API and EF Core Code-First.
   - `RaceDayDbContext` maps to the 9 core entities matching the Part 1 database schema (`Users`, `EventTypes`, `Locations`, `Events`, `Categories`, `Enrolments`, `Results`, `EventImages`, `WeatherSnapshots`).
   - Seeds default categories, locations, event types, events, and sample user accounts.

2. **Authentication & Password Hashing**
   - Account registration (`POST /api/auth/register`) supporting `Organiser` and `Participant` roles.
   - Login (`POST /api/auth/login`) with credential validation and session creation.
   - Password hashing using PBKDF2 with SHA256 key derivation.
   - Sensitive fields (`PasswordHash`) are excluded from API response contracts (`UserResponse`).

3. **Role-Based Access Control (RBAC)**
   - Server-side session authentication enforced via custom session filters (`[RequireSession]`).
   - Role permissions for `Organiser` and `Participant` strictly enforced at API level:
     - **Organiser:** Create, edit, and delete events; create and manage categories; record and update participant finish times/positions; view enrolments for owned events.
     - **Participant:** Browse events, view event categories, enrol in events, view personal enrolments, cancel eligible enrolments, and view personal race history.

4. **API Endpoint Coverage**
   - Implemented **43 RESTful API endpoints** across 10 controllers:
     - Authentication (4 endpoints)
     - User Profile (3 endpoints)
     - Events (6 endpoints)
     - Event Types (5 endpoints)
     - Locations (5 endpoints)
     - Categories (5 endpoints)
     - Event Enrolments (4 endpoints)
     - Results (5 endpoints)
     - Event Images (3 endpoints)
     - Weather Snapshots (3 endpoints)

5. **Swagger Integration**
   - Swagger / OpenAPI UI integrated at `/swagger` with XML documentation comments describing endpoints, request models, and expected HTTP status codes.

6. **Unit & Integration Testing**
   - Test suite containing **27 integration tests** using xUnit and `WebApplicationFactory`.
   - Tests cover registration, login/logout, password protection, role authorization rejections, event CRUD, category distance checks, capacity limits, enrolment cancellations, and duplicate result position prevention.

7. **Version Control & CI/CD**
   - Contains over 40 commits tracking project progress.
   - GitHub Actions workflow (`.github/workflows/ci.yml`) compiles the solution and executes unit tests automatically on push.

---

### Database Setup & Connection String

The API connects to SQL Server Express using the connection string in `RaceDay-Event-Management.API/appsettings.json`:

```text
Server=localhost\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;
```

If your SQL Server instance uses a different server name, update `appsettings.json` accordingly (e.g., `localhost\SQLEXPRESS01` or `(localdb)\MSSQLLocalDB`).

---

### Running the API

From the root repository directory:

```text
dotnet run --project RaceDay-Event-Management.API
```

Access Swagger UI in browser:

```text
http://localhost:5264/swagger
```

To run using HTTPS launch profile:

```text
dotnet run --project RaceDay-Event-Management.API --launch-profile https
```

Access Swagger UI:

```text
https://localhost:7121/swagger
```

---

### Sample Accounts for Testing

All seeded test accounts use the password: `Password123!`

| Email | Role |
| :--- | :--- |
| `thabo@raceday.co.za` | Organiser |
| `naledi@raceday.co.za` | Organiser |
| `kabelo@example.com` | Participant |
| `lerato@example.com` | Participant |

---

### Running Unit Tests

To run the unit test suite:

```text
dotnet test RaceDay-Event-Management.sln
```

---

### Part 2 Component Status

| Part 2 Component | Status |
| :--- | :--- |
| API & Database Architecture | Complete |
| Authentication & Password Security | Complete |
| Role-Based Access Control | Complete |
| RESTful Endpoints (43 Endpoints) | Complete |
| Swagger UI Integration | Complete |
| Unit Testing Suite (27 Tests) | Complete |
| GitHub Actions CI/CD | Complete |

---

### Author

- **Name:** `ST10444612 Mike Thando Ndaba`
