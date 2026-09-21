using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Appointments.Validation;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Tests.Unit.Application.Appointments;

public sealed class AppointmentValidationTests
{
    [Fact]
    public void Create_accepts_valid_request()
    {
        var request = new CreateAppointmentRequest(
            1, 2, 3, new DateOnly(2026, 10, 1), new TimeOnly(9, 30),
            AppointmentType.General, null);

        var result = new CreateAppointmentRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_rejects_invalid_ids_enum_and_notes_length()
    {
        var request = new CreateAppointmentRequest(
            0, -1, 0, DateOnly.MinValue, new TimeOnly(9, 30),
            (AppointmentType)99, new string('n', 2001));

        var result = new CreateAppointmentRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "appointment.PatientId.positive");
        Assert.Contains(result.Errors, error => error.Code == "appointment.Type.invalid");
        Assert.Contains(result.Errors, error => error.Code == "appointment.Notes.maximum");
    }

    [Fact]
    public void Cancel_normalizes_reason_and_concurrency_token()
    {
        var request = new CancelAppointmentRequest(4, "  reason  ", "  token  ");

        var result = new CancelAppointmentRequestValidator().Validate(request);

        Assert.True(result.IsValid);
        Assert.Equal("reason", request.Reason);
        Assert.Equal("token", request.ExpectedConcurrencyToken);
    }

    [Fact]
    public void Search_rejects_reversed_or_oversized_date_ranges()
    {
        var reversed = new AppointmentSearchRequest(new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 1));
        var oversized = new AppointmentSearchRequest(new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 2));

        var reversedResult = new AppointmentSearchRequestValidator().Validate(reversed);
        var oversizedResult = new AppointmentSearchRequestValidator().Validate(oversized);

        Assert.Contains(reversedResult.Errors, error => error.Code == "appointment.date_range.order");
        Assert.Contains(oversizedResult.Errors, error => error.Code == "appointment.date_range.maximum");
    }

    [Fact]
    public void Search_rejects_invalid_status_sort_and_paging()
    {
        var request = new AppointmentSearchRequest(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2),
            page: new PageRequest(0, 251), status: (AppointmentStatus)99,
            sortBy: (AppointmentSortField)99, sortDirection: (SortDirection)99);

        var result = new AppointmentSearchRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "pagination.page_number.positive");
        Assert.Contains(result.Errors, error => error.Code == "appointment.Status.invalid");
        Assert.Contains(result.Errors, error => error.Code == "appointment.SortBy.invalid");
    }
}
