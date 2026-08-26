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
## 5. Locations

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/locations` | Retrieves available event locations. | Any authenticated user | None | **200 OK** - List of locations returned. **401 Unauthorized** - User is not authenticated. |
| GET | `/api/locations/{id}` | Retrieves a specific event location. | Any authenticated user | None | **200 OK** - Location returned. **401 Unauthorized** - User is not authenticated. **404 Not Found** - Location does not exist. |
| POST | `/api/locations` | Creates a new event location. | Organiser | `VenueName`, `AddressLine`, `City`, `Province`, `PostalCode`, `Latitude`, `Longitude` | **201 Created** - Location created successfully. **400 Bad Request** - Invalid location data. **401 Unauthorized** - User is not authenticated. **403 Forbidden** - User is not an Organiser. |
| PUT | `/api/locations/{id}` | Updates an existing event location. | Organiser | `VenueName`, `AddressLine`, `City`, `Province`, `PostalCode`, `Latitude`, `Longitude` | **200 OK** - Location updated successfully. **400 Bad Request** - Invalid data. **403 Forbidden** - User is not an Organiser. **404 Not Found** - Location does not exist. |
| DELETE | `/api/locations/{id}` | Deletes an unused event location. | Organiser | None | **204 No Content** - Location deleted successfully. **403 Forbidden** - User is not an Organiser. **404 Not Found** - Location does not exist. **409 Conflict** - Location is currently linked to an event. |

---

## 6. Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/categories` | Retrieves all categories available for a specific event. | None | None | **200 OK** - Categories returned. **404 Not Found** - Event does not exist. |
| GET | `/api/categories/{id}` | Retrieves a specific event category. | None | None | **200 OK** - Category returned. **404 Not Found** - Category does not exist. |
| POST | `/api/events/{eventId}/categories` | Creates a new age or distance category for an event. | Organiser | `CategoryName`, `MinimumAge`, `MaximumAge`, `CategoryDistanceKm`, `MaximumParticipants` | **201 Created** - Category created successfully. **400 Bad Request** - Invalid category. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |
| PUT | `/api/categories/{id}` | Updates an existing event category. | Organiser | `CategoryName`, `MinimumAge`, `MaximumAge`, `CategoryDistanceKm`, `MaximumParticipants` | **200 OK** - Category updated successfully. **400 Bad Request** - Invalid data. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Category does not exist. |
| DELETE | `/api/categories/{id}` | Removes a category from an event when it is no longer required. | Organiser | None | **204 No Content** - Category deleted successfully. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Category does not exist. **409 Conflict** - Category has existing enrolments. |

---

## 7. Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/events/{eventId}/enrolments` | Enrols the authenticated Participant into an event using a selected category. | Participant | `CategoryId` | **201 Created** - Enrolment created successfully. **400 Bad Request** - Invalid category. **403 Forbidden** - User is not a Participant. **404 Not Found** - Event or category does not exist. **409 Conflict** - Participant is already enrolled. |
| GET | `/api/enrolments/me` | Retrieves all events the authenticated Participant has entered. | Participant | None | **200 OK** - Participant's enrolments returned. **401 Unauthorized** - User is not authenticated. **403 Forbidden** - User is not a Participant. |
| GET | `/api/enrolments/{id}` | Retrieves details of a specific enrolment belonging to the authenticated Participant. | Participant | None | **200 OK** - Enrolment returned. **403 Forbidden** - Enrolment belongs to another user. **404 Not Found** - Enrolment does not exist. |
| DELETE | `/api/enrolments/{id}` | Cancels an existing Participant enrolment where cancellation is allowed. | Participant | None | **204 No Content** - Enrolment cancelled successfully. **403 Forbidden** - Enrolment belongs to another user. **404 Not Found** - Enrolment does not exist. **409 Conflict** - Enrolment can no longer be cancelled. |

---
## 8. Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/enrolments/{enrolmentId}/result` | Records the finish time and finishing position for a Participant after an event. | Organiser | `FinishTime`, `FinishPosition` | **201 Created** - Result recorded successfully. **400 Bad Request** - Invalid result. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Enrolment does not exist. **409 Conflict** - Result already exists. |
| GET | `/api/results/me` | Retrieves the authenticated Participant's complete race history. | Participant | None | **200 OK** - Personal results returned. **401 Unauthorized** - User is not authenticated. **403 Forbidden** - User is not a Participant. |
| GET | `/api/results/{id}` | Retrieves a specific race result belonging to the authenticated Participant. | Participant | None | **200 OK** - Result returned. **403 Forbidden** - Result belongs to another Participant. **404 Not Found** - Result does not exist. |
| GET | `/api/events/{eventId}/results` | Retrieves the results recorded for an event. | Organiser | None | **200 OK** - Event results returned. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |
| PUT | `/api/results/{id}` | Corrects or updates an existing Participant result. | Organiser | `FinishTime`, `FinishPosition` | **200 OK** - Result updated successfully. **400 Bad Request** - Invalid result. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Result does not exist. |

---
## 9. Event Images

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/images` | Retrieves images associated with an event. | None | None | **200 OK** - Event images returned. **404 Not Found** - Event does not exist. |
| POST | `/api/events/{eventId}/images` | Uploads an event image through the API and stores its image URL information. | Organiser | `Image file` | **201 Created** - Image uploaded successfully. **400 Bad Request** - Invalid file. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Event does not exist. |
| DELETE | `/api/event-images/{id}` | Removes an event image from an event. | Organiser | None | **204 No Content** - Image removed successfully. **403 Forbidden** - Organiser does not own event. **404 Not Found** - Image does not exist. |

---
## 10. Weather Snapshots

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | `/api/events/{eventId}/weather` | Retrieves weather information associated with an event. | None | None | **200 OK** - Weather information returned. **404 Not Found** - Event does not exist. |
| POST | `/api/events/{eventId}/weather` | Stores a weather snapshot associated with an event. | Organiser | `TemperatureCelsius`, `FeelsLikeCelsius`, `WeatherCondition`, `WindSpeedKmh`, `HumidityPercentage`, `RecordedAt` | **201 Created** - Weather snapshot stored successfully. **400 Bad Request** - Invalid weather data. **403 Forbidden** - User is not an Organiser. **404 Not Found** - Event does not exist. |
| GET | `/api/weather/{id}` | Retrieves a specific weather snapshot. | None | None | **200 OK** - Weather snapshot returned. **404 Not Found** - Weather snapshot does not exist. |

---

# API Design Rules

## Authentication and Sessions

- Users must authenticate before accessing protected endpoints.
- The server must maintain the authenticated user's session.
- The user's ID and role must be available to protected endpoints.
- Participants must not access Organiser-only operations.
- Organisers must not perform Participant-only enrolment operations.
- Users must only be allowed to access resources they are authorised to manage.

## Role Permissions

### Organiser

Organisers can:

- Create events.
- Update their own events.
- Delete their own events.
- Create and manage event categories.
- Manage event locations.
- Record participant results.
- Update participant results.
- View enrolments for their own events.
- Manage event images.
- Manage event-related weather information.
