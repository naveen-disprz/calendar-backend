namespace Calendar.DTOs.Appointment;

public class AddAppointmentRequestDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int AppointmentTypeId { get; set; }
    public List<string> Participants { get; set; }
    
    public string? Frequency { get; set; } // Nullable, e.g., "Daily", "Weekly", "Monthly"
    
    public DateOnly? Until { get; set; } // Nullable, the date until the recurrence is valid
}