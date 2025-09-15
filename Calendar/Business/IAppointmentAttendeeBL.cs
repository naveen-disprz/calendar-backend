using Calendar.DTOs;

namespace Calendar.Business;

public interface IAppointmentAttendeeBL
{
    Task<List<AttendeeResponseDto>> GetAvailableAttendeesAsync(Guid? excludeUserId);
    Task<AvailabilityResponseDto> CheckAttendeeAvailabilityAsync(CheckAvailabilityRequestDto request);
    Task<BulkAvailabilityResponseDto> CheckBulkAttendeeAvailabilityAsync(BulkAvailabilityRequestDto request);
    Task<List<AttendeeResponseDto>> GetAppointmentAttendeesAsync(Guid appointmentId, Guid currentUserId);
}