using Calendar.DTOs;
using Calendar.Models;

namespace Calendar.Business
{
    public interface IAppointmentBL
    {
        Task<AppointmentResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto request, Guid userId);

        Task<AppointmentResponseDto> UpdateAppointmentAsync(Guid appointmentId, UpdateAppointmentRequestDto request,
            Guid userId);

        Task<List<AppointmentResponseDto>> GetAppointmentsAsync(Guid userId, DateTime fromDate, DateTime toDate,
            Guid? appointmentTypeId = null, bool? includeRecurring = true);
        
        Task<bool> DeleteAppointmentAsync(Guid appointmentId, Guid userId);
        
        Task<List<AppointmentType>> GetAppointmentTypesAsync();
    }
}