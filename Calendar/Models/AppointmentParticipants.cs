using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class AppointmentParticipant
{
    [ForeignKey(nameof(Appointment))]
    public Guid AppointmentId { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    public User User { get; set; }
}