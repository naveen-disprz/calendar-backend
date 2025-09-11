using Calendar.DTOs.Appointment;
using Calendar.Models;

namespace Calendar.Mappers;

public class AppointmentMapper
{
    public static Appointment ToEntity(AddAppointmentRequestDto appointmentDto, Guid organizerId)
    {
        return new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            Title = appointmentDto.Title,
            Description = appointmentDto.Description,
            AppointmentDate = appointmentDto.AppointmentDate,
            StartTime = appointmentDto.StartTime,
            EndTime = appointmentDto.EndTime,
            OrganizerId = organizerId,
            AppointmentTypeId = appointmentDto.AppointmentTypeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static GetAppointmentResponseDto ToResponseDto(Appointment appointment)
    {
        return new GetAppointmentResponseDto
        {
            AppointmentId = appointment.AppointmentId,
            Title = appointment.Title,
            Description = appointment.Description,
            AppointmentDate = appointment.AppointmentDate,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            OrganizerId = appointment.OrganizerId,
            AppointmentTypeId = appointment.AppointmentTypeId,
            RecurrenceRuleId = appointment.RecurrenceRuleId,
        };
    }
}