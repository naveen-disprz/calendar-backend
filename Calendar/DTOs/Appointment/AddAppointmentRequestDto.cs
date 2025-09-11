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
}