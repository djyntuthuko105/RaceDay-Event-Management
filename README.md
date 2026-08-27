# RaceDay

RaceDay is a web-based event management system designed for the South African road running, walking and cycling community.

The system is designed to help Event Organisers manage sporting events while allowing Participants to browse events, register for events, select categories and track their race results.

RaceDay is being developed as a three-part Portfolio of Evidence project:

- Part 1 - System Planning and Database
- Part 2 - RESTful API Development
- Part 3 - MVC Web Application

Each part builds on the previous part. The planning completed in Part 1 will be used as the foundation for the RESTful API in Part 2 and the MVC web application in Part 3.

---



## Part 1 - System Planning and Database

Part 1 focuses on planning the RaceDay system before application code is developed.

The three main deliverables for Part 1 are:

1. Entity Relationship Diagram (ERD)
2. REST API Endpoint Plan
3. SQL Database Script

All three planning documents are stored inside the `docs` folder.

---



## Repository Structure

The current repository structure is:

```text
RaceDay/
│
├── docs/
│   ├── api_endpoint_plan.md
│   ├── erd.png
│   └── RaceDayDatabase.sql
│
└── README.md
```

---
## System Roles

RaceDay has two main user roles.

### Organiser

An Organiser is responsible for managing events on the RaceDay platform.

Organisers will be able to:

- Create events
- Edit events
- Delete events
- Manage event categories
- View participant enrolments for their events
- Capture participant results



### Participant

A Participant uses RaceDay to find and enter events.

Participants will be able to:

- Create an account
- Browse available events
- View event information
- Select an event category
- Enter an event
- View their own enrolments
- Track their personal race results

Role-based access will be enforced at the API level in Part 2 and reflected in the MVC application in Part 3.

---



## Entity Relationship Diagram

The ERD represents the database structure planned for the RaceDay system.

The current ERD contains the following entities:

- Users
- EventTypes
- Locations
- Events
- Categories
- Enrolments
- Results
- EventImages
- WeatherSnapshots

The ERD shows:

- Primary keys
- Foreign keys
- Entity attributes
- Relationships between entities
- Relationship cardinality

The ERD is stored in:

```text
docs/erd.png
```

The database structure shown in the ERD is designed to support the RaceDay requirements while allowing the system to be expanded during Parts 2 and 3.

---
## API Endpoint Plan

The API Endpoint Plan defines the RESTful API that will be developed in Part 2.

The endpoint plan is created before API development so that the implementation can follow the planned design.

The plan includes:

- HTTP Method
- Route
- Description
- Role Required
- Request Body
- Expected Response

The planned API covers the required RaceDay functionality, including:

- Authentication
- User Profile
- Events
- Categories
- Event Enrolments
- Results

The endpoint plan is stored in:

```text
docs/api_endpoint_plan.md
```

---



## SQL Database

The SQL script creates and populates the RaceDay database using Microsoft SQL Server.

The script includes:

- Database creation
- Table creation
- Primary keys
- Foreign keys
- NOT NULL constraints
- UNIQUE constraints
- DEFAULT constraints
- CHECK constraints
- Sample data

The database contains the following entities:


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


The SQL script includes sample data for:

- 2 Organisers
- 2 Participants
- 3 Events
- Event categories
- Sample enrolments
- Sample results
- Event images
- Weather information

The SQL script is stored in:

```text
docs/RaceDayDatabase.sql
```

---
## Running the Database

The SQL database can be created using Microsoft SQL Server Management Studio (SSMS).

### Requirements

The following software is required:

- Microsoft SQL Server
- SQL Server Management Studio (SSMS)



### Steps

1. Open SQL Server Management Studio.
2. Connect to your SQL Server instance.
3. Open the following file:
  ```text
   docs/RaceDayDatabase.sql
  ```
4. Run the complete SQL script.
5. The `RaceDayDB` database will be created.
6. The database tables and sample data will be inserted.

The database can then be viewed in SQL Server Management Studio under:

```text
Databases
└── RaceDayDB
```

---



## Part 1 Testing

The SQL script will be tested on a clean SQL Server database before submission.

The test will confirm that:

- The database is created successfully.
- All tables are created successfully.
- Primary keys are created.
- Foreign keys are created.
- Required constraints are created.
- Sample data is inserted successfully.
- Relationships between the tables work correctly.
- The script runs without errors.

---