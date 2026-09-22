# ElsheiekhHMS Architecture Graph Report

## Snapshot

- **generatedAtUtc:** `2026-09-22T20:03:50.8560806+00:00`
- **gitBranch:** `main`
- **gitCommit:** `a341f442ad1e47d623756b82b934fd2644e5a768`
- **workingTreeDirty:** `True`
- **sourceFingerprint:** `49a773af9bccc9b5a2758d7534937db325860fca38813b17a89d557822ce8516`
- Framework: net10.0
- Structural extraction only: **0 LLM API calls / 0 LLM tokens**. No statistical community clustering is claimed; project ownership supplies the visual groups.

## Solution

`ElsheiekhHMS.slnx`

- [ElsheiekhHMS.Application](../ElsheiekhHMS.Application/ElsheiekhHMS.Application.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Core](../ElsheiekhHMS.Core/ElsheiekhHMS.Core.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Infrastructure](../ElsheiekhHMS.Infrastructure/ElsheiekhHMS.Infrastructure.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Tests](../ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj) — net10.0; solution member: True
- [ElsheiekhHMS.Web](../ElsheiekhHMS.Web/ElsheiekhHMS.Web.csproj) — net10.0; solution member: True

## Project Dependencies

Arrows mean references; extracted from evaluated ProjectReference items.

```text
ElsheiekhHMS.Application -> ElsheiekhHMS.Core
ElsheiekhHMS.Core -> (none)
ElsheiekhHMS.Infrastructure -> ElsheiekhHMS.Application, ElsheiekhHMS.Core
ElsheiekhHMS.Tests -> ElsheiekhHMS.Application, ElsheiekhHMS.Core, ElsheiekhHMS.Infrastructure, ElsheiekhHMS.Web
ElsheiekhHMS.Web -> ElsheiekhHMS.Application, ElsheiekhHMS.Infrastructure
```

## Core Foundation

- [ElsheiekhHMS.Core.Common.AuditableEntity](../ElsheiekhHMS.Core/Common/AuditableEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Common.BaseEntity](../ElsheiekhHMS.Core/Common/BaseEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Common.SoftDeletableEntity](../ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs) — abstract-class, public
- [ElsheiekhHMS.Core.Domain.Organization.Entities.Department](../ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Patients.Entities.Patient](../ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Patients.Enums.BloodGroup](../ElsheiekhHMS.Core/Domain/Patients/Enums/BloodGroup.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Patients.Enums.Gender](../ElsheiekhHMS.Core/Domain/Patients/Enums/Gender.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Entities.Appointment](../ElsheiekhHMS.Core/Domain/Scheduling/Entities/Appointment.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Entities.WalkInQueueEntry](../ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Enums.AppointmentStatus](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentStatus.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Enums.AppointmentType](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentType.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Enums.QueuePriority](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueuePriority.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Scheduling.Enums.QueueStatus](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueueStatus.cs) — enum, public
- [ElsheiekhHMS.Core.Domain.Staff.Entities.Doctor](../ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Staff.Entities.DoctorSchedule](../ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs) — class, public
- [ElsheiekhHMS.Core.Domain.Staff.Enums.DoctorStatus](../ElsheiekhHMS.Core/Domain/Staff/Enums/DoctorStatus.cs) — enum, public
- [ElsheiekhHMS.Core.Exceptions.BusinessRuleException](../ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs) — class, public
- [ElsheiekhHMS.Core.Exceptions.DomainException](../ElsheiekhHMS.Core/Exceptions/DomainException.cs) — class, public
- [ElsheiekhHMS.Core.Exceptions.DomainValidationException](../ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs) — class, public
- [ElsheiekhHMS.Core.Interfaces.IHasConcurrencyToken](../ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs) — interface, public

## Inheritance and Exceptions

Derived -> base (includes private test helper types and resolved external bases):

- `ElsheiekhHMS.Core.Common.AuditableEntity` -> `ElsheiekhHMS.Core.Common.BaseEntity` — ElsheiekhHMS.Core/Common/AuditableEntity.cs:L3
- `ElsheiekhHMS.Core.Common.SoftDeletableEntity` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs:L3
- `ElsheiekhHMS.Core.Domain.Organization.Entities.Department` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs:L6
- `ElsheiekhHMS.Core.Domain.Patients.Entities.Patient` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs:L8
- `ElsheiekhHMS.Core.Domain.Scheduling.Entities.Appointment` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Core/Domain/Scheduling/Entities/Appointment.cs:L8
- `ElsheiekhHMS.Core.Domain.Scheduling.Entities.WalkInQueueEntry` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs:L8
- `ElsheiekhHMS.Core.Domain.Staff.Entities.Doctor` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs:L8
- `ElsheiekhHMS.Core.Domain.Staff.Entities.DoctorSchedule` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs:L6
- `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` -> `ElsheiekhHMS.Core.Exceptions.DomainException` — ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs:L4
- `ElsheiekhHMS.Core.Exceptions.DomainException` -> `System.Exception` — ElsheiekhHMS.Core/Exceptions/DomainException.cs:L3
- `ElsheiekhHMS.Core.Exceptions.DomainValidationException` -> `ElsheiekhHMS.Core.Exceptions.DomainException` — ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs:L4
- `ElsheiekhHMS.Infrastructure.Auditing.EntityAuditSaveChangesInterceptor` -> `Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor` — ElsheiekhHMS.Infrastructure/Auditing/EntityAuditSaveChangesInterceptor.cs:L9
- `ElsheiekhHMS.Infrastructure.Migrations.AddAppointmentCodeAllocator` -> `Microsoft.EntityFrameworkCore.Migrations.Migration` — ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.cs:L8
- `ElsheiekhHMS.Infrastructure.Migrations.AddAppointmentQueueLink` -> `Microsoft.EntityFrameworkCore.Migrations.Migration` — ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.cs:L8
- `ElsheiekhHMS.Infrastructure.Migrations.AddPhase05IdentityAndAuditLog` -> `Microsoft.EntityFrameworkCore.Migrations.Migration` — ElsheiekhHMS.Infrastructure/Migrations/20260921182651_AddPhase05IdentityAndAuditLog.cs:L9
- `ElsheiekhHMS.Infrastructure.Migrations.AddPhase07AllocatorInfrastructure` -> `Microsoft.EntityFrameworkCore.Migrations.Migration` — ElsheiekhHMS.Infrastructure/Migrations/20260921230714_AddPhase07AllocatorInfrastructure.cs:L9
- `ElsheiekhHMS.Infrastructure.Migrations.ElsheiekhHmsDbContextModelSnapshot` -> `Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot` — ElsheiekhHMS.Infrastructure/Migrations/ElsheiekhHmsDbContextModelSnapshot.cs:L13
- `ElsheiekhHMS.Infrastructure.Migrations.InitialCreate` -> `Microsoft.EntityFrameworkCore.Migrations.Migration` — ElsheiekhHMS.Infrastructure/Migrations/20260921111137_InitialCreate.cs:L9
- `ElsheiekhHMS.Infrastructure.Persistence.ElsheiekhHmsDbContext` -> `Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ElsheiekhHMS.Infrastructure.Identity.Entities.ApplicationUser>` — ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs:L13
- `ElsheiekhHMS.Tests.Integration.Persistence.AppointmentArrivalQueueSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/AppointmentArrivalQueueSqlServerTests.cs:L118
- `ElsheiekhHMS.Tests.Integration.Persistence.AppointmentCodeAllocatorSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/AppointmentCodeAllocatorSqlServerTests.cs:L137
- `ElsheiekhHMS.Tests.Integration.Persistence.AppointmentServiceSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/AppointmentServiceSqlServerTests.cs:L297
- `ElsheiekhHMS.Tests.Integration.Persistence.DepartmentServiceSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/DepartmentServiceSqlServerTests.cs:L159
- `ElsheiekhHMS.Tests.Integration.Persistence.ElsheiekhHmsDbContextSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/ElsheiekhHmsDbContextSqlServerTests.cs:L208
- `ElsheiekhHMS.Tests.Integration.Persistence.IdentityAndAuditLogSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/IdentityAndAuditLogSqlServerTests.cs:L300
- `ElsheiekhHMS.Tests.Integration.Persistence.PatientServiceSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/PatientServiceSqlServerTests.cs:L97
- `ElsheiekhHMS.Tests.Integration.Persistence.PerformanceSmokeSqlServerTests.CommandCountingInterceptor` -> `Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor` — ElsheiekhHMS.Tests/Integration/Persistence/PerformanceSmokeSqlServerTests.cs:L258
- `ElsheiekhHMS.Tests.Integration.Persistence.PerformanceSmokeSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/PerformanceSmokeSqlServerTests.cs:L253
- `ElsheiekhHMS.Tests.Integration.Persistence.QueueServiceSqlServerTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Integration/Persistence/QueueServiceSqlServerTests.cs:L437
- `ElsheiekhHMS.Tests.Unit.Application.Appointments.AppointmentServiceTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentServiceTests.cs:L229
- `ElsheiekhHMS.Tests.Unit.Application.Departments.DepartmentServiceTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentServiceTests.cs:L250
- `ElsheiekhHMS.Tests.Unit.Application.Patients.PatientServiceTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Unit/Application/Patients/PatientServiceTests.cs:L226
- `ElsheiekhHMS.Tests.Unit.Application.Queue.QueueServiceTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Unit/Application/Queue/QueueServiceTests.cs:L185
- `ElsheiekhHMS.Tests.Unit.Domain.Common.AuditableEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.AuditableEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs:L28
- `ElsheiekhHMS.Tests.Unit.Domain.Common.BaseEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.BaseEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs:L15
- `ElsheiekhHMS.Tests.Unit.Domain.Common.SoftDeletableEntityTests.TestEntity` -> `ElsheiekhHMS.Core.Common.SoftDeletableEntity` — ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs:L18
- `ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing.AuditEventWriterTests.FixedTimeProvider` -> `System.TimeProvider` — ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditEventWriterTests.cs:L192

## Interfaces

- `ElsheiekhHMS.Application.Appointments.IAppointmentService`
  - Implementations: ElsheiekhHMS.Application.Appointments.AppointmentService, ElsheiekhHMS.Tests.Unit.Application.Workflows.AppointmentArrivalQueueServiceTests.FakeAppointmentService, ElsheiekhHMS.Tests.Unit.Application.Workflows.PatientIntakeAppointmentServiceTests.FakeAppointmentService
- `ElsheiekhHMS.Application.Appointments.Persistence.IAppointmentPersistence`
  - Implementations: ElsheiekhHMS.Infrastructure.Persistence.Appointments.AppointmentPersistence, ElsheiekhHMS.Tests.Unit.Application.Appointments.AppointmentServiceTests.FakePersistence
- `ElsheiekhHMS.Application.Common.Auditing.IAuditEventWriter`
  - Implementations: ElsheiekhHMS.Infrastructure.Auditing.AuditEventWriter, ElsheiekhHMS.Tests.Integration.Persistence.DepartmentServiceSqlServerTests.InvalidAuditWriter, ElsheiekhHMS.Tests.Unit.Application.Appointments.AppointmentServiceTests.RecordingAuditWriter, ElsheiekhHMS.Tests.Unit.Application.Departments.DepartmentServiceTests.RecordingAuditWriter, ElsheiekhHMS.Tests.Unit.Application.Patients.PatientServiceTests.RecordingAuditWriter, ElsheiekhHMS.Tests.Unit.Application.Queue.QueueServiceTests.RecordingAuditWriter
- `ElsheiekhHMS.Application.Common.Security.ICurrentUser`
  - Implementations: ElsheiekhHMS.Tests.Integration.Persistence.AppointmentArrivalQueueSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.AppointmentServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.DepartmentServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.ElsheiekhHmsDbContextSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.IdentityAndAuditLogSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.PatientServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.PerformanceSmokeSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.QueueServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Appointments.AppointmentServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Departments.DepartmentServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Patients.PatientServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Queue.QueueServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Workflows.AppointmentArrivalQueueServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Workflows.PatientIntakeAppointmentServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing.AuditEventWriterTests.TestCurrentUser, ElsheiekhHMS.Web.Security.CurrentUserAccessor
- `ElsheiekhHMS.Application.Common.Validation.IRequestValidator<TRequest>`
  - Implementations: ElsheiekhHMS.Application.Appointments.Validation.AppointmentActionRequestValidator, ElsheiekhHMS.Application.Appointments.Validation.AppointmentSearchRequestValidator, ElsheiekhHMS.Application.Appointments.Validation.CancelAppointmentRequestValidator, ElsheiekhHMS.Application.Appointments.Validation.CreateAppointmentRequestValidator, ElsheiekhHMS.Application.Common.Validation.PageRequestValidator, ElsheiekhHMS.Application.Departments.Validation.CreateDepartmentRequestValidator, ElsheiekhHMS.Application.Departments.Validation.DepartmentSearchRequestValidator, ElsheiekhHMS.Application.Departments.Validation.UpdateDepartmentRequestValidator, ElsheiekhHMS.Application.Patients.Validation.PatientSearchRequestValidator, ElsheiekhHMS.Application.Patients.Validation.RegisterPatientRequestValidator, ElsheiekhHMS.Application.Patients.Validation.UpdatePatientContactDetailsRequestValidator, ElsheiekhHMS.Application.Patients.Validation.UpdatePatientDemographicsRequestValidator, ElsheiekhHMS.Application.Patients.Validation.UpdatePatientIdentifiersRequestValidator, ElsheiekhHMS.Application.Queue.Validation.AddAppointmentQueueEntryRequestValidator, ElsheiekhHMS.Application.Queue.Validation.AddWalkInQueueEntryRequestValidator, ElsheiekhHMS.Application.Queue.Validation.QueueEntryActionRequestValidator, ElsheiekhHMS.Application.Queue.Validation.QueueSearchRequestValidator, ElsheiekhHMS.Application.Queue.Validation.SendToDoctorRequestValidator
- `ElsheiekhHMS.Application.Departments.IDepartmentService`
  - Implementations: ElsheiekhHMS.Application.Departments.DepartmentService
- `ElsheiekhHMS.Application.Departments.Persistence.IDepartmentPersistence`
  - Implementations: ElsheiekhHMS.Infrastructure.Persistence.Departments.DepartmentPersistence, ElsheiekhHMS.Tests.Unit.Application.Departments.DepartmentServiceTests.FakeDepartmentPersistence
- `ElsheiekhHMS.Application.Patients.IPatientService`
  - Implementations: ElsheiekhHMS.Application.Patients.PatientService, ElsheiekhHMS.Tests.Unit.Application.Workflows.PatientIntakeAppointmentServiceTests.FakePatientService
- `ElsheiekhHMS.Application.Patients.Persistence.IPatientPersistence`
  - Implementations: ElsheiekhHMS.Infrastructure.Persistence.Patients.PatientPersistence, ElsheiekhHMS.Tests.Unit.Application.Patients.PatientServiceTests.FakePatientPersistence
- `ElsheiekhHMS.Application.Queue.IQueueService`
  - Implementations: ElsheiekhHMS.Application.Queue.QueueService, ElsheiekhHMS.Tests.Unit.Application.Workflows.AppointmentArrivalQueueServiceTests.FakeQueueService
- `ElsheiekhHMS.Application.Queue.Persistence.IQueuePersistence`
  - Implementations: ElsheiekhHMS.Infrastructure.Persistence.Queue.QueuePersistence, ElsheiekhHMS.Tests.Unit.Application.Queue.QueueServiceTests.FakePersistence
- `ElsheiekhHMS.Application.Workflows.AppointmentArrival.IAppointmentArrivalQueueService`
  - Implementations: ElsheiekhHMS.Application.Workflows.AppointmentArrival.AppointmentArrivalQueueService
- `ElsheiekhHMS.Application.Workflows.PatientIntake.IPatientIntakeAppointmentService`
  - Implementations: ElsheiekhHMS.Application.Workflows.PatientIntake.PatientIntakeAppointmentService
- `ElsheiekhHMS.Core.Interfaces.IHasConcurrencyToken`
  - Implementations: ElsheiekhHMS.Core.Domain.Patients.Entities.Patient, ElsheiekhHMS.Core.Domain.Scheduling.Entities.Appointment, ElsheiekhHMS.Core.Domain.Scheduling.Entities.WalkInQueueEntry
- `Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<ElsheiekhHMS.Infrastructure.Auditing.Entities.AuditLog>`
  - Implementations: ElsheiekhHMS.Infrastructure.Auditing.Configurations.AuditLogConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.AppointmentConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.DepartmentConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.DoctorConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.DoctorScheduleConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.PatientConfiguration, ElsheiekhHMS.Infrastructure.Configurations.Entities.WalkInQueueEntryConfiguration, ElsheiekhHMS.Infrastructure.Identity.Configurations.ApplicationUserConfiguration, ElsheiekhHMS.Infrastructure.Persistence.Allocation.AppointmentCodeAllocationConfiguration, ElsheiekhHMS.Infrastructure.Persistence.Allocation.PatientCodeAllocationConfiguration, ElsheiekhHMS.Infrastructure.Persistence.Allocation.QueueTicketAllocationConfiguration
- `Microsoft.Extensions.DependencyInjection.IServiceScope`
  - Implementations: ElsheiekhHMS.Tests.Unit.Infrastructure.Health.HealthCheckTests.ThrowingScope, ElsheiekhHMS.Tests.Unit.Web.Security.AuthenticationStateValidationTests.SingleScope
- `Microsoft.Extensions.DependencyInjection.IServiceScopeFactory`
  - Implementations: ElsheiekhHMS.Tests.Unit.Infrastructure.Health.HealthCheckTests.ThrowingScopeFactory, ElsheiekhHMS.Tests.Unit.Web.Security.AuthenticationStateValidationTests.SingleServiceScopeFactory
- `System.IDisposable`
  - Implementations: ElsheiekhHMS.Tests.Unit.Infrastructure.Observability.RequestObservabilityMiddlewareTests.NoopDisposable
- `System.IEquatable<ElsheiekhHMS.Application.Appointments.Contracts.AppointmentActionRequest>`
  - Implementations: ElsheiekhHMS.Application.Appointments.Contracts.AppointmentActionRequest, ElsheiekhHMS.Application.Appointments.Contracts.AppointmentDetailsDto, ElsheiekhHMS.Application.Appointments.Contracts.AppointmentSearchRequest, ElsheiekhHMS.Application.Appointments.Contracts.AppointmentSummaryDto, ElsheiekhHMS.Application.Appointments.Contracts.CancelAppointmentRequest, ElsheiekhHMS.Application.Appointments.Contracts.CreateAppointmentRequest, ElsheiekhHMS.Application.Common.Auditing.AuditEventRequest, ElsheiekhHMS.Application.Common.Contracts.PageRequest, ElsheiekhHMS.Application.Common.Results.ServiceError, ElsheiekhHMS.Application.Common.Validation.ValidationError, ElsheiekhHMS.Application.Departments.Contracts.CreateDepartmentRequest, ElsheiekhHMS.Application.Departments.Contracts.DepartmentDetailsDto, ElsheiekhHMS.Application.Departments.Contracts.DepartmentSearchRequest, ElsheiekhHMS.Application.Departments.Contracts.DepartmentSummaryDto, ElsheiekhHMS.Application.Departments.Contracts.UpdateDepartmentRequest, ElsheiekhHMS.Application.Patients.Contracts.PatientDetailsDto, ElsheiekhHMS.Application.Patients.Contracts.PatientSearchRequest, ElsheiekhHMS.Application.Patients.Contracts.PatientSummaryDto, ElsheiekhHMS.Application.Patients.Contracts.RegisterPatientRequest, ElsheiekhHMS.Application.Patients.Contracts.UpdatePatientContactDetailsRequest, ElsheiekhHMS.Application.Patients.Contracts.UpdatePatientDemographicsRequest, ElsheiekhHMS.Application.Patients.Contracts.UpdatePatientIdentifiersRequest, ElsheiekhHMS.Application.Queue.Contracts.AddAppointmentQueueEntryRequest, ElsheiekhHMS.Application.Queue.Contracts.AddWalkInQueueEntryRequest, ElsheiekhHMS.Application.Queue.Contracts.QueueEntryActionRequest, ElsheiekhHMS.Application.Queue.Contracts.QueueEntryDetailsDto, ElsheiekhHMS.Application.Queue.Contracts.QueueEntrySummaryDto, ElsheiekhHMS.Application.Queue.Contracts.QueueSearchRequest, ElsheiekhHMS.Application.Queue.Contracts.SendToDoctorRequest, ElsheiekhHMS.Application.Workflows.AppointmentArrival.AppointmentArrivalQueueRequest, ElsheiekhHMS.Application.Workflows.AppointmentArrival.AppointmentArrivalQueueResult, ElsheiekhHMS.Application.Workflows.PatientIntake.PatientIntakeAppointmentRequest, ElsheiekhHMS.Application.Workflows.PatientIntake.PatientIntakeAppointmentResult, ElsheiekhHMS.Infrastructure.Persistence.Appointments.AppointmentPersistence.AppointmentDetailsProjection, ElsheiekhHMS.Infrastructure.Persistence.Patients.PatientPersistence.PatientDetailsProjection, ElsheiekhHMS.Infrastructure.Persistence.Queue.QueuePersistence.QueueEntryDetailsProjection, ElsheiekhHMS.Tests.Integration.Persistence.AppointmentArrivalQueueSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.AppointmentServiceSqlServerTests.SetupRecords, ElsheiekhHMS.Tests.Integration.Persistence.AppointmentServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.DepartmentServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.IdentityAndAuditLogSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.PatientServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.PerformanceSmokeSqlServerTests.SmokeData, ElsheiekhHMS.Tests.Integration.Persistence.PerformanceSmokeSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Integration.Persistence.QueueServiceSqlServerTests.SetupRecords, ElsheiekhHMS.Tests.Integration.Persistence.QueueServiceSqlServerTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Application.Workflows.AppointmentArrivalQueueServiceTests.TestCurrentUser, ElsheiekhHMS.Tests.Unit.Infrastructure.Observability.RequestObservabilityMiddlewareTests.LogRecord
- `System.IServiceProvider`
  - Implementations: ElsheiekhHMS.Tests.Unit.Infrastructure.Health.HealthCheckTests.ThrowingServiceProvider, ElsheiekhHMS.Tests.Unit.Web.Security.AuthenticationStateValidationTests.SingleServiceProvider

## Tests

11 classes; 76 methods; 113 statically enumerable cases. This is discovery from source, not an execution result.

- [ElsheiekhHMS.Tests.Unit.Application.Auditing.AuditVocabularyTests](../ElsheiekhHMS.Tests/Unit/Application/Auditing/AuditVocabularyTests.cs) — 3 cases
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditActions` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditActorKinds` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditCategories` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Common.AuditableEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs) — 2 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Common.BaseEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs) — 1 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Common.SoftDeletableEntityTests](../ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs) — 1 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.SoftDeletableEntity` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Exceptions.DomainExceptionTests](../ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs) — 6 cases
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Organization.DepartmentTests](../ElsheiekhHMS.Tests/Unit/Domain/Organization/DepartmentTests.cs) — 9 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Organization.Entities.Department` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Patients.PatientTests](../ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs) — 33 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.SoftDeletableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Patients.Entities.Patient` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Patients.Enums.BloodGroup` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Patients.Enums.Gender` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Interfaces.IHasConcurrencyToken` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Staff.DoctorScheduleTests](../ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorScheduleTests.cs) — 21 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.SoftDeletableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Staff.Entities.Doctor` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Staff.Entities.DoctorSchedule` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Domain.Staff.DoctorTests](../ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs) — 27 cases
  - Uses/tests `ElsheiekhHMS.Core.Common.AuditableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.BaseEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Common.SoftDeletableEntity` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Staff.Entities.Doctor` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Domain.Staff.Enums.DoctorStatus` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.BusinessRuleException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainException` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Core.Exceptions.DomainValidationException` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing.AuditEventWriterTests](../ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditEventWriterTests.cs) — 7 cases
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditActions` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditActorKinds` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditCategories` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Auditing.AuditEventRequest` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Application.Common.Security.ICurrentUser` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Infrastructure.Auditing.AuditEventWriter` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Infrastructure.Auditing.Entities.AuditLog` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Infrastructure.Persistence.ElsheiekhHmsDbContext` (source type/member binding; includes inherited foundation dependencies)
- [ElsheiekhHMS.Tests.Unit.Infrastructure.Auditing.AuditLogModelTests](../ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditLogModelTests.cs) — 3 cases
  - Uses/tests `ElsheiekhHMS.Infrastructure.Auditing.Entities.AuditLog` (source type/member binding; includes inherited foundation dependencies)
  - Uses/tests `ElsheiekhHMS.Infrastructure.Persistence.ElsheiekhHmsDbContext` (source type/member binding; includes inherited foundation dependencies)

## Packages

Evaluated direct PackageReference items only. Framework/SDK auto-references and transitives are outside this list.

- **ElsheiekhHMS.Application:** Microsoft.Extensions.DependencyInjection.Abstractions 10.0.12
- **ElsheiekhHMS.Core:** none
- **ElsheiekhHMS.Infrastructure:** Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.12; Microsoft.EntityFrameworkCore 10.0.12; Microsoft.EntityFrameworkCore.Design 10.0.12; Microsoft.EntityFrameworkCore.SqlServer 10.0.12
- **ElsheiekhHMS.Tests:** coverlet.collector 6.0.4; Microsoft.NET.Test.Sdk 17.14.1; xunit 2.9.3; xunit.runner.visualstudio 3.1.4
- **ElsheiekhHMS.Web:** Microsoft.EntityFrameworkCore.Design 10.0.12

## Architecture Checks

- coreIndependent: `True`
- circularProjectDependenciesDetected: `False`
- unexpectedDependencies: `[
        "ElsheiekhHMS.Tests: observed references differ from documented HMS layer rules"
      ]`
- forbiddenCoreUsings: `[]`
- ruleSource: `Documented HMS layer policy; observed edges extracted separately`
- efCorePresent: `True`
- sqlServerPresent: `True`
- identityPackagePresent: `True`
- Identity source references: `[
      "ElsheiekhHMS.Infrastructure/Identity/AdministratorBootstrapper.cs",
      "ElsheiekhHMS.Infrastructure/Identity/Entities/ApplicationUser.cs",
      "ElsheiekhHMS.Infrastructure/Identity/IdentityRoleSeeder.cs",
      "ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs",
      "ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs",
      "ElsheiekhHMS.Tests/Integration/Persistence/IdentityAndAuditLogSqlServerTests.cs",
      "ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityOptionsTests.cs",
      "ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityRoleSeederTests.cs",
      "ElsheiekhHMS.Tests/Unit/Web/Security/AuthenticationStateValidationTests.cs",
      "ElsheiekhHMS.Web/Program.cs",
      "ElsheiekhHMS.Web/Security/HmsRevalidatingAuthenticationStateProvider.cs"
    ]`

## Current Development Phase

Declared by README; not inferred from the existence of classes:

- > **Current development stage:** Phase 12G — Dashboard and UI/UX hardening complete; backend remains accepted and frozen — README.md:L5
- > **Phase 01:** ✅ Complete — README.md:L7
- > **Phase 02 Setup:** ✅ Complete and verified — README.md:L9
- > **Phase 03A / 03B / 03C:** ✅ Complete — README.md:L11
- > **Phase 03D:** ✅ Implemented and verified — README.md:L13
- > **Phase 04A:** ✅ EF Core foundation implemented and verified — README.md:L15
- > **Phase 04B:** ✅ Entity Fluent mappings implemented and verified — README.md:L17
- > **Phase 04C:** ✅ Relationships, filters, indexes, constraints, and concurrency metadata implemented and verified — README.md:L19
- > **Phase 04D-A:** ✅ SQL Server environment readiness verified — README.md:L21
- > **Phase 04D-B0:** ✅ EF design-time tooling verified — README.md:L23
- > **Phase 04D-B:** ✅ Initial migration generated and inspected; database not created or updated — README.md:L25
- > **Phase 04D-C:** ✅ Initial migration applied and local SQL Server schema verified — README.md:L27
- > **Phase 04E:** ✅ Persistence integration tests implemented and verified — README.md:L29
- > **Phase 05A:** ✅ Identity foundation and security-model amendment implemented and verified — README.md:L31
- > **Phase 05B:** ✅ Roles, authorization policies, fallback policy, and password-free role seeder implemented and verified — README.md:L33
- > **Phase 05C:** ✅ Current-user foundation and entity lifecycle auditing implemented and verified — README.md:L35
- > **Phase 05C-A:** ✅ Durable security/business AuditLog model and writer foundation implemented and verified; event-producing workflows, retention policy, IP/UserAgent capture, and clinical/read auditing remain deferred — README.md:L37
- > **Phase 05D:** ✅ Identity and AuditLog migration applied and verified against development and isolated integration SQL Server databases; pending-model suppression removed — README.md:L39
- > **Phase 05E:** ✅ Security integration and hardening implemented and verified; account workflows, durable event producers, retention, IP/UserAgent capture, clinical/read auditing, and UI remain deferred — README.md:L41
- > **Phase 07S:** ✅ Database-backed allocator prerequisite implemented, migration applied, physical schema verified, and regression suite passed — README.md:L43
- > **Phase 07A:** ✅ Patient Application Service implemented, tested, and reviewed; registration and PatientCode allocation remain one-save transactional — README.md:L45
- > **Phase 07B:** ✅ Department Application Service complete, implemented, tested, and reviewed — README.md:L47
- > **Phase 07C-P:** ✅ AppointmentCode allocator prerequisite complete; migration applied and physical schema verified — README.md:L49
- > **Phase 07C:** ✅ Appointment Application Service complete — README.md:L51
- > **Phase 07D:** ✅ Queue Application Service complete, implemented, verified, and closed out — README.md:L53
- > **Phase 07E:** ⏸ Doctor/Provider Application Service deferred; Doctor/Provider contracts and Doctor↔ApplicationUser ownership-aware authorization are not yet approved — README.md:L55
- > **Phase 07:** ✅ Application Services complete for the approved scope; 07E is explicitly deferred — README.md:L57
- > **Phase 08A:** ✅ Staff Patient Intake & Appointment Scheduling implemented, tested, and verified; partial-success retry semantics preserve newly registered Patients — README.md:L59
- > **Phase 08B-P:** ✅ Appointment↔Queue durable-link prerequisite implemented; `AddAppointmentQueueLink` applied and physically verified — README.md:L61
- > **Phase 08B:** ✅ Appointment Arrival & Queue Handoff implemented, verified, and closed out — README.md:L63
- > **Phase 08:** ✅ Business Workflows complete; 08A, 08B-P, and 08B are complete; 08C is not required — README.md:L65
- > **Phase 09A:** ✅ Structured Application Observability complete — README.md:L67
- > **Phase 09B:** ✅ SQL Server readiness health checks complete — README.md:L69
- > **Phase 09:** ✅ Enterprise Infrastructure complete; 09A and 09B complete; 09C is not required — README.md:L71
- > **Phase 10:** ✅ Testing & Hardening complete; 10A, 10B, 10C, and 10D complete; 418 tests passing — README.md:L73
- > **Phase 11:** ✅ Backend Review complete; 11A passed, 11B was not required, and 11C accepted and froze the backend baseline — README.md:L75
- > **Phase 12A:** ✅ Blazor UI architecture, application shell, navigation foundation, design tokens, reusable feedback/form/list patterns, and accessibility baseline established — README.md:L77
- > **Phase 12B:** ✅ Authentication presentation, anonymous/authenticated shell separation, current-user/logout controls, unauthorized UX, and role-aware navigation complete; external browser QA remains required — README.md:L80
- > **Phase 12C:** ✅ Patient registry, server-side search/pagination, registration, details, and concurrency-safe edit UI complete; external browser QA remains required — README.md:L82
- > **Phase 12D:** ✅ Department registry, search/filter/sort, create, details, edit, and one-way deactivation UI complete; external browser QA remains required — README.md:L84
- > **Phase 12E:** ✅ Appointment registry, bounded Patient lookup, scheduling, lifecycle details/actions, Kigali presentation, and concurrency-safe mutation UI complete; external browser QA remains required — README.md:L86
- > **Phase 12F:** ✅ Queue registry, explicit walk-in workflow, queue lifecycle UI, and Appointment→Queue handoff UI complete; external browser QA remains required — README.md:L88
- > **Phase 12G:** ✅ Operational dashboard, cross-module UI hardening, accessibility source review, and consolidated QA handoff complete; external browser QA remains required — README.md:L90
-  /  01  /  Solution & Architecture  /  ✅ Complete  /  — README.md:L407
-  /  02  /  Core Foundation  /  ✅ Complete  /  — README.md:L408
-  /  03  /  Domain Entities  /  ✅ Complete  /  — README.md:L409
-  /  04  /  EF Core & Database  /  ✅ Complete  /  — README.md:L410
-  /  05  /  Identity & Security  /  ✅ Complete (05A, 05B, 05C, 05C-A, 05D, and 05E)  /  — README.md:L411
-  /  06  /  DTOs & Validation  /  ✅ Complete (06A–06D)  /  — README.md:L412
-  /  07  /  Application Services  /  ✅ Complete for approved scope (07S, 07A, 07B, 07C-P, 07C, 07D; 07E deferred)  /  — README.md:L413
-  /  08  /  Business Workflows  /  ✅ Complete (08A, 08B-P, 08B)  /  — README.md:L414
-  /  09  /  Enterprise Infrastructure  /  ✅ Complete (09A, 09B)  /  — README.md:L415

## Important Files

- [ElsheiekhHMS.Application/ApplicationServiceExtensions.cs](../ElsheiekhHMS.Application/ApplicationServiceExtensions.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/AppointmentService.cs](../ElsheiekhHMS.Application/Appointments/AppointmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/AppointmentActionRequest.cs](../ElsheiekhHMS.Application/Appointments/Contracts/AppointmentActionRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/AppointmentDetailsDto.cs](../ElsheiekhHMS.Application/Appointments/Contracts/AppointmentDetailsDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSearchRequest.cs](../ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSearchRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSortField.cs](../ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSortField.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSummaryDto.cs](../ElsheiekhHMS.Application/Appointments/Contracts/AppointmentSummaryDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/CancelAppointmentRequest.cs](../ElsheiekhHMS.Application/Appointments/Contracts/CancelAppointmentRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Contracts/CreateAppointmentRequest.cs](../ElsheiekhHMS.Application/Appointments/Contracts/CreateAppointmentRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/IAppointmentService.cs](../ElsheiekhHMS.Application/Appointments/IAppointmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Persistence/AppointmentPersistenceSaveStatus.cs](../ElsheiekhHMS.Application/Appointments/Persistence/AppointmentPersistenceSaveStatus.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Persistence/IAppointmentPersistence.cs](../ElsheiekhHMS.Application/Appointments/Persistence/IAppointmentPersistence.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Validation/AppointmentActionRequestValidator.cs](../ElsheiekhHMS.Application/Appointments/Validation/AppointmentActionRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Validation/AppointmentSearchRequestValidator.cs](../ElsheiekhHMS.Application/Appointments/Validation/AppointmentSearchRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Validation/CancelAppointmentRequestValidator.cs](../ElsheiekhHMS.Application/Appointments/Validation/CancelAppointmentRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Appointments/Validation/CreateAppointmentRequestValidator.cs](../ElsheiekhHMS.Application/Appointments/Validation/CreateAppointmentRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Auditing/AuditActions.cs](../ElsheiekhHMS.Application/Common/Auditing/AuditActions.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Auditing/AuditActorKinds.cs](../ElsheiekhHMS.Application/Common/Auditing/AuditActorKinds.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Auditing/AuditCategories.cs](../ElsheiekhHMS.Application/Common/Auditing/AuditCategories.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Auditing/AuditEventRequest.cs](../ElsheiekhHMS.Application/Common/Auditing/AuditEventRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Auditing/IAuditEventWriter.cs](../ElsheiekhHMS.Application/Common/Auditing/IAuditEventWriter.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Contracts/PageRequest.cs](../ElsheiekhHMS.Application/Common/Contracts/PageRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Contracts/PagedResult.cs](../ElsheiekhHMS.Application/Common/Contracts/PagedResult.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Contracts/SortDirection.cs](../ElsheiekhHMS.Application/Common/Contracts/SortDirection.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Results/ServiceError.cs](../ElsheiekhHMS.Application/Common/Results/ServiceError.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Results/ServiceResult.cs](../ElsheiekhHMS.Application/Common/Results/ServiceResult.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Security/ICurrentUser.cs](../ElsheiekhHMS.Application/Common/Security/ICurrentUser.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Security/PolicyNames.cs](../ElsheiekhHMS.Application/Common/Security/PolicyNames.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Security/RoleNames.cs](../ElsheiekhHMS.Application/Common/Security/RoleNames.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Validation/IRequestValidator.cs](../ElsheiekhHMS.Application/Common/Validation/IRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Validation/PageRequestValidator.cs](../ElsheiekhHMS.Application/Common/Validation/PageRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Validation/ValidationError.cs](../ElsheiekhHMS.Application/Common/Validation/ValidationError.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Common/Validation/ValidationResult.cs](../ElsheiekhHMS.Application/Common/Validation/ValidationResult.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/CreateDepartmentRequest.cs](../ElsheiekhHMS.Application/Departments/Contracts/CreateDepartmentRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/DepartmentDetailsDto.cs](../ElsheiekhHMS.Application/Departments/Contracts/DepartmentDetailsDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/DepartmentSearchRequest.cs](../ElsheiekhHMS.Application/Departments/Contracts/DepartmentSearchRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/DepartmentSortField.cs](../ElsheiekhHMS.Application/Departments/Contracts/DepartmentSortField.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/DepartmentSummaryDto.cs](../ElsheiekhHMS.Application/Departments/Contracts/DepartmentSummaryDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Contracts/UpdateDepartmentRequest.cs](../ElsheiekhHMS.Application/Departments/Contracts/UpdateDepartmentRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/DepartmentService.cs](../ElsheiekhHMS.Application/Departments/DepartmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/IDepartmentService.cs](../ElsheiekhHMS.Application/Departments/IDepartmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Persistence/IDepartmentPersistence.cs](../ElsheiekhHMS.Application/Departments/Persistence/IDepartmentPersistence.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Validation/CreateDepartmentRequestValidator.cs](../ElsheiekhHMS.Application/Departments/Validation/CreateDepartmentRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Validation/DepartmentSearchRequestValidator.cs](../ElsheiekhHMS.Application/Departments/Validation/DepartmentSearchRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Departments/Validation/UpdateDepartmentRequestValidator.cs](../ElsheiekhHMS.Application/Departments/Validation/UpdateDepartmentRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/PatientDetailsDto.cs](../ElsheiekhHMS.Application/Patients/Contracts/PatientDetailsDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/PatientSearchRequest.cs](../ElsheiekhHMS.Application/Patients/Contracts/PatientSearchRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/PatientSortField.cs](../ElsheiekhHMS.Application/Patients/Contracts/PatientSortField.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/PatientSummaryDto.cs](../ElsheiekhHMS.Application/Patients/Contracts/PatientSummaryDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/RegisterPatientRequest.cs](../ElsheiekhHMS.Application/Patients/Contracts/RegisterPatientRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientContactDetailsRequest.cs](../ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientContactDetailsRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientDemographicsRequest.cs](../ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientDemographicsRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientIdentifiersRequest.cs](../ElsheiekhHMS.Application/Patients/Contracts/UpdatePatientIdentifiersRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/IPatientService.cs](../ElsheiekhHMS.Application/Patients/IPatientService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/PatientService.cs](../ElsheiekhHMS.Application/Patients/PatientService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Persistence/IPatientPersistence.cs](../ElsheiekhHMS.Application/Patients/Persistence/IPatientPersistence.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Persistence/PatientPersistenceSaveStatus.cs](../ElsheiekhHMS.Application/Patients/Persistence/PatientPersistenceSaveStatus.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Validation/PatientSearchRequestValidator.cs](../ElsheiekhHMS.Application/Patients/Validation/PatientSearchRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Validation/RegisterPatientRequestValidator.cs](../ElsheiekhHMS.Application/Patients/Validation/RegisterPatientRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Validation/UpdatePatientContactDetailsRequestValidator.cs](../ElsheiekhHMS.Application/Patients/Validation/UpdatePatientContactDetailsRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Validation/UpdatePatientDemographicsRequestValidator.cs](../ElsheiekhHMS.Application/Patients/Validation/UpdatePatientDemographicsRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Patients/Validation/UpdatePatientIdentifiersRequestValidator.cs](../ElsheiekhHMS.Application/Patients/Validation/UpdatePatientIdentifiersRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/AddWalkInQueueEntryRequest.cs](../ElsheiekhHMS.Application/Queue/Contracts/AddWalkInQueueEntryRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/QueueEntryActionRequest.cs](../ElsheiekhHMS.Application/Queue/Contracts/QueueEntryActionRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/QueueEntryDetailsDto.cs](../ElsheiekhHMS.Application/Queue/Contracts/QueueEntryDetailsDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/QueueEntrySummaryDto.cs](../ElsheiekhHMS.Application/Queue/Contracts/QueueEntrySummaryDto.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/QueueSearchRequest.cs](../ElsheiekhHMS.Application/Queue/Contracts/QueueSearchRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/QueueSortField.cs](../ElsheiekhHMS.Application/Queue/Contracts/QueueSortField.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Contracts/SendToDoctorRequest.cs](../ElsheiekhHMS.Application/Queue/Contracts/SendToDoctorRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/IQueueService.cs](../ElsheiekhHMS.Application/Queue/IQueueService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Persistence/IQueuePersistence.cs](../ElsheiekhHMS.Application/Queue/Persistence/IQueuePersistence.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Persistence/QueuePersistenceSaveStatus.cs](../ElsheiekhHMS.Application/Queue/Persistence/QueuePersistenceSaveStatus.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/QueueService.cs](../ElsheiekhHMS.Application/Queue/QueueService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Validation/AddWalkInQueueEntryRequestValidator.cs](../ElsheiekhHMS.Application/Queue/Validation/AddWalkInQueueEntryRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Validation/QueueEntryActionRequestValidator.cs](../ElsheiekhHMS.Application/Queue/Validation/QueueEntryActionRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Validation/QueueSearchRequestValidator.cs](../ElsheiekhHMS.Application/Queue/Validation/QueueSearchRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Queue/Validation/SendToDoctorRequestValidator.cs](../ElsheiekhHMS.Application/Queue/Validation/SendToDoctorRequestValidator.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueOutcome.cs](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueOutcome.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueRequest.cs](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueResult.cs](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueResult.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueService.cs](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/AppointmentArrivalQueueService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/AppointmentArrival/IAppointmentArrivalQueueService.cs](../ElsheiekhHMS.Application/Workflows/AppointmentArrival/IAppointmentArrivalQueueService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/PatientIntake/IPatientIntakeAppointmentService.cs](../ElsheiekhHMS.Application/Workflows/PatientIntake/IPatientIntakeAppointmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentRequest.cs](../ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentRequest.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentResult.cs](../ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentResult.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentService.cs](../ElsheiekhHMS.Application/Workflows/PatientIntake/PatientIntakeAppointmentService.cs) — ElsheiekhHMS.Application
- [ElsheiekhHMS.Core/Common/AuditableEntity.cs](../ElsheiekhHMS.Core/Common/AuditableEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Common/BaseEntity.cs](../ElsheiekhHMS.Core/Common/BaseEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs](../ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs](../ElsheiekhHMS.Core/Domain/Organization/Entities/Department.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs](../ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Patients/Enums/BloodGroup.cs](../ElsheiekhHMS.Core/Domain/Patients/Enums/BloodGroup.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Patients/Enums/Gender.cs](../ElsheiekhHMS.Core/Domain/Patients/Enums/Gender.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Entities/Appointment.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Entities/Appointment.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Entities/WalkInQueueEntry.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentStatus.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentStatus.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentType.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/AppointmentType.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueuePriority.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueuePriority.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueueStatus.cs](../ElsheiekhHMS.Core/Domain/Scheduling/Enums/QueueStatus.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs](../ElsheiekhHMS.Core/Domain/Staff/Entities/Doctor.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs](../ElsheiekhHMS.Core/Domain/Staff/Entities/DoctorSchedule.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Domain/Staff/Enums/DoctorStatus.cs](../ElsheiekhHMS.Core/Domain/Staff/Enums/DoctorStatus.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs](../ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/DomainException.cs](../ElsheiekhHMS.Core/Exceptions/DomainException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs](../ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs](../ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs) — ElsheiekhHMS.Core
- [ElsheiekhHMS.Infrastructure/Auditing/AuditEventWriter.cs](../ElsheiekhHMS.Infrastructure/Auditing/AuditEventWriter.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Auditing/Configurations/AuditLogConfiguration.cs](../ElsheiekhHMS.Infrastructure/Auditing/Configurations/AuditLogConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Auditing/Entities/AuditLog.cs](../ElsheiekhHMS.Infrastructure/Auditing/Entities/AuditLog.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Auditing/EntityAuditSaveChangesInterceptor.cs](../ElsheiekhHMS.Infrastructure/Auditing/EntityAuditSaveChangesInterceptor.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/AppointmentConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/AppointmentConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/DepartmentConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/DepartmentConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorScheduleConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/DoctorScheduleConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/PatientConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/PatientConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Configurations/Entities/WalkInQueueEntryConfiguration.cs](../ElsheiekhHMS.Infrastructure/Configurations/Entities/WalkInQueueEntryConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Health/HmsHealthChecks.cs](../ElsheiekhHMS.Infrastructure/Health/HmsHealthChecks.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/AccountLoginEligibility.cs](../ElsheiekhHMS.Infrastructure/Identity/AccountLoginEligibility.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/AdministratorBootstrapper.cs](../ElsheiekhHMS.Infrastructure/Identity/AdministratorBootstrapper.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/Configurations/ApplicationUserConfiguration.cs](../ElsheiekhHMS.Infrastructure/Identity/Configurations/ApplicationUserConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/Entities/AccountSecurityState.cs](../ElsheiekhHMS.Infrastructure/Identity/Entities/AccountSecurityState.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/Entities/ApplicationUser.cs](../ElsheiekhHMS.Infrastructure/Identity/Entities/ApplicationUser.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Identity/IdentityRoleSeeder.cs](../ElsheiekhHMS.Infrastructure/Identity/IdentityRoleSeeder.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs](../ElsheiekhHMS.Infrastructure/InfrastructureServiceExtensions.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921111137_InitialCreate.Designer.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921111137_InitialCreate.Designer.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921111137_InitialCreate.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921111137_InitialCreate.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921182651_AddPhase05IdentityAndAuditLog.Designer.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921182651_AddPhase05IdentityAndAuditLog.Designer.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921182651_AddPhase05IdentityAndAuditLog.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921182651_AddPhase05IdentityAndAuditLog.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921230714_AddPhase07AllocatorInfrastructure.Designer.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921230714_AddPhase07AllocatorInfrastructure.Designer.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260921230714_AddPhase07AllocatorInfrastructure.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260921230714_AddPhase07AllocatorInfrastructure.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.Designer.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.Designer.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260922013639_AddAppointmentCodeAllocator.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.Designer.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.Designer.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.cs](../ElsheiekhHMS.Infrastructure/Migrations/20260922115337_AddAppointmentQueueLink.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Migrations/ElsheiekhHmsDbContextModelSnapshot.cs](../ElsheiekhHMS.Infrastructure/Migrations/ElsheiekhHmsDbContextModelSnapshot.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Observability/RequestObservabilityMiddleware.cs](../ElsheiekhHMS.Infrastructure/Observability/RequestObservabilityMiddleware.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocation.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocation.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocationConfiguration.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocationConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocator.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/AppointmentCodeAllocator.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocation.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocation.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocationConfiguration.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocationConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocator.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/PatientCodeAllocator.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocation.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocation.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocationConfiguration.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocationConfiguration.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocator.cs](../ElsheiekhHMS.Infrastructure/Persistence/Allocation/QueueTicketAllocator.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Appointments/AppointmentPersistence.cs](../ElsheiekhHMS.Infrastructure/Persistence/Appointments/AppointmentPersistence.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Departments/DepartmentPersistence.cs](../ElsheiekhHMS.Infrastructure/Persistence/Departments/DepartmentPersistence.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs](../ElsheiekhHMS.Infrastructure/Persistence/ElsheiekhHmsDbContext.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Patients/PatientPersistence.cs](../ElsheiekhHMS.Infrastructure/Persistence/Patients/PatientPersistence.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Infrastructure/Persistence/Queue/QueuePersistence.cs](../ElsheiekhHMS.Infrastructure/Persistence/Queue/QueuePersistence.cs) — ElsheiekhHMS.Infrastructure
- [ElsheiekhHMS.Tests/Integration/Persistence/AppointmentArrivalQueueSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/AppointmentArrivalQueueSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/AppointmentCodeAllocatorSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/AppointmentCodeAllocatorSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/AppointmentServiceSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/AppointmentServiceSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/DepartmentServiceSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/DepartmentServiceSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/ElsheiekhHmsDbContextSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/ElsheiekhHmsDbContextSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/HmsHealthEndpointHostTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/HmsHealthEndpointHostTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/IdentityAndAuditLogSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/IdentityAndAuditLogSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/PatientServiceSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/PatientServiceSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/PerformanceSmokeSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/PerformanceSmokeSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/QueueServiceSqlServerTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/QueueServiceSqlServerTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/SqlServerReadinessHealthCheckTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/SqlServerReadinessHealthCheckTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseFixture.cs](../ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseFixture.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseGuard.cs](../ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseGuard.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseSafetyTests.cs](../ElsheiekhHMS.Tests/Integration/Persistence/SqlServerTestDatabaseSafetyTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentContractTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentContractTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentValidationTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Appointments/AppointmentValidationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Architecture/ApplicationContractArchitectureTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Architecture/ApplicationContractArchitectureTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Auditing/AuditVocabularyTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Auditing/AuditVocabularyTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Common/Contracts/PageRequestTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Common/Contracts/PageRequestTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Common/Results/ServiceResultTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Common/Results/ServiceResultTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Common/Validation/ValidationResultTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Common/Validation/ValidationResultTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentContractTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentContractTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentValidationTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Departments/DepartmentValidationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Patients/PatientContractTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Patients/PatientContractTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Patients/PatientServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Patients/PatientServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Patients/PatientValidationTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Patients/PatientValidationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Queue/QueueContractTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Queue/QueueContractTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Queue/QueueServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Queue/QueueServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Queue/QueueValidationTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Queue/QueueValidationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Security/SecurityVocabularyTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Security/SecurityVocabularyTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Workflows/AppointmentArrivalQueueServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Workflows/AppointmentArrivalQueueServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Application/Workflows/PatientIntakeAppointmentServiceTests.cs](../ElsheiekhHMS.Tests/Unit/Application/Workflows/PatientIntakeAppointmentServiceTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Organization/DepartmentTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Organization/DepartmentTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Scheduling/AppointmentTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Scheduling/AppointmentTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Scheduling/WalkInQueueEntryTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Scheduling/WalkInQueueEntryTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorScheduleTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorScheduleTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs](../ElsheiekhHMS.Tests/Unit/Domain/Staff/DoctorTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Allocation/AllocatorFormattingTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Allocation/AllocatorFormattingTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Allocation/AllocatorRegistrationTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Allocation/AllocatorRegistrationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditEventWriterTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditEventWriterTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditLogModelTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Auditing/AuditLogModelTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Configuration/InfrastructureConfigurationSafetyTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Configuration/InfrastructureConfigurationSafetyTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextModelTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextModelTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/ElsheiekhHmsDbContextTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Health/HealthCheckTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Health/HealthCheckTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/AccountLoginEligibilityTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/AccountLoginEligibilityTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/ApplicationUserTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/ApplicationUserTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityModelTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityModelTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityOptionsTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityOptionsTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityRoleSeederTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Identity/IdentityRoleSeederTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Infrastructure/Observability/RequestObservabilityMiddlewareTests.cs](../ElsheiekhHMS.Tests/Unit/Infrastructure/Observability/RequestObservabilityMiddlewareTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Web/Security/AuthenticationStateValidationTests.cs](../ElsheiekhHMS.Tests/Unit/Web/Security/AuthenticationStateValidationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Tests/Unit/Web/Security/AuthorizationConfigurationTests.cs](../ElsheiekhHMS.Tests/Unit/Web/Security/AuthorizationConfigurationTests.cs) — ElsheiekhHMS.Tests
- [ElsheiekhHMS.Web/Components/App.razor](../ElsheiekhHMS.Web/Components/App.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/AnonymousLayout.razor](../ElsheiekhHMS.Web/Components/Layout/AnonymousLayout.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/MainLayout.razor](../ElsheiekhHMS.Web/Components/Layout/MainLayout.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/NavMenu.razor](../ElsheiekhHMS.Web/Components/Layout/NavMenu.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Layout/ReconnectModal.razor](../ElsheiekhHMS.Web/Components/Layout/ReconnectModal.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/AccessDenied.razor](../ElsheiekhHMS.Web/Components/Pages/AccessDenied.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentCreate.razor](../ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentCreate.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentDetails.razor](../ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentDetails.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentRegistry.razor](../ElsheiekhHMS.Web/Components/Pages/Appointments/AppointmentRegistry.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentCreate.razor](../ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentCreate.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentDetails.razor](../ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentDetails.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentEdit.razor](../ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentEdit.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentRegistry.razor](../ElsheiekhHMS.Web/Components/Pages/Departments/DepartmentRegistry.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Error.razor](../ElsheiekhHMS.Web/Components/Pages/Error.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Home.razor](../ElsheiekhHMS.Web/Components/Pages/Home.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Login.razor](../ElsheiekhHMS.Web/Components/Pages/Login.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/NotFound.razor](../ElsheiekhHMS.Web/Components/Pages/NotFound.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Patients/PatientCreate.razor](../ElsheiekhHMS.Web/Components/Pages/Patients/PatientCreate.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Patients/PatientDetails.razor](../ElsheiekhHMS.Web/Components/Pages/Patients/PatientDetails.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Patients/PatientEdit.razor](../ElsheiekhHMS.Web/Components/Pages/Patients/PatientEdit.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Patients/PatientRegistry.razor](../ElsheiekhHMS.Web/Components/Pages/Patients/PatientRegistry.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Queue/AppointmentQueueHandoff.razor](../ElsheiekhHMS.Web/Components/Pages/Queue/AppointmentQueueHandoff.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Queue/QueueCreate.razor](../ElsheiekhHMS.Web/Components/Pages/Queue/QueueCreate.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Queue/QueueDetails.razor](../ElsheiekhHMS.Web/Components/Pages/Queue/QueueDetails.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Pages/Queue/QueueRegistry.razor](../ElsheiekhHMS.Web/Components/Pages/Queue/QueueRegistry.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Routes.razor](../ElsheiekhHMS.Web/Components/Routes.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/AppAlert.razor](../ElsheiekhHMS.Web/Components/Shared/AppAlert.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/ConfirmDialog.razor](../ElsheiekhHMS.Web/Components/Shared/ConfirmDialog.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/EmptyState.razor](../ElsheiekhHMS.Web/Components/Shared/EmptyState.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/ErrorState.razor](../ElsheiekhHMS.Web/Components/Shared/ErrorState.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/Field.razor](../ElsheiekhHMS.Web/Components/Shared/Field.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/LoadingState.razor](../ElsheiekhHMS.Web/Components/Shared/LoadingState.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/PageHeader.razor](../ElsheiekhHMS.Web/Components/Shared/PageHeader.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/Pagination.razor](../ElsheiekhHMS.Web/Components/Shared/Pagination.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/SearchBox.razor](../ElsheiekhHMS.Web/Components/Shared/SearchBox.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/StatusBadge.razor](../ElsheiekhHMS.Web/Components/Shared/StatusBadge.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/Shared/UserAccountMenu.razor](../ElsheiekhHMS.Web/Components/Shared/UserAccountMenu.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Components/_Imports.razor](../ElsheiekhHMS.Web/Components/_Imports.razor) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Program.cs](../ElsheiekhHMS.Web/Program.cs) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Security/AuthorizationConfiguration.cs](../ElsheiekhHMS.Web/Security/AuthorizationConfiguration.cs) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Security/CurrentUserAccessor.cs](../ElsheiekhHMS.Web/Security/CurrentUserAccessor.cs) — ElsheiekhHMS.Web
- [ElsheiekhHMS.Web/Security/HmsRevalidatingAuthenticationStateProvider.cs](../ElsheiekhHMS.Web/Security/HmsRevalidatingAuthenticationStateProvider.cs) — ElsheiekhHMS.Web

## Graph Freshness

`generatedAtUtc` and Git metadata describe generation time. Dirty status includes unrelated local changes. Git commit alone cannot describe uncommitted source.
`sourceManifest` hashes source, project, solution, Razor, build configuration and README inputs. Each sorted record is relative path + NUL + SHA-256; records are joined with LF and hashed again. All graphify-out directories and build output are excluded. `generatorFingerprint` also detects tooling changes.

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1 -Check
powershell -ExecutionPolicy Bypass -File .\tools\graphify.ps1
```

## Usage and Limits

- `graph.json`: machine-readable Codex architecture context. `edges` is canonical; `links` is an identical node-link compatibility alias.
- `graph.html`: offline, self-contained interactive explorer with project, Core, test and full-structure views.
- `GRAPH_REPORT.md`: human navigation index generated from the same JSON data.
- Read README and this index first. Graphify is an architectural index, NOT authoritative source code. Read the relevant source before reasoning about implementation or editing it. Regenerate if fingerprints differ.
- Refresh these outputs with tools/graphify.ps1. The generic Graphify CLI updater uses another schema and may replace this custom metadata. Existing CLI caches and project-local snapshots have not been refreshed or removed.
- Single combined C# analysis compilation; not a replacement for project-by-project compilation.
- Only resolved bases and test targets become edges; unresolved relationships are omitted with warnings.
- testCount is static Fact + InlineData case count, not a test execution result; dynamic theories reported separately.
- Package list is evaluated direct PackageReference only; transitive and SDK auto-references are not claimed as installed direct packages.
- Architecture checks are structural observations, not a full semantic security or dependency audit.
- WARNING: Unresolved base omitted: ElsheiekhHMS.Infrastructure.Health.HmsLivenessHealthCheck : IHealthCheck
- WARNING: Unresolved base omitted: ElsheiekhHMS.Infrastructure.Health.SqlServerReadinessHealthCheck : IHealthCheck
- WARNING: Unresolved base omitted: ElsheiekhHMS.Infrastructure.Identity.Entities.ApplicationUser : IdentityUser
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Integration.Persistence.SqlServerPersistenceCollection : ICollectionFixture<ElsheiekhHMS.Tests.Integration.Persistence.SqlServerTestDatabaseFixture>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Integration.Persistence.SqlServerTestDatabaseFixture : IAsyncLifetime
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Unit.Infrastructure.Identity.IdentityRoleSeederTests.NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Unit.Infrastructure.Identity.IdentityRoleSeederTests.InMemoryRoleStore : IRoleStore<IdentityRole>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Unit.Infrastructure.Observability.RequestObservabilityMiddlewareTests.CapturingLogger : ILogger<ElsheiekhHMS.Infrastructure.Observability.RequestObservabilityMiddleware>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Unit.Web.Security.AuthenticationStateValidationTests.StubUserManager : UserManager<ElsheiekhHMS.Infrastructure.Identity.Entities.ApplicationUser>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Tests.Unit.Web.Security.AuthenticationStateValidationTests.EmptyUserStore : IUserStore<ElsheiekhHMS.Infrastructure.Identity.Entities.ApplicationUser>
- WARNING: Unresolved base omitted: ElsheiekhHMS.Web.Security.HmsRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
