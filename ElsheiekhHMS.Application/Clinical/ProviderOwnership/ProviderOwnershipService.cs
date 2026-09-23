using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Security;

namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership;

public sealed class ProviderOwnershipService(
    IProviderOwnershipPersistence persistence,
    IAuditEventWriter auditEventWriter,
    ICurrentUser currentUser) : IProviderOwnershipService
{
    public async Task<ServiceResult<ProviderOwnershipDto>> AssignAsync(
        AssignProviderOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageOwnership())
        {
            return Forbidden<ProviderOwnershipDto>();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.DoctorId <= 0)
        {
            return Failure<ProviderOwnershipDto>(
                "provider_ownership.doctor_id_invalid",
                "Doctor ID must be greater than zero.",
                nameof(request.DoctorId));
        }

        var userId = request.UserId?.Trim();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure<ProviderOwnershipDto>(
                "provider_ownership.user_id_required",
                "A stable Identity UserId is required.",
                nameof(request.UserId));
        }

        try
        {
            var doctor = await persistence.GetDoctorStatusAsync(request.DoctorId, cancellationToken);
            if (!doctor.Exists)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.doctor_not_found",
                    "The Doctor was not found.");
            }

            if (!doctor.IsActive)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.doctor_inactive",
                    "Only an active Doctor can receive provider ownership.");
            }

            var account = await persistence.GetAccountStatusAsync(userId, cancellationToken);
            if (!account.Exists)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.user_not_found",
                    "The Identity user was not found.");
            }

            if (!account.HasProviderRole)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.provider_role_required",
                    "The Identity user must hold the Provider role.");
            }

            if (!account.IsActive)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.account_inactive",
                    "Suspended, banned, or login-disabled accounts cannot receive provider ownership.");
            }

            var byDoctor = await persistence.GetByDoctorIdAsync(request.DoctorId, cancellationToken);
            if (byDoctor is not null)
            {
                return byDoctor.UserId == userId
                    ? ServiceResult<ProviderOwnershipDto>.Success(byDoctor)
                    : Conflict<ProviderOwnershipDto>();
            }

            var byUser = await persistence.GetByUserIdAsync(userId, cancellationToken);
            if (byUser is not null)
            {
                return Conflict<ProviderOwnershipDto>();
            }

            persistence.Add(request.DoctorId, userId);
            await auditEventWriter.RecordAsync(
                new AuditEventRequest(
                    AuditCategories.Authorization,
                    AuditActions.ProviderOwnershipAssigned,
                    "DoctorApplicationUserLink",
                    request.DoctorId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Metadata: new Dictionary<string, string?> { ["UserId"] = userId }),
                cancellationToken);
            await persistence.SaveChangesAsync(cancellationToken);

            return ServiceResult<ProviderOwnershipDto>.Success(
                new ProviderOwnershipDto(request.DoctorId, userId));
        }
        catch (OperationCanceledException) { throw; }
        catch (ProviderOwnershipConflictException)
        {
            return Conflict<ProviderOwnershipDto>();
        }
        catch (Exception)
        {
            return Failure<ProviderOwnershipDto>(
                "provider_ownership.persistence_failure",
                "The provider ownership operation could not be completed.");
        }
    }

    public async Task<ServiceResult<ProviderOwnershipDto>> UnassignAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        if (!CanManageOwnership())
        {
            return Forbidden<ProviderOwnershipDto>();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (doctorId <= 0)
        {
            return Failure<ProviderOwnershipDto>(
                "provider_ownership.doctor_id_invalid",
                "Doctor ID must be greater than zero.",
                nameof(doctorId));
        }

        try
        {
            var ownership = await persistence.GetByDoctorIdAsync(doctorId, cancellationToken);
            if (ownership is null)
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.not_found",
                    "Provider ownership was not found.");
            }

            if (!await persistence.RemoveByDoctorIdAsync(doctorId, cancellationToken))
            {
                return Failure<ProviderOwnershipDto>(
                    "provider_ownership.not_found",
                    "Provider ownership was not found.");
            }

            await auditEventWriter.RecordAsync(
                new AuditEventRequest(
                    AuditCategories.Authorization,
                    AuditActions.ProviderOwnershipUnassigned,
                    "DoctorApplicationUserLink",
                    doctorId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Metadata: new Dictionary<string, string?> { ["UserId"] = ownership.UserId }),
                cancellationToken);
            await persistence.SaveChangesAsync(cancellationToken);

            return ServiceResult<ProviderOwnershipDto>.Success(ownership);
        }
        catch (OperationCanceledException) { throw; }
        catch (ProviderOwnershipConflictException)
        {
            return Conflict<ProviderOwnershipDto>();
        }
        catch (Exception)
        {
            return Failure<ProviderOwnershipDto>(
                "provider_ownership.persistence_failure",
                "The provider ownership operation could not be completed.");
        }
    }

    public async Task<ServiceResult<int>> ResolveCurrentDoctorIdAsync(
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return Failure<int>(
                "provider_ownership.unauthenticated",
                "An authenticated Provider account is required.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var account = await persistence.GetAccountStatusAsync(
                currentUser.UserId,
                cancellationToken);
            if (!account.Exists || !account.HasProviderRole)
            {
                return Failure<int>(
                    "provider_ownership.provider_role_required",
                    "The current account is not an approved Provider.");
            }

            if (!account.IsActive)
            {
                return Failure<int>(
                    "provider_ownership.account_inactive",
                    "Suspended, banned, or login-disabled Provider accounts are not clinically eligible.");
            }

            var ownership = await persistence.GetByUserIdAsync(
                currentUser.UserId,
                cancellationToken);
            if (ownership is null)
            {
                return Failure<int>(
                    "provider_ownership.mapping_missing",
                    "The current Provider account is not mapped to a Doctor.");
            }

            var doctor = await persistence.GetDoctorStatusAsync(
                ownership.DoctorId,
                cancellationToken);
            if (!doctor.Exists || !doctor.IsActive)
            {
                return Failure<int>(
                    "provider_ownership.doctor_inactive",
                    "The mapped Doctor is not active.");
            }

            return ServiceResult<int>.Success(ownership.DoctorId);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            return Failure<int>(
                "provider_ownership.persistence_failure",
                "Provider ownership could not be resolved.");
        }
    }

    private bool CanManageOwnership() =>
        currentUser.IsAuthenticated &&
        !string.IsNullOrWhiteSpace(currentUser.UserId) &&
        currentUser.Roles.Any(role =>
            string.Equals(role, RoleNames.SystemAdministrator, StringComparison.Ordinal) ||
            string.Equals(role, RoleNames.Administrator, StringComparison.Ordinal));

    private static ServiceResult<T> Forbidden<T>() => Failure<T>(
        "provider_ownership.forbidden",
        "The current user is not authorized to manage provider ownership.");

    private static ServiceResult<T> Conflict<T>() => Failure<T>(
        "provider_ownership.conflict",
        "The Doctor or Identity user already has provider ownership.");

    private static ServiceResult<T> Failure<T>(string code, string message, string? field = null) =>
        ServiceResult<T>.Failure(new ServiceError(code, message, field));
}
