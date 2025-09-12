namespace Calendar.DTOs.Appointment;

public class GetAppointmentRequestDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}