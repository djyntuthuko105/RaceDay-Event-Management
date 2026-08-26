-- RaceDay Database
-- Create the database
CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO
-- 1. Users
-- Stores Organisers and Participants

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    PhoneNumber VARCHAR(20),
    Role VARCHAR(20) NOT NULL,
    ProfilePictureUrl VARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT CK_Users_Role
    CHECK (Role IN ('Organiser', 'Participant'))
);
-- 2. EventTypes
-- Stores the type of race event

CREATE TABLE EventTypes
(
    EventTypeId INT IDENTITY(1,1) PRIMARY KEY,
    TypeName VARCHAR(30) NOT NULL UNIQUE,
    Description VARCHAR(255)
);
-- 3. Locations
-- Stores event locations

CREATE TABLE Locations
(
    LocationId INT IDENTITY(1,1) PRIMARY KEY,
    VenueName VARCHAR(150) NOT NULL,
    AddressLine VARCHAR(255) NOT NULL,
    City VARCHAR(100) NOT NULL,
    Province VARCHAR(100) NOT NULL,
    PostalCode VARCHAR(10),
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6)
);
-- 4. Events
-- Stores RaceDay events

CREATE TABLE Events
(
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventTypeId INT NOT NULL,
    LocationId INT NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    Description VARCHAR(1000) NOT NULL,
    EventDate DATE NOT NULL,
    DistanceKm DECIMAL(6,2) NOT NULL,
    RegistrationDeadline DATE NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (OrganiserId)
        REFERENCES Users(UserId),

    FOREIGN KEY (EventTypeId)
        REFERENCES EventTypes(EventTypeId),

    FOREIGN KEY (LocationId)
        REFERENCES Locations(LocationId),

    CONSTRAINT CK_Events_Distance
    CHECK (DistanceKm > 0)
);
-- 5. Categories
-- Stores categories for each event

CREATE TABLE Categories
(
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    MinimumAge INT,
    MaximumAge INT,
    CategoryDistanceKm DECIMAL(6,2),
    MaximumParticipants INT,

    FOREIGN KEY (EventId)
        REFERENCES Events(EventId),

    CONSTRAINT CK_Categories_Age
    CHECK (MinimumAge IS NULL OR MinimumAge >= 0),

    CONSTRAINT CK_Categories_MaxAge
    CHECK (MaximumAge IS NULL OR MaximumAge >= MinimumAge),

    CONSTRAINT CK_Categories_Participants
    CHECK (MaximumParticipants IS NULL OR MaximumParticipants > 0)
);
-- 6. Enrolments
-- Connects Participants to Events
CREATE TABLE Enrolments
(
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME DEFAULT GETDATE(),
    Status VARCHAR(20) DEFAULT 'Pending',

    FOREIGN KEY (ParticipantId)
        REFERENCES Users(UserId),

    FOREIGN KEY (EventId)
        REFERENCES Events(EventId),

    FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),

    CONSTRAINT CK_Enrolments_Status
    CHECK (Status IN
    ('Pending', 'Confirmed', 'Cancelled', 'Completed')),

    CONSTRAINT UQ_Enrolment
    UNIQUE (ParticipantId, EventId)
);
-- 7. Results
-- Stores participant race results
CREATE TABLE Results
(
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    FinishPosition INT NOT NULL,
    RecordedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (EnrolmentId)
        REFERENCES Enrolments(EnrolmentId),

    CONSTRAINT CK_Results_Position
    CHECK (FinishPosition > 0)
);
-- 8. EventImages
-- Stores images for event

CREATE TABLE EventImages
(
    EventImageId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    ImageUrl VARCHAR(500) NOT NULL,
    ImageType VARCHAR(50) NOT NULL,
    UploadedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (EventId)
        REFERENCES Events(EventId)
);
-- 9. WeatherSnapshots
-- Stores weather information for events
CREATE TABLE WeatherSnapshots
(
    WeatherSnapshotId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    TemperatureCelsius DECIMAL(5,2),
    FeelsLikeCelsius DECIMAL(5,2),
    WeatherCondition VARCHAR(100),
    WindSpeedKmh DECIMAL(6,2),
    HumidityPercentage INT,
    RecordedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (EventId)
        REFERENCES Events(EventId),

    CONSTRAINT CK_Weather_Humidity
    CHECK (HumidityPercentage BETWEEN 0 AND 100)
);
-- SAMPLE DATA
-- Event Types

INSERT INTO EventTypes
(TypeName, Description)
VALUES
('Run', 'Running events'),
('Walk', 'Walking events'),
('Cycle', 'Cycling events');
-- Locations

INSERT INTO Locations
(VenueName, AddressLine, City, Province, PostalCode, Latitude, Longitude)
VALUES
(
    'Pretoria National Botanical Garden',
    '2 Cussonia Avenue',
    'Pretoria',
    'Gauteng',
    '0001',
    -25.738000,
    28.267000
),
(
    'Loch Logan Waterfront',
    'Waterfront Road',
    'Bloemfontein',
    'Free State',
    '9301',
    -29.118000,
    26.215000
),
(
    'Durban Beachfront',
    'Snell Parade',
    'Durban',
    'KwaZulu-Natal',
    '4001',
    -29.858700,
    31.021800
);
-- Users
INSERT INTO Users
(FirstName, LastName, Email, PasswordHash, PhoneNumber, Role)
VALUES
(
    'Thabo',
    'Mokoena',
    'thabo@raceday.co.za',
    'password_hash_1',
    '0825550101',
    'Organiser'
),
(
    'Naledi',
    'Dlamini',
    'naledi@raceday.co.za',
    'password_hash_2',
    '0835550102',
    'Organiser'
),
(
    'Kabelo',
    'Molefe',
    'kabelo@example.com',
    'password_hash_3',
    '0845550103',
    'Participant'
),
(
    'Lerato',
    'Mokoena',
    'lerato@example.com',
    'password_hash_4',
    '0855550104',
    'Participant'
);
-- Events
INSERT INTO Events
(
    OrganiserId,
    EventTypeId,
    LocationId,
    EventName,
    Description,
    EventDate,
    DistanceKm,
    RegistrationDeadline
)
VALUES
(
    1,
    1,
    1,
    'Pretoria City Run',
    'A community running event in Pretoria.',
    '2026-10-10',
    10.00,
    '2026-10-03'
),
(
    2,
    2,
    2,
    'Bloemfontein Family Walk',
    'A family friendly walking event.',
    '2026-11-07',
    5.00,
    '2026-10-31'
),
(
    1,
    3,
    3,
    'Durban Coastal Cycle',
    'A cycling event along the Durban coastline.',
    '2026-12-05',
    40.00,
    '2026-11-28'
);