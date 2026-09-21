using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Appointments;

public sealed class AppointmentContractTests
{
    [Fact]
    public void Create_contract_trims_optional_notes()
    {
        var request = new CreateAppointmentRequest(
            1, 2, 3, new DateOnly(2026, 10, 1), new TimeOnly(9, 30),
            AppointmentType.Specialist, "  Follow up  ");

        Assert.Equal("Follow up", request.Notes);
    }

    [Fact]
    public void Read_contracts_expose_current_appointment_fields_only()
    {
        var summaryNames = typeof(AppointmentSummaryDto).GetProperties().Select(property => property.Name);
        var detailNames = typeof(AppointmentDetailsDto).GetProperties().Select(property => property.Name);

        Assert.Contains("DoctorId", summaryNames);
        Assert.Contains("CancellationReason", detailNames);
        Assert.Contains("ConcurrencyToken", detailNames);
        Assert.DoesNotContain("ProviderId", summaryNames);
        Assert.DoesNotContain("FacilityId", detailNames);
        Assert.DoesNotContain("Reschedule", detailNames);
    }

    [Fact]
    public void Search_contract_has_bounded_paging_and_explicit_sorting()
    {
        var request = new AppointmentSearchRequest(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7));

        Assert.Equal(25, request.Page.PageSize);
        Assert.Equal(AppointmentSortField.ScheduledDateTime, request.SortBy);
        Assert.Equal(SortDirection.Ascending, request.SortDirection);
    }
}
