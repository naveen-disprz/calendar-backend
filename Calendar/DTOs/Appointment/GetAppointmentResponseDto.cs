using Calendar.Models;
using Type = Calendar.Models.Type;

namespace Calendar.DTOs.Appointment;

public class GetAppointmentResponseDto
{
    public Guid AppointmentId { get; set; } =  Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // 🔑 Date of the appointment
    public DateOnly AppointmentDate { get; set; }

    // 🔑 Start and end times (time-only)
    public TimeOnly StartTime { get; set; }
    
    public TimeOnly EndTime { get; set; }

    // Foreign Keys
    public Guid OrganizerId { get; set; }
    
    public Models.User Organizer { get; set; }

    public int? RecurrenceRuleId { get; set; }
    
    public RecurrenceRule? RecurrenceRule { get; set; }

    public int? AppointmentTypeId { get; set; }
    
    public Type? AppointmentType { get; set; }
}