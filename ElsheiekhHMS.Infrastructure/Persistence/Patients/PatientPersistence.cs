using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Persistence;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Infrastructure.Persistence.Patients;

public sealed class PatientPersistence(
    ElsheiekhHmsDbContext context,
    PatientCodeAllocator codeAllocator) : IPatientPersistence
{
    public Task<string> AllocatePatientCodeAsync(int year, CancellationToken cancellationToken) =>
        codeAllocator.AllocateAsync(year, cancellationToken);

    public async Task<PatientDetailsDto?> GetDetailsAsync(int patientId, CancellationToken cancellationToken)
    {
        var projection = await context.Patients
            .AsNoTracking()
            .Where(patient => patient.Id == patientId)
            .Select(patient => new PatientDetailsProjection(
                patient.Id,
                patient.PatientCode,
                patient.FirstName,
                patient.MiddleName,
                patient.ThirdName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Gender,
                patient.Phone,
                patient.BloodGroup,
                patient.NationalId,
                patient.PassportNumber,
                patient.Address,
                patient.City,
                patient.EmergencyContactName,
                patient.EmergencyContactPhone,
                patient.EmergencyContactRelationship,
                patient.InsuranceProvider,
                patient.RowVersion))
            .SingleOrDefaultAsync(cancellationToken);

        return projection?.ToDto();
    }

    public async Task<PagedResult<PatientSummaryDto>> SearchAsync(
        PatientSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = context.Patients.AsNoTracking();

        if (request.SearchText is not null)
        {
            var text = request.SearchText;
            query = query.Where(patient =>
                patient.PatientCode.Contains(text) ||
                patient.FirstName.Contains(text) ||
                (patient.MiddleName != null && patient.MiddleName.Contains(text)) ||
                (patient.ThirdName != null && patient.ThirdName.Contains(text)) ||
                patient.LastName.Contains(text));
        }
        if (request.PatientCode is not null) query = query.Where(patient => patient.PatientCode == request.PatientCode);
        if (request.Phone is not null) query = query.Where(patient => patient.Phone == request.Phone);
        if (request.NationalId is not null) query = query.Where(patient => patient.NationalId == request.NationalId);
        if (request.PassportNumber is not null) query = query.Where(patient => patient.PassportNumber == request.PassportNumber);
        if (request.DateOfBirth.HasValue) query = query.Where(patient => patient.DateOfBirth == request.DateOfBirth.Value);

        query = ApplyOrdering(query, request);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page.PageNumber - 1) * request.Page.PageSize)
            .Take(request.Page.PageSize)
            .Select(patient => new PatientSummaryDto(
                patient.Id,
                patient.PatientCode,
                (patient.FirstName + " " +
                 (patient.MiddleName == null ? "" : patient.MiddleName + " ") +
                 (patient.ThirdName == null ? "" : patient.ThirdName + " ") +
                 patient.LastName).Trim(),
                patient.DateOfBirth,
                patient.Gender,
                patient.Phone))
            .ToListAsync(cancellationToken);

        return new PagedResult<PatientSummaryDto>(items, totalCount, request.Page.PageNumber, request.Page.PageSize);
    }

    public Task<Patient?> LoadTrackedAsync(int patientId, CancellationToken cancellationToken) =>
        context.Patients.SingleOrDefaultAsync(patient => patient.Id == patientId, cancellationToken);

    public Task<bool> ExistsByNationalIdAsync(string value, int? excludingPatientId, CancellationToken cancellationToken) =>
        context.Patients.AnyAsync(
            patient => patient.NationalId == value &&
                       (!excludingPatientId.HasValue || patient.Id != excludingPatientId.Value),
            cancellationToken);

    public Task<bool> ExistsByPassportNumberAsync(string value, int? excludingPatientId, CancellationToken cancellationToken) =>
        context.Patients.AnyAsync(
            patient => patient.PassportNumber == value &&
                       (!excludingPatientId.HasValue || patient.Id != excludingPatientId.Value),
            cancellationToken);

    public void Add(Patient patient) => context.Patients.Add(patient);

    public async Task<PatientPersistenceSaveStatus> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return PatientPersistenceSaveStatus.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return PatientPersistenceSaveStatus.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (ContainsIndex(exception, "IX_Patients_NationalId"))
        {
            return PatientPersistenceSaveStatus.NationalIdConflict;
        }
        catch (DbUpdateException exception) when (ContainsIndex(exception, "IX_Patients_PassportNumber"))
        {
            return PatientPersistenceSaveStatus.PassportNumberConflict;
        }
        catch (DbUpdateException)
        {
            throw;
        }
    }

    private static bool ContainsIndex(DbUpdateException exception, string indexName) =>
        exception.ToString().Contains(indexName, StringComparison.OrdinalIgnoreCase);

    private static IQueryable<Patient> ApplyOrdering(IQueryable<Patient> query, PatientSearchRequest request)
    {
        var ordered = request.SortBy switch
        {
            PatientSortField.Name => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(patient => patient.LastName).ThenBy(patient => patient.FirstName)
                : query.OrderByDescending(patient => patient.LastName).ThenByDescending(patient => patient.FirstName),
            PatientSortField.DateOfBirth => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(patient => patient.DateOfBirth)
                : query.OrderByDescending(patient => patient.DateOfBirth),
            PatientSortField.Phone => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(patient => patient.Phone)
                : query.OrderByDescending(patient => patient.Phone),
            _ => request.SortDirection == SortDirection.Ascending
                ? query.OrderBy(patient => patient.PatientCode)
                : query.OrderByDescending(patient => patient.PatientCode)
        };

        return ordered.ThenBy(patient => patient.PatientCode);
    }

    private sealed record PatientDetailsProjection(
        int Id,
        string PatientCode,
        string FirstName,
        string? MiddleName,
        string? ThirdName,
        string LastName,
        DateOnly DateOfBirth,
        Core.Domain.Patients.Enums.Gender Gender,
        string Phone,
        Core.Domain.Patients.Enums.BloodGroup? BloodGroup,
        string? NationalId,
        string? PassportNumber,
        string Address,
        string? City,
        string? EmergencyContactName,
        string? EmergencyContactPhone,
        string? EmergencyContactRelationship,
        string? InsuranceProvider,
        byte[] RowVersion)
    {
        public PatientDetailsDto ToDto() => new(
            Id,
            PatientCode,
            string.Join(" ", new[] { FirstName, MiddleName, ThirdName, LastName }.Where(part => !string.IsNullOrEmpty(part))),
            DateOfBirth,
            Gender,
            Phone,
            MiddleName,
            ThirdName,
            BloodGroup,
            NationalId,
            PassportNumber,
            Address,
            City,
            EmergencyContactName,
            EmergencyContactPhone,
            EmergencyContactRelationship,
            InsuranceProvider,
            RowVersion.Length == 0 ? null : Convert.ToBase64String(RowVersion));
    }
}
