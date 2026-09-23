using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Infrastructure.Identity;

public sealed class ProviderOwnershipPersistence(ElsheiekhHmsDbContext context)
    : IProviderOwnershipPersistence
{
    public async Task<ProviderAccountStatus> GetAccountStatusAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
        {
            return new ProviderAccountStatus(false, false, false);
        }

        var hasProviderRole = await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.Name == RoleNames.Provider
            select role.Id)
            .AnyAsync(cancellationToken);

        return new ProviderAccountStatus(
            true,
            hasProviderRole,
            user.LoginAllowed && user.SecurityState == AccountSecurityState.Active);
    }

    public async Task<DoctorStatusSnapshot> GetDoctorStatusAsync(
        int doctorId,
        CancellationToken cancellationToken)
    {
        var doctor = await context.Doctors
            .AsNoTracking()
            .Where(item => item.Id == doctorId)
            .Select(item => new { item.Status, item.IsDeleted })
            .SingleOrDefaultAsync(cancellationToken);
        return doctor is null
            ? new DoctorStatusSnapshot(false, false)
            : new DoctorStatusSnapshot(
                true,
                doctor.Status == DoctorStatus.Active && !doctor.IsDeleted);
    }

    public Task<ProviderOwnershipDto?> GetByDoctorIdAsync(
        int doctorId,
        CancellationToken cancellationToken) =>
        context.DoctorApplicationUserLinks
            .AsNoTracking()
            .Where(link => link.DoctorId == doctorId)
            .Select(link => new ProviderOwnershipDto(link.DoctorId, link.UserId))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<ProviderOwnershipDto?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken) =>
        context.DoctorApplicationUserLinks
            .AsNoTracking()
            .Where(link => link.UserId == userId)
            .Select(link => new ProviderOwnershipDto(link.DoctorId, link.UserId))
            .SingleOrDefaultAsync(cancellationToken);

    public void Add(int doctorId, string userId) =>
        context.DoctorApplicationUserLinks.Add(
            new DoctorApplicationUserLink(doctorId, userId));

    public async Task<bool> RemoveByDoctorIdAsync(
        int doctorId,
        CancellationToken cancellationToken)
    {
        var link = await context.DoctorApplicationUserLinks
            .SingleOrDefaultAsync(item => item.DoctorId == doctorId, cancellationToken);
        if (link is null)
        {
            return false;
        }

        context.DoctorApplicationUserLinks.Remove(link);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ProviderOwnershipConflictException(exception);
        }
    }
}
