using Calendar.Models;
namespace Calendar.DTOs.Appointment;

public class ParticipantResponseDto
{
    public Guid AppointmentId { get; set; }
    public Guid UserId { get; set; }
    public Models.User User { get; set; }
}