using Calendar.Models;

namespace Calendar.DataAccess
{
    public interface IAppointmentAttendeeDAL
    {
        Task<List<AppointmentAttendee>> CreateAttendeesAsync(List<AppointmentAttendee> attendees);
        Task<AppointmentAttendee> CreateAttendeeAsync(AppointmentAttendee attendee);
        Task<List<AppointmentAttendee>> GetAttendeesByAppointmentIdAsync(Guid appointmentId);
        Task<List<AppointmentAttendee>> GetAttendeesByUserIdAsync(Guid userId);
        Task<AppointmentAttendee?> GetAttendeeAsync(Guid appointmentId, Guid userId);
        Task<bool> IsOrganizerAsync(Guid appointmentId, Guid userId);
        Task<bool> IsAttendeeAsync(Guid appointmentId, Guid userId);
        Task<bool> DeleteAttendeeAsync(Guid appointmentId, Guid userId);
        Task<bool> DeleteAllAttendeesAsync(Guid appointmentId);
        Task<int> GetAttendeeCountAsync(Guid appointmentId);
        Task<List<AppointmentAttendee>> UpdateAttendeesAsync(Guid appointmentId, List<Guid> newAttendeeIds, Guid organizerId);
        Task<bool> ExistsAsync(Guid appointmentId, Guid userId);
    }
}