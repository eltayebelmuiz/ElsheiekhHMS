using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record AppointmentSearchRequest
{
    public AppointmentSearchRequest(
        DateOnly fromDate,
        DateOnly toDate,
        int? patientId = null,
        int? doctorId = null,
        int? departmentId = null,
        AppointmentStatus? status = null,
        PageRequest? page = null,
        AppointmentSortField sortBy = AppointmentSortField.ScheduledDateTime,
        SortDirection sortDirection = SortDirection.Ascending)
    {
        FromDate = fromDate;
        ToDate = toDate;
        PatientId = patientId;
        DoctorId = doctorId;
        DepartmentId = departmentId;
        Status = status;
        Page = page ?? new PageRequest();
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public DateOnly FromDate { get; }
    public DateOnly ToDate { get; }
    public int? PatientId { get; }
    public int? DoctorId { get; }
    public int? DepartmentId { get; }
    public AppointmentStatus? Status { get; }
    public PageRequest Page { get; }
    public AppointmentSortField SortBy { get; }
    public SortDirection SortDirection { get; }
}
