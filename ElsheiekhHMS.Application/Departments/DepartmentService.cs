using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Persistence;
using ElsheiekhHMS.Application.Departments.Validation;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Application.Departments;

public sealed class DepartmentService(
    IDepartmentPersistence persistence,
    IAuditEventWriter auditEventWriter,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IDepartmentService
{
    public async Task<ServiceResult<DepartmentDetailsDto>> GetByIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<DepartmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (departmentId <= 0)
        {
            return Failure<DepartmentDetailsDto>(
                "department.id.positive",
                "Department ID must be greater than zero.",
                nameof(departmentId));
        }

        try
        {
            var department = await persistence.GetDetailsAsync(departmentId, cancellationToken);
            return department is null
                ? Failure<DepartmentDetailsDto>("department.not_found", "Department was not found.")
                : ServiceResult<DepartmentDetailsDto>.Success(department);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<DepartmentDetailsDto>(); }
    }

    public async Task<ServiceResult<PagedResult<DepartmentSummaryDto>>> SearchAsync(
        DepartmentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<PagedResult<DepartmentSummaryDto>>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new DepartmentSearchRequestValidator().Validate(request);
        if (!validation.IsValid)
        {
            return ValidationFailure<PagedResult<DepartmentSummaryDto>>(validation);
        }

        try
        {
            var page = await persistence.SearchAsync(request, cancellationToken);
            return ServiceResult<PagedResult<DepartmentSummaryDto>>.Success(page);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { return PersistenceFailure<PagedResult<DepartmentSummaryDto>>(); }
    }

    public async Task<ServiceResult<DepartmentDetailsDto>> CreateAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<DepartmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new CreateDepartmentRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<DepartmentDetailsDto>(validation);

        try
        {
            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            var department = new Department(
                request.Name,
                request.Description,
                request.PhoneExtension,
                utcNow,
                currentUser.UserId);
            persistence.Add(department);
            await RecordAuditAsync(AuditActions.DepartmentCreated, department.Name, cancellationToken);
            await persistence.SaveChangesAsync(cancellationToken);
            return ServiceResult<DepartmentDetailsDto>.Success(ToDetails(department));
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<DepartmentDetailsDto>("department.domain_validation", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<DepartmentDetailsDto>(); }
    }

    public async Task<ServiceResult<DepartmentDetailsDto>> UpdateAsync(
        int departmentId,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<DepartmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        var validation = new UpdateDepartmentRequestValidator().Validate(request);
        if (!validation.IsValid) return ValidationFailure<DepartmentDetailsDto>(validation);
        if (departmentId <= 0)
        {
            return Failure<DepartmentDetailsDto>(
                "department.id.positive",
                "Department ID must be greater than zero.",
                nameof(departmentId));
        }

        try
        {
            var department = await persistence.LoadTrackedAsync(departmentId, cancellationToken);
            if (department is null)
            {
                return Failure<DepartmentDetailsDto>("department.not_found", "Department was not found.");
            }

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            department.UpdateDetails(
                request.Name,
                request.Description,
                request.PhoneExtension,
                utcNow,
                currentUser.UserId);
            await RecordAuditAsync(
                AuditActions.DepartmentUpdated,
                department.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                cancellationToken);
            await persistence.SaveChangesAsync(cancellationToken);
            return ServiceResult<DepartmentDetailsDto>.Success(ToDetails(department));
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<DepartmentDetailsDto>("department.domain_rule", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<DepartmentDetailsDto>(); }
    }

    public async Task<ServiceResult<DepartmentDetailsDto>> DeactivateAsync(
        int departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!HasAccess()) return Forbidden<DepartmentDetailsDto>();
        cancellationToken.ThrowIfCancellationRequested();
        if (departmentId <= 0)
        {
            return Failure<DepartmentDetailsDto>(
                "department.id.positive",
                "Department ID must be greater than zero.",
                nameof(departmentId));
        }

        try
        {
            var department = await persistence.LoadTrackedAsync(departmentId, cancellationToken);
            if (department is null)
            {
                return Failure<DepartmentDetailsDto>("department.not_found", "Department was not found.");
            }

            var utcNow = timeProvider.GetUtcNow().ToUniversalTime();
            department.Deactivate(utcNow, currentUser.UserId);
            await RecordAuditAsync(
                AuditActions.DepartmentDeactivated,
                department.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                cancellationToken);
            await persistence.SaveChangesAsync(cancellationToken);
            return ServiceResult<DepartmentDetailsDto>.Success(ToDetails(department));
        }
        catch (OperationCanceledException) { throw; }
        catch (DomainException exception)
        {
            return Failure<DepartmentDetailsDto>("department.domain_rule", exception.Message);
        }
        catch (Exception) { return PersistenceFailure<DepartmentDetailsDto>(); }
    }

    private Task RecordAuditAsync(string action, string targetId, CancellationToken cancellationToken) =>
        auditEventWriter.RecordAsync(
            new AuditEventRequest(
                AuditCategories.Business,
                action,
                "Department",
                targetId),
            cancellationToken);

    private bool HasAccess() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.SystemAdministrator, StringComparison.Ordinal));

    private static DepartmentDetailsDto ToDetails(Department department) => new(
        department.Id,
        department.Name,
        department.Description,
        department.PhoneExtension,
        department.IsActive);

    private static ServiceResult<T> Forbidden<T>() => Failure<T>(
        "department.forbidden",
        "The current user is not authorized to manage departments.");

    private static ServiceResult<T> PersistenceFailure<T>() => Failure<T>(
        "department.persistence_failure",
        "The department operation could not be completed.");

    private static ServiceResult<T> ValidationFailure<T>(ValidationResult validation) =>
        ServiceResult<T>.Failure(validation.Errors
            .Select(error => new ServiceError(error.Code, error.Message, error.Field))
            .ToArray());

    private static ServiceResult<T> Failure<T>(string code, string message, string? field = null) =>
        ServiceResult<T>.Failure(new ServiceError(code, message, field));
}
