using ElsheiekhHMS.Application.Clinical.ProviderOwnership;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Application.Clinical;

public sealed class ProviderOwnershipServiceTests
{
    [Fact]
    public async Task Assign_maps_active_provider_and_records_one_audit_write()
    {
        var persistence = CreatePersistence();
        var audit = new TestAuditWriter();
        var service = CreateService(persistence, audit, "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ProviderOwnershipDto(12, "provider-1"), result.Value);
        Assert.Equal(1, persistence.SaveCount);
        var auditEvent = Assert.Single(audit.Events);
        Assert.Equal(AuditActions.ProviderOwnershipAssigned, auditEvent.Action);
    }

    [Fact]
    public async Task Assign_is_idempotent_for_the_same_user_and_doctor()
    {
        var persistence = CreatePersistence();
        persistence.Ownership[12] = new ProviderOwnershipDto(12, "provider-1");
        var audit = new TestAuditWriter();
        var service = CreateService(persistence, audit, "SystemAdministrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, persistence.SaveCount);
        Assert.Empty(audit.Events);
    }

    [Fact]
    public async Task Assign_rejects_a_doctor_that_is_already_owned()
    {
        var persistence = CreatePersistence();
        persistence.Ownership[12] = new ProviderOwnershipDto(12, "provider-2");
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.conflict", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Assign_rejects_a_user_that_is_already_owned()
    {
        var persistence = CreatePersistence();
        persistence.Ownership[14] = new ProviderOwnershipDto(14, "provider-1");
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.conflict", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Assign_rejects_missing_doctor()
    {
        var persistence = CreatePersistence();
        persistence.Doctors.Remove(12);
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.doctor_not_found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Assign_rejects_missing_user()
    {
        var persistence = CreatePersistence();
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "missing"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.user_not_found", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Assign_rejects_a_user_without_the_provider_role()
    {
        var persistence = CreatePersistence();
        persistence.Accounts["administrator-1"] = new ProviderAccountStatus(true, false, true);
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "administrator-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.provider_role_required", Assert.Single(result.Errors).Code);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Assign_rejects_inactive_or_disabled_provider(bool active, bool loginAllowed)
    {
        var persistence = CreatePersistence();
        persistence.Accounts["provider-1"] = new ProviderAccountStatus(true, true, active && loginAllowed);
        var service = CreateService(persistence, new TestAuditWriter(), "Administrator");

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.account_inactive", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Assign_requires_an_administrator_role_for_the_assigning_actor()
    {
        var persistence = CreatePersistence();
        var service = CreateService(persistence, new TestAuditWriter(), RoleNames.Receptionist);

        var result = await service.AssignAsync(new AssignProviderOwnershipRequest(12, "provider-1"));

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.forbidden", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Unassign_removes_only_the_mapping_and_audits_the_action()
    {
        var persistence = CreatePersistence();
        persistence.Ownership[12] = new ProviderOwnershipDto(12, "provider-1");
        var audit = new TestAuditWriter();
        var service = CreateService(persistence, audit, "Administrator");

        var result = await service.UnassignAsync(12);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ProviderOwnershipDto(12, "provider-1"), result.Value);
        Assert.Empty(persistence.Ownership);
        Assert.Contains(12, persistence.Doctors.Keys);
        Assert.Equal(AuditActions.ProviderOwnershipUnassigned, Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Resolve_returns_the_active_mapped_doctor_id()
    {
        var persistence = CreatePersistence();
        persistence.Ownership[12] = new ProviderOwnershipDto(12, "provider-1");
        var service = CreateService(persistence, new TestAuditWriter(), RoleNames.Provider, "provider-1");

        var result = await service.ResolveCurrentDoctorIdAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value);
    }

    [Fact]
    public async Task Resolve_rejects_a_provider_without_a_mapping()
    {
        var service = CreateService(CreatePersistence(), new TestAuditWriter(), RoleNames.Provider, "provider-1");

        var result = await service.ResolveCurrentDoctorIdAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.mapping_missing", Assert.Single(result.Errors).Code);
    }

    [Theory]
    [InlineData(false)]
    public async Task Resolve_rejects_an_ineligible_provider_account(bool accountIsActive)
    {
        var persistence = CreatePersistence();
        persistence.Accounts["provider-1"] = new ProviderAccountStatus(true, true, accountIsActive);
        persistence.Ownership[12] = new ProviderOwnershipDto(12, "provider-1");
        var service = CreateService(persistence, new TestAuditWriter(), RoleNames.Provider, "provider-1");

        var result = await service.ResolveCurrentDoctorIdAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("provider_ownership.account_inactive", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Resolve_rejects_anonymous_and_non_provider_accounts()
    {
        var anonymous = CreateService(CreatePersistence(), new TestAuditWriter(), [], null, false);
        var anonymousResult = await anonymous.ResolveCurrentDoctorIdAsync();
        Assert.Equal("provider_ownership.unauthenticated", Assert.Single(anonymousResult.Errors).Code);

        var administrator = CreateService(CreatePersistence(), new TestAuditWriter(), RoleNames.Administrator, "administrator-1");
        var administratorResult = await administrator.ResolveCurrentDoctorIdAsync();
        Assert.Equal("provider_ownership.provider_role_required", Assert.Single(administratorResult.Errors).Code);
    }

    private static FakeProviderOwnershipPersistence CreatePersistence()
    {
        var persistence = new FakeProviderOwnershipPersistence();
        persistence.Doctors[12] = new DoctorStatusSnapshot(true, true);
        persistence.Doctors[14] = new DoctorStatusSnapshot(true, true);
        persistence.Accounts["provider-1"] = new ProviderAccountStatus(true, true, true);
        persistence.Accounts["provider-2"] = new ProviderAccountStatus(true, true, true);
        return persistence;
    }

    private static ProviderOwnershipService CreateService(
        FakeProviderOwnershipPersistence persistence,
        TestAuditWriter audit,
        string role,
        string userId = "admin-1",
        bool authenticated = true) =>
        CreateService(persistence, audit, [role], userId, authenticated);

    private static ProviderOwnershipService CreateService(
        FakeProviderOwnershipPersistence persistence,
        TestAuditWriter audit,
        IReadOnlyCollection<string> roles,
        string? userId,
        bool authenticated = true) =>
        new(persistence, audit, new TestCurrentUser(userId, roles, authenticated));

    private sealed class TestCurrentUser(
        string? userId,
        IReadOnlyCollection<string> roles,
        bool authenticated) : ICurrentUser
    {
        public bool IsAuthenticated { get; } = authenticated;
        public string? UserId { get; } = userId;
        public string? UserName => null;
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class TestAuditWriter : IAuditEventWriter
    {
        public List<AuditEventRequest> Events { get; } = [];

        public Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken = default)
        {
            Events.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProviderOwnershipPersistence : IProviderOwnershipPersistence
    {
        public Dictionary<int, ProviderOwnershipDto> Ownership { get; } = [];
        public Dictionary<string, ProviderAccountStatus> Accounts { get; } = [];
        public Dictionary<int, DoctorStatusSnapshot> Doctors { get; } = [];
        public int SaveCount { get; private set; }
        private readonly List<ProviderOwnershipDto> _pendingAdds = [];
        private readonly List<int> _pendingRemovals = [];

        public Task<ProviderAccountStatus> GetAccountStatusAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Accounts.TryGetValue(userId, out var status)
                ? status
                : new ProviderAccountStatus(false, false, false));

        public Task<DoctorStatusSnapshot> GetDoctorStatusAsync(int doctorId, CancellationToken cancellationToken) =>
            Task.FromResult(Doctors.TryGetValue(doctorId, out var status)
                ? status
                : new DoctorStatusSnapshot(false, false));

        public Task<ProviderOwnershipDto?> GetByDoctorIdAsync(int doctorId, CancellationToken cancellationToken) =>
            Task.FromResult(Ownership.TryGetValue(doctorId, out var ownership) ? ownership : null);

        public Task<ProviderOwnershipDto?> GetByUserIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Ownership.Values.SingleOrDefault(item => item.UserId == userId));

        public void Add(int doctorId, string userId) => _pendingAdds.Add(new ProviderOwnershipDto(doctorId, userId));

        public Task<bool> RemoveByDoctorIdAsync(int doctorId, CancellationToken cancellationToken)
        {
            if (!Ownership.ContainsKey(doctorId)) return Task.FromResult(false);
            _pendingRemovals.Add(doctorId);
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            foreach (var id in _pendingRemovals) Ownership.Remove(id);
            foreach (var ownership in _pendingAdds) Ownership[ownership.DoctorId] = ownership;
            _pendingRemovals.Clear();
            _pendingAdds.Clear();
            return Task.CompletedTask;
        }
    }
}
