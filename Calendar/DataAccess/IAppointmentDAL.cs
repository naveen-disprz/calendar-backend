using Calendar.Models;

namespace Calendar.DataAccess;

public interface IAppointmentDAL
{
    Task<Appointment> CreateAsync(Appointment appointment);
    Task<Appointment> UpdateAsync(Appointment appointment);
    Task<bool> DeleteAsync(Guid appointmentId);
    Task<List<Appointment>> GetConflictingAppointmentsAsync(DateTime startDateTime, DateTime endDateTime, Guid userId,
        Guid? excludeAppointmentId = null);

    Task<bool> ExistsAsync(Guid appointmentId);
    Task<Appointment?> GetByIdAsync(Guid appointmentId);

    Task<List<Appointment>> GetAppointmentsByDateRangeAsync(Guid userId, DateTime fromDate, DateTime toDate,
        Guid? appointmentTypeId = null, bool? includeRecurring = true);
    
    Task<List<Appointment>> GetPotentialConflictingAppointmentsAsync(
        DateTime startDateTime, 
        DateTime endDateTime, 
        Guid userId, 
        Guid? excludeAppointmentId = null);
}