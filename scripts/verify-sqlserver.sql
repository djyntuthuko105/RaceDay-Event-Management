-- Assertions for disposable CI databases only; this does not modify application data.
USE RaceDayDB;
GO
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory)
    THROW 51000, 'EF migrations were not applied', 1;
IF (SELECT COUNT(*) FROM Users WHERE Role = 'Organiser') < 2
    OR (SELECT COUNT(*) FROM Users WHERE Role = 'Participant') < 2
    OR (SELECT COUNT(*) FROM Events) < 3
    THROW 51000, 'Demo seed is incomplete', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE name IN
    ('Users', 'EventTypes', 'Locations', 'Events', 'Categories', 'Enrolments', 'Results', 'EventImages', 'WeatherSnapshots')) <> 9
    THROW 51000, 'The migrated schema is incomplete', 1;
SELECT 'PASS: SQL Server EF migrations, nine tables and demo seed' AS Verification;
GO
USE RaceDayPlanningCI;
GO
IF (SELECT COUNT(*) FROM Users WHERE Role = 'Organiser') < 2
    OR (SELECT COUNT(*) FROM Users WHERE Role = 'Participant') < 2
    OR (SELECT COUNT(*) FROM Events) < 3
    THROW 51000, 'Planning seed is incomplete', 1;
IF NOT EXISTS (SELECT 1 FROM Categories) OR NOT EXISTS (SELECT 1 FROM Enrolments)
    THROW 51000, 'Planning categories or enrolments are missing', 1;
SELECT 'PASS: original Part 1 script on fresh SQL Server' AS Verification;
GO
