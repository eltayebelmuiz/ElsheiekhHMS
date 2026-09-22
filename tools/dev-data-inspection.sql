-- Read-only Development inspection helper.
-- Run only against ElsheiekhHMS_Dev. It never inserts, updates, or deletes.
SET NOCOUNT ON;

SELECT DB_NAME() AS DatabaseName;

SELECT name AS TableName
FROM sys.tables
ORDER BY name;

SELECT Entity, CountValue
FROM
(
    SELECT 'Departments' AS Entity, COUNT_BIG(*) AS CountValue FROM dbo.Departments
    UNION ALL SELECT 'Doctors', COUNT_BIG(*) FROM dbo.Doctors
    UNION ALL SELECT 'Patients', COUNT_BIG(*) FROM dbo.Patients
    UNION ALL SELECT 'Appointments', COUNT_BIG(*) FROM dbo.Appointments
    UNION ALL SELECT 'WalkInQueueEntries', COUNT_BIG(*) FROM dbo.WalkInQueueEntries
    UNION ALL SELECT 'AspNetUsers', COUNT_BIG(*) FROM dbo.AspNetUsers
    UNION ALL SELECT 'AspNetRoles', COUNT_BIG(*) FROM dbo.AspNetRoles
    UNION ALL SELECT 'AuditLogs', COUNT_BIG(*) FROM dbo.AuditLogs
) AS counts
ORDER BY Entity;

SELECT Status, COUNT_BIG(*) AS CountValue
FROM dbo.Appointments
GROUP BY Status
ORDER BY Status;

SELECT Status, COUNT_BIG(*) AS CountValue
FROM dbo.WalkInQueueEntries
GROUP BY Status
ORDER BY Status;

SELECT UserName, DisplayName, SecurityState, LoginAllowed, CreatedAt
FROM dbo.AspNetUsers
ORDER BY UserName;

SELECT Name AS RoleName
FROM dbo.AspNetRoles
ORDER BY Name;

SELECT TOP (50)
    PatientCode, FirstName, MiddleName, ThirdName, LastName, DateOfBirth,
    Gender, Phone, City, NationalId, PassportNumber
FROM dbo.Patients
WHERE IsDeleted = 0
ORDER BY Id;

SELECT TOP (50)
    AppointmentCode, PatientId, DoctorId, DepartmentId, ScheduledDate,
    ScheduledTime, Status, Notes
FROM dbo.Appointments
WHERE IsDeleted = 0
ORDER BY ScheduledDate, ScheduledTime, Id;

SELECT TOP (50)
    QueueNumber, PatientId, DepartmentId, AppointmentId, DoctorId,
    QueueDate, Priority, Status, RegisteredAt, Notes
FROM dbo.WalkInQueueEntries
WHERE IsDeleted = 0
ORDER BY QueueDate DESC, SequenceNumber;

-- Phase13B has not started; this should return zero rows.
SELECT name AS UnexpectedClinicalTable
FROM sys.tables
WHERE name IN ('Encounters', 'Vitals', 'ClinicalNotes', 'Diagnoses', 'Prescriptions', 'Labs', 'Radiology', 'Billing');
