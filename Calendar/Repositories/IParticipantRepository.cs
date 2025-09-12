using Calendar.Models;

namespace Calendar.Repositories;

public interface IParticipantRepository
{
    Task<List<User>> GetByAppointmentIdAsync(Guid appointmentId);
    Task<List<User>> GetAllAsync();
    Task BulkAddAsync(List<Participant> participants);
    Task BulkRemoveAsync(Guid appointmentId, List<Guid> userIds);
    Task<List<Appointment>> GetAppointmentsByIdAsync(string userId, DateOnly fromDate, DateOnly toDate);
}