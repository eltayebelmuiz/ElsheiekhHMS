# Phase 03C Patient Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a durable Patient domain model and focused tests without visit state, vitals, persistence, or application workflows.

**Architecture:** `Patient` is a `SoftDeletableEntity` master aggregate that opts into the existing opaque concurrency-token interface. It validates its own data against caller-supplied dates, preserves Unicode names, allows shared phones, and controls updates/deletion. Future Application and Phase 04 persistence implement code generation, uniqueness, auditing, authorization, and conflict handling.

**Tech Stack:** .NET 10, C#, xUnit, PowerShell, Graphify.

**Spec:** `docs/superpowers/specs/2026-09-21-phase03c-patient-identity-vitals-design.md`

## Global Constraints

- Execute this plan **only after separate implementation approval**. This document itself changes no product code.
- Create only Patient, Gender, BloodGroup, and PatientTests. Do not modify Department, Doctor, DoctorSchedule, `IHasConcurrencyToken`, project files, or existing tests.
- No visit/current-status property, vitals property, clinical observation type, approximate-DOB system, stored Age, phone uniqueness, or contact hierarchy.
- No EF Core, SQL Server, Identity, repositories, DTOs, application services, migrations, UI, or new dependencies in this batch.
- Intrinsic Core validation uses supplied `DateOnly asOfDate`; Core does not read a clock, query the database, generate patient codes, or determine global uniqueness.
- Do not stage, commit, or push without a separate request. After source changes and tests pass, run `graphify update .`; inspect generated diff.

## Review Focus

- Arabic and other Unicode names are accepted; whitespace-only first/last names are rejected (Task 1 tests).
- A future or default DOB is rejected against supplied date, including on update, without changing previous values (Tasks 1 and 2).
- Two distinct patients may share a phone; no local uniqueness invariant or SQL index is introduced (Task 1 test and final diff review).
- Optional blank passport/national ID is stored as null; full name never contains double spaces when middle parts are absent (Task 1 tests).
- Invalid update or repeated deletion cannot partially change the Patient or audit metadata (Tasks 2 and 3).

## Shared API for all tasks

Namespaces: `ElsheiekhHMS.Core.Domain.Patients.Entities` and `ElsheiekhHMS.Core.Domain.Patients.Enums`. Tests: `ElsheiekhHMS.Tests.Unit.Domain.Patients`. All required strings are trimmed; all optional strings are trimmed and whitespace-only becomes null. The constructor receives trusted code but only checks *shape*; no user-supplied authoritative code may be passed through the later Application boundary.

```csharp
public Patient(
    string patientCode, string firstName, string? middleName, string? thirdName,
    string lastName, DateOnly dateOfBirth, Gender gender, BloodGroup? bloodGroup,
    string? nationalId, string? passportNumber, string phone, string address,
    string? city, string? emergencyContactName, string? emergencyContactPhone,
    string? emergencyContactRelationship, string? insuranceProvider,
    DateOnly asOfDate, DateTimeOffset createdAt, string? createdBy);

public void UpdateDemographics(
    string firstName, string? middleName, string? thirdName, string lastName,
    DateOnly dateOfBirth, Gender gender, BloodGroup? bloodGroup,
    DateOnly asOfDate, DateTimeOffset updatedAt, string? updatedBy);
public void UpdateContactDetails(
    string phone, string address, string? city,
    string? emergencyContactName, string? emergencyContactPhone,
    string? emergencyContactRelationship, string? insuranceProvider,
    DateTimeOffset updatedAt, string? updatedBy);
public void UpdateIdentifiers(
    string? nationalId, string? passportNumber,
    DateTimeOffset updatedAt, string? updatedBy);
public void MarkDeleted(DateTimeOffset deletedAt, string? deletedBy);
```

The 20-argument constructor is long because this design deliberately keeps simple scalar contact information on Patient and avoids a speculative second public aggregate/registration type. Use named arguments in callers/tests and review it during implementation; do not silently add public value objects to reduce argument count. All domain methods validate candidates before assigning any state. `RowVersion` remains the existing interface's publicly settable opaque byte array.

---

### Task 1: Patient construction, names, demographics, and enums

**Files:**
- Create test first: `ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs`
- After RED create: `ElsheiekhHMS.Core/Domain/Patients/Enums/Gender.cs`
- After RED create: `ElsheiekhHMS.Core/Domain/Patients/Enums/BloodGroup.cs`
- After RED create: `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs`

**Interfaces:** Consumes `SoftDeletableEntity`, `IHasConcurrencyToken`, `DomainValidationException`. Produces constructor, scalar getters, computed names, `Gender` and `BloodGroup`. Later tasks add mutation and deletion to this same Patient file.

- [ ] **Step 1: Write focused failing tests.** Start with the concrete helper and assertions below; add theories for null/empty/whitespace first/last/code/phone/address, invalid code shapes and undefined enum values, DOB `MinValue` and future DOB, trimmed/blank optional strings, shared phone between two patients, and all eight blood groups. Use `DateOnly(2026, 9, 21)` as the supplied reference date and `DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero)` for audit assertions.

```csharp
private static Patient CreatePatient(string firstName = "أحمد", string phone = "0912345678") =>
    new(patientCode: "PT-2026-00001", firstName: firstName,
        middleName: null, thirdName: null, lastName: "محمد",
        dateOfBirth: new DateOnly(1990, 1, 1), gender: Gender.Male,
        bloodGroup: null, nationalId: "  N-42  ", passportNumber: " ",
        phone: phone, address: " Khartoum ", city: null,
        emergencyContactName: null, emergencyContactPhone: null,
        emergencyContactRelationship: null, insuranceProvider: null,
        asOfDate: new DateOnly(2026, 9, 21),
        createdAt: new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero),
        createdBy: "registrar-1");

[Fact]
public void Construction_accepts_Unicode_name_and_normalizes_optional_identifiers()
{
    var patient = CreatePatient();
    Assert.Equal("أحمد محمد", patient.FullName);
    Assert.Equal("أحمد محمد", patient.ShortName);
    Assert.Equal("N-42", patient.NationalId);
    Assert.Null(patient.PassportNumber);
    Assert.False(patient.IsDeleted);
}

[Fact]
public void Shared_phone_does_not_invalidate_either_patient()
{
    Assert.Equal(CreatePatient().Phone, CreatePatient("فاطمة").Phone);
}

[Fact]
public void Future_birth_date_is_rejected()
{
    Assert.Throws<DomainValidationException>(() => new Patient(
        "PT-2026-00001", "أحمد", null, null, "محمد",
        new DateOnly(2026, 9, 22), Gender.Male, null, null, null,
        "0912345678", "Khartoum", null, null, null, null, null,
        new DateOnly(2026, 9, 21),
        new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero), "registrar-1"));
}
```

- [ ] **Step 2: Verify RED.** Run `rtk proxy dotnet test ElsheiekhHMS.Tests/ElsheiekhHMS.Tests.csproj --no-restore --filter FullyQualifiedName~PatientTests`. Expect compilation failure because Patient/enums do not exist; do not change tests to hide a failure.
- [ ] **Step 3: Add minimal enums and Patient constructor.** Use exact values and properties from the spec; no stored Age, Status or vital field. The key validations follow this shape; validate all locals first, assign after success.

```csharp
public enum Gender { Male = 0, Female = 1, Other = 2 }
public enum BloodGroup
{
    APositive = 0, ANegative = 1, BPositive = 2, BNegative = 3,
    ABPositive = 4, ABNegative = 5, OPositive = 6, ONegative = 7
}

private static string Required(string? value, string label) =>
    string.IsNullOrWhiteSpace(value)
        ? throw new DomainValidationException($"{label} is required.")
        : value.Trim();
private static string? Optional(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();
private static void ValidateBirthDate(DateOnly birth, DateOnly asOfDate)
{
    if (birth == DateOnly.MinValue || birth > asOfDate)
        throw new DomainValidationException("Date of birth must not be in the future.");
}

// For a normalized PatientCode: exact 13-character PT-YYYY-NNNNN shape.
private static bool IsPatientCode(string code) =>
    code.Length == 13 && code.StartsWith("PT-", StringComparison.Ordinal) &&
    code[7] == '-' && code[3..7].All(c => c is >= '0' and <= '9') &&
    code[8..].All(c => c is >= '0' and <= '9');

public string FullName => string.Join(" ",
    new[] { FirstName, MiddleName, ThirdName, LastName }
        .Where(part => !string.IsNullOrEmpty(part)));
public string ShortName => $"{FirstName} {LastName}";
```

Validate `Enum.IsDefined(gender)` and nullable blood group when present; assign every listed demographic/contact property, `CreatedAt`, and `CreatedBy`. Inherit deletion defaults. The concurrency property is added in Task 3. Do not add `[NotMapped]`, EF attributes or database indexes to Core.
- [ ] **Step 4: Verify GREEN.** Rerun the focused command; require all new cases pass. Review constructor argument order against the shared API.

### Task 2: Atomic demographic, contact and identifier updates

**Files:**
- Modify test first: `ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs`
- After RED modify: `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs`

**Interfaces:** Consumes Patient constructor and properties from Task 1; produces `UpdateDemographics`, `UpdateContactDetails`, `UpdateIdentifiers` with signatures in Shared API.

- [ ] **Step 1: Write failing tests.** Assert successful trimmed updates and `UpdatedAt/UpdatedBy`; optional fields normalize to null; name/code remain independent; a future DOB, empty required contact field or invalid enum leaves all prior data and audit fields unchanged. Confirm an update can retain the same phone shared by another Patient without any uniqueness lookup.

```csharp
[Fact]
public void Invalid_demographic_update_keeps_previous_values_and_audit()
{
    var patient = CreatePatient();
    var before = patient.FullName;
    Assert.Throws<DomainValidationException>(() => patient.UpdateDemographics(
        "Changed", null, null, "Name", new DateOnly(2026, 9, 22),
        Gender.Other, null, new DateOnly(2026, 9, 21),
        new DateTimeOffset(2026, 9, 21, 9, 0, 0, TimeSpan.Zero), "registrar-2"));
    Assert.Equal(before, patient.FullName);
    Assert.Null(patient.UpdatedAt);
    Assert.Null(patient.UpdatedBy);
}
```

- [ ] **Step 2: Verify RED.** Run the same filtered `dotnet test` command and require compilation failure on the missing methods.
- [ ] **Step 3: Implement minimal methods.** For each method call `EnsureNotDeleted()` first, validate/normalize every incoming value into locals, then assign properties and audit fields. Keep `PatientCode` unchanged. Example pattern:

```csharp
public void UpdateIdentifiers(string? nationalId, string? passportNumber,
    DateTimeOffset updatedAt, string? updatedBy)
{
    EnsureNotDeleted();
    var nextNationalId = Optional(nationalId);
    var nextPassportNumber = Optional(passportNumber);
    NationalId = nextNationalId;
    PassportNumber = nextPassportNumber;
    UpdatedAt = updatedAt;
    UpdatedBy = updatedBy;
}
```

Use `Required` for name, phone/address; `Optional` for middle/third, city, emergency-contact fields and insurance; `ValidateBirthDate` and `Enum.IsDefined` for demographics. Never compare one Patient's values with another's in Core.
- [ ] **Step 4: Verify GREEN.** Run focused tests and review invalid-update state assertions for every update method.

### Task 3: Soft-delete lifecycle and opt-in concurrency contract

**Files:**
- Modify test first: `ElsheiekhHMS.Tests/Unit/Domain/Patients/PatientTests.cs`
- After RED modify: `ElsheiekhHMS.Core/Domain/Patients/Entities/Patient.cs`

**Interfaces:** Consumes `SoftDeletableEntity`, `IHasConcurrencyToken`, Patient methods from Tasks 1–2; produces `MarkDeleted` and mutation guard.

- [ ] **Step 1: Write failing tests.** Confirm `Patient is IHasConcurrencyToken` and has the interface `RowVersion`, deletion metadata and update metadata are set together, second delete fails without changing values, and each of the three update methods fails after deletion. Test code must not treat Core's byte array as working SQL concurrency.

```csharp
[Fact]
public void Deletion_sets_metadata_and_repeated_deletion_is_rejected()
{
    var patient = CreatePatient();
    Assert.IsAssignableFrom<IHasConcurrencyToken>(patient);
    var at = new DateTimeOffset(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
    patient.MarkDeleted(at, "registrar-2");
    Assert.True(patient.IsDeleted);
    Assert.Equal(at, patient.DeletedAt);
    Assert.Equal("registrar-2", patient.DeletedBy);
    Assert.Equal(at, patient.UpdatedAt);
    Assert.Equal("registrar-2", patient.UpdatedBy);
    Assert.Throws<BusinessRuleException>(() => patient.MarkDeleted(at, "registrar-3"));
    Assert.Equal("registrar-2", patient.DeletedBy);
}
```

- [ ] **Step 2: Verify RED.** Run filtered tests; expect missing method/interface failure.
- [ ] **Step 3: Implement minimal lifecycle.** Add interface to Patient declaration and `public byte[] RowVersion { get; set; } = [];` to satisfy the existing contract without changing it. Add guard and deletion method:

```csharp
private void EnsureNotDeleted()
{
    if (IsDeleted)
        throw new BusinessRuleException("Deleted patients cannot be changed.");
}

public void MarkDeleted(DateTimeOffset deletedAt, string? deletedBy)
{
    EnsureNotDeleted();
    IsDeleted = true;
    DeletedAt = deletedAt;
    DeletedBy = deletedBy;
    UpdatedAt = deletedAt;
    UpdatedBy = deletedBy;
}
```

- [ ] **Step 4: Verify GREEN.** Run focused tests. Verify all update methods guard before state changes. The Phase 04 mapping/conflict test is not part of this plan.

### Task 4: Full verification and boundary review

**Files:** Review only the four files listed above; no new files unless a test exposes a defect within this scope.

**Interfaces:** No new API. Confirms the delivered Patient foundation matches the spec.

- [ ] **Step 1: Run commands sequentially.** From repo root: `rtk proxy dotnet restore ElsheiekhHMS.slnx`, then `rtk proxy dotnet build ElsheiekhHMS.slnx --no-restore`, then `rtk proxy dotnet test ElsheiekhHMS.slnx --no-build --no-restore`. Require success; do not suppress warnings. Failures are investigated before continuing.
- [ ] **Step 2: Review boundaries and diff.** Run `rtk proxy rg -n 'PatientStatus|CheckedIn|CurrentBed|Height|Weight|Temperature|BloodPressure|Pulse|VitalObservation|ClinicalObservation|Microsoft.EntityFrameworkCore|System.Data' ElsheiekhHMS.Core/Domain/Patients` and require **no matches**; inspect actual source to ensure age is computed only if needed and no phone global uniqueness. Run `rtk proxy git diff --check`, `rtk proxy git diff`, and `rtk proxy git status --short --untracked-files=all`; include untracked new source in manual review because ordinary `git diff` omits it.
- [ ] **Step 3: Update graph after source changes.** Run `rtk proxy graphify update .`, verify the generated graph still shows Core with no outer-layer references, and inspect Git status for expected graph artifacts. Do not discard pre-existing user changes.
- [ ] **Step 4: Report.** State exact files changed, test counts, build result, any warnings, graph result, and Git status. Do not claim Phase 03C implemented if any required verification fails; do not start 03D or clinical observations.
