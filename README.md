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