namespace ElsheiekhHMS.Infrastructure.Identity.Entities;

public sealed class DoctorApplicationUserLink
{
    private DoctorApplicationUserLink()
    {
        UserId = null!;
    }

    public DoctorApplicationUserLink(int doctorId, string userId)
    {
        DoctorId = doctorId;
        UserId = userId;
    }

    public int DoctorId { get; private set; }

    public string UserId { get; private set; }
}
