namespace DWB.Services
{
    public interface IDoctorScheduleService
    {
        // Gets the schedule string (e.g., "Mon, Wed (09:00 AM - 05:00 PM)")
        Task<string> GetScheduleStringAsync(int doctorId, int companyId);
    }
}
