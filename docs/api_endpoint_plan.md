# RaceDay API Endpoint Plan

## 1. Authentication


| HTTP Method | Route                | Description                                                                                          | Role Required          | Request Body                                                        | Expected Response                                                                                                            |
| ----------- | -------------------- | ---------------------------------------------------------------------------------------------------- | ---------------------- | ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| POST        | `/api/auth/register` | Creates a new RaceDay account and registers the user as either an Organiser or Participant.          | None                   | `FirstName`, `LastName`, `Email`, `Password`, `PhoneNumber`, `Role` | **201 Created** - User registered successfully. **400 Bad Request** - Invalid data. **409 Conflict** - Email already exists. |
| POST        | `/api/auth/login`    | Authenticates a registered user and creates a server-side session containing the user's ID and role. | None                   | `Email`, `Password`                                                 | **200 OK** - Login successful. **401 Unauthorized** - Invalid credentials.                                                   |
| POST        | `/api/auth/logout`   | Ends the current authenticated session.                                                              | Any authenticated user | None                                                                | **200 OK** - Session ended successfully. **401 Unauthorized** - No active session.                                           |
| GET         | `/api/auth/session`  | Returns information about the currently authenticated user and their role.                           | Any authenticated user | None                                                                | **200 OK** - Current session information returned. **401 Unauthorized** - User is not logged in.                             |


---
## 2. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/users/me` | Retrieves the profile information of the currently authenticated user. | Any authenticated user | None | **200 OK** - User profile returned. **401 Unauthorized** - User is not authenticated. |
| PUT | `/api/users/me` | Updates the profile information of the currently authenticated user. | Any authenticated user | `FirstName`, `LastName`, `PhoneNumber`, `ProfilePictureUrl` | **200 OK** - Profile updated successfully. **400 Bad Request** - Invalid data. **401 Unauthorized** - User is not authenticated. |
| GET | `/api/users/{id}` | Retrieves a user's profile by ID where access is permitted. | Organiser | None | **200 OK** - User profile returned. **401 Unauthorized** - User is not authenticated. **403 Forbidden** - Insufficient permissions. **404 Not Found** - User does not exist. |

---

## 3. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events` | Retrieves all upcoming RaceDay events for browsing and event discovery. | None | None | **200 OK** - List of events returned. |
| GET | `/api/events/{id}` | Retrieves complete information for a specific event. | None | None | **200 OK** - Event details returned. **404 Not Found** - Event does not exist. |
| POST | `/api/events` | Creates a new RaceDay event. | Organiser | `EventName`, `Description`, `EventDate`, `DistanceKm`, `RegistrationDeadline`, `EventTypeId`, `LocationId` | **201 Created** - Event created successfully. **400 Bad Request** - Invalid event data. **401 Unauthorized** - User is not authenticated. **403 Forbidden** - Participant attempted access. |
| PUT | `/api/events/{id}` | Updates an event created by the authenticated Organiser. | Organiser | `EventName`, `Description`, `EventDate`, `DistanceKm`, `RegistrationDeadline`, `EventTypeId`, `LocationId` | **200 OK** - Event updated successfully. **400 Bad Request** - Invalid data. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |
| DELETE | `/api/events/{id}` | Deletes an event managed by the authenticated Organiser. | Organiser | None | **204 No Content** - Event deleted successfully. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |
| GET | `/api/events/{id}/enrolments` | Retrieves all participant enrolments for an event managed by the Organiser. | Organiser | None | **200 OK** - Enrolment list returned. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |

---

## 4. Event Types

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/event-types` | Retrieves all available event types such as Run, Walk and Cycle. | None | None | **200 OK** - List of event types returned. |
| GET | `/api/event-types/{id}` | Retrieves a specific event type. | None | None | **200 OK** - Event type returned. **404 Not Found** - Event type does not exist. |
| POST | `/api/event-types` | Creates a new event type for the RaceDay system. | Organiser | `TypeName`, `Description` | **201 Created** - Event type created successfully. **400 Bad Request** - Invalid data. **409 Conflict** - Event type already exists. |
| PUT | `/api/event-types/{id}` | Updates an existing event type. | Organiser | `TypeName`, `Description` | **200 OK** - Event type updated successfully. **400 Bad Request** - Invalid data. **404 Not Found** - Event type does not exist. |
| DELETE | `/api/event-types/{id}` | Removes an event type that is no longer required. | Organiser | None | **204 No Content** - Event type deleted successfully. **404 Not Found** - Event type does not exist. **409 Conflict** - Event type is being used by an event. |

---
