using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class Participant
{
    [ForeignKey(nameof(Appointment))]
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User User { get; set; }
}