using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Departments;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Persistence;
using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Organization.Entities;

namespace ElsheiekhHMS.Tests.Unit.Application.Departments;

public sealed class DepartmentServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 9, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetById_returns_projected_department()
    {
        var persistence = new FakeDepartmentPersistence
        {
            Details = new DepartmentDetailsDto(7, "Emergency", "Care", "101", true)
        };

        var result = await CreateService(persistence).GetByIdAsync(7);

        Assert.True(result.IsSuccess);
        Assert.Equal("Emergency", result.Value!.Name);
    }

    [Fact]
    public async Task GetById_returns_not_found_without_writing()
    {
        var persistence = new FakeDepartmentPersistence();

        var result = await CreateService(persistence).GetByIdAsync(7);

        Assert.False(result.IsSuccess);
        Assert.Equal("department.not_found", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Search_passes_bounded_request_to_persistence()
    {
        var persistence = new FakeDepartmentPersistence
        {
            SearchResult = new PagedResult<DepartmentSummaryDto>(
                [new(7, "Emergency", true, "101")], 1, 1, 25)
        };
        var request = new DepartmentSearchRequest(
            " emer ", true, new PageRequest(1, 25), DepartmentSortField.Name);

        var result = await CreateService(persistence).SearchAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Same(request, persistence.LastSearchRequest);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task Create_stages_audit_and_saves_once()
    {
        var persistence = new FakeDepartmentPersistence();
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).CreateAsync(
            new CreateDepartmentRequest("  Emergency  ", "Care", "101"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Emergency", result.Value!.Name);
        Assert.Equal(1, persistence.SaveChangesCalls);
        var entry = Assert.Single(audit.Events);
        Assert.Equal(AuditActions.DepartmentCreated, entry.Action);
        Assert.Equal("Department", entry.TargetType);
        Assert.Equal("Emergency", entry.TargetId);
    }

    [Fact]
    public async Task Create_rejects_invalid_request_without_writing()
    {
        var persistence = new FakeDepartmentPersistence();

        var result = await CreateService(persistence).CreateAsync(
            new CreateDepartmentRequest("", null, null));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "department.name.required");
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Update_stages_audit_and_saves_once()
    {
        var department = CreateDepartment();
        var persistence = new FakeDepartmentPersistence { TrackedDepartment = department };
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).UpdateAsync(
            7, new UpdateDepartmentRequest("Clinical Laboratory", "Testing", "205"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Clinical Laboratory", result.Value!.Name);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.DepartmentUpdated, Assert.Single(audit.Events).Action);
        Assert.Equal("7", audit.Events[0].TargetId);
    }

    [Fact]
    public async Task Deactivate_stages_audit_and_preserves_historical_entity()
    {
        var department = CreateDepartment();
        var persistence = new FakeDepartmentPersistence { TrackedDepartment = department };
        var audit = new RecordingAuditWriter();

        var result = await CreateService(persistence, audit).DeactivateAsync(7);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        Assert.False(department.IsActive);
        Assert.Equal(1, persistence.SaveChangesCalls);
        Assert.Equal(AuditActions.DepartmentDeactivated, Assert.Single(audit.Events).Action);
    }

    [Fact]
    public async Task Deactivate_maps_repeated_transition_to_domain_error_without_save()
    {
        var department = CreateDepartment();
        department.Deactivate(Now, "admin-1");
        var persistence = new FakeDepartmentPersistence { TrackedDepartment = department };

        var result = await CreateService(persistence).DeactivateAsync(7);

        Assert.False(result.IsSuccess);
        Assert.Equal("department.domain_rule", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    [Fact]
    public async Task Provider_is_rejected_before_persistence()
    {
        var persistence = new FakeDepartmentPersistence();

        var result = await CreateService(persistence, roles: [RoleNames.Provider])
            .GetByIdAsync(7);

        Assert.False(result.IsSuccess);
        Assert.Equal("department.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.GetCalls);
    }

    [Fact]
    public async Task Receptionist_is_not_granted_configuration_authority()
    {
        var persistence = new FakeDepartmentPersistence();

        var result = await CreateService(persistence, roles: [RoleNames.Receptionist])
            .SearchAsync(new DepartmentSearchRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal("department.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.SearchCalls);
    }

    [Fact]
    public async Task Administrator_is_not_granted_configuration_authority_by_current_policy()
    {
        var persistence = new FakeDepartmentPersistence();

        var result = await CreateService(persistence, roles: [RoleNames.Administrator])
            .GetByIdAsync(7);

        Assert.False(result.IsSuccess);
        Assert.Equal("department.forbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, persistence.GetCalls);
    }

    [Fact]
    public async Task Cancellation_is_propagated_before_persistence()
    {
        var persistence = new FakeDepartmentPersistence();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateService(persistence).SearchAsync(new DepartmentSearchRequest(), cancellation.Token));

        Assert.Equal(0, persistence.SearchCalls);
    }

    [Fact]
    public async Task Persistence_failure_is_translated_without_details()
    {
        var persistence = new FakeDepartmentPersistence
        {
            SaveException = new InvalidOperationException("database details")
        };

        var result = await CreateService(persistence).CreateAsync(
            new CreateDepartmentRequest("Emergency", null, null));

        Assert.False(result.IsSuccess);
        Assert.Equal("department.persistence_failure", Assert.Single(result.Errors).Code);
        Assert.DoesNotContain("database details", result.Errors[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_inactive_department_maps_domain_rule_without_mutation()
    {
        var department = CreateDepartment();
        department.Deactivate(Now, "admin-1");
        var persistence = new FakeDepartmentPersistence { TrackedDepartment = department };

        var result = await CreateService(persistence).UpdateAsync(
            7, new UpdateDepartmentRequest("Cardiology", null, null));

        Assert.False(result.IsSuccess);
        Assert.Equal("department.domain_rule", Assert.Single(result.Errors).Code);
        Assert.Equal("Emergency", department.Name);
        Assert.Equal(0, persistence.SaveChangesCalls);
    }

    private static DepartmentService CreateService(
        FakeDepartmentPersistence persistence,
        RecordingAuditWriter? audit = null,
        IReadOnlyCollection<string>? roles = null) =>
        new(
            persistence,
            audit ?? new RecordingAuditWriter(),
            new TestCurrentUser("stable-user", roles ?? [RoleNames.SystemAdministrator]),
            new FixedTimeProvider(Now));

    private static Department CreateDepartment()
    {
        var department = new Department("Emergency", "Emergency care", "101", Now.AddHours(-1), "admin-1");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(department, 7);
        return department;
    }

    private sealed class TestCurrentUser(
        string userId,
        IReadOnlyCollection<string> roles) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserId { get; } = userId;
        public string? UserName => "operator";
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class RecordingAuditWriter : IAuditEventWriter
    {
        public List<AuditEventRequest> Events { get; } = [];

        public Task RecordAsync(AuditEventRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDepartmentPersistence : IDepartmentPersistence
    {
        public DepartmentDetailsDto? Details { get; init; }
        public PagedResult<DepartmentSummaryDto> SearchResult { get; init; } =
            new([], 0, 1, 25);
        public Department? TrackedDepartment { get; init; }
        public Exception? SaveException { get; init; }
        public int GetCalls { get; private set; }
        public int SearchCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }
        public DepartmentSearchRequest? LastSearchRequest { get; private set; }

        public Task<DepartmentDetailsDto?> GetDetailsAsync(int departmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetCalls++;
            return Task.FromResult(Details);
        }

        public Task<PagedResult<DepartmentSummaryDto>> SearchAsync(
            DepartmentSearchRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SearchCalls++;
            LastSearchRequest = request;
            return Task.FromResult(SearchResult);
        }

        public Task<Department?> LoadTrackedAsync(int departmentId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(TrackedDepartment);
        }

        public void Add(Department department)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveChangesCalls++;
            if (SaveException is not null) throw SaveException;
            return Task.CompletedTask;
        }
    }
}
