namespace Calendar.DTOs.Appointment;

public class CheckAvailabilityRequestDto
{
    public Guid ParticipantId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}