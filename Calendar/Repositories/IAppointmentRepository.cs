using Calendar.DTOs.Appointment;
using Calendar.Models;

namespace Calendar.Repositories;

public interface IAppointmentRepository
{
    Task<List<Appointment>> GetByUserIdAsync(string userId);
    Task<Appointment> GetByIdAsync(Guid appointmentId);
    Task AddAsync(Appointment appointment);
    Task UpdateAsync(Appointment appointment);
    Task DeleteByIdAsync(Guid appointmentId);
}