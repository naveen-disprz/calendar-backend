using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class AppointmentAttendee
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid AppointmentId { get; set; }

    [Required] public Guid UserId { get; set; }

    [Required] public bool IsOrganizer { get; set; } = false;

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey("AppointmentId")] public virtual Appointment Appointment { get; set; } = null!;

    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;

    // Computed Properties
    [NotMapped] public string Role => IsOrganizer ? "Organizer" : "Attendee";
}