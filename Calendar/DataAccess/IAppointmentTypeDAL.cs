using Calendar.Models;

namespace Calendar.DataAccess;

public interface IAppointmentTypeDAL
{
    Task<AppointmentType?> GetByIdAsync(Guid appointmentTypeId);
    Task<List<AppointmentType>> GetAllAsync();
}