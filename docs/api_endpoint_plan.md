# RaceDay API Endpoint Plan

## 1. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Creates a new RaceDay account and registers the user as either an Organiser or Participant. | None | `FirstName`, `LastName`, `Email`, `Password`, `PhoneNumber`, `Role` | **201 Created** - User registered successfully. **400 Bad Request** - Invalid data. **409 Conflict** - Email already exists. |
| POST | `/api/auth/login` | Authenticates a registered user and creates a server-side session containing the user's ID and role. | None | `Email`, `Password` | **200 OK** - Login successful. **401 Unauthorized** - Invalid credentials. |
| POST | `/api/auth/logout` | Ends the current authenticated session. | Any authenticated user | None | **200 OK** - Session ended successfully. **401 Unauthorized** - No active session. |
| GET | `/api/auth/session` | Returns information about the currently authenticated user and their role. | Any authenticated user | None | **200 OK** - Current session information returned. **401 Unauthorized** - User is not logged in. |

---