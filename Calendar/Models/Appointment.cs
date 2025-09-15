using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class Appointment
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid OrganizerId { get; set; }

    [Required] [MaxLength(200)] public string Title { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Description { get; set; }

    [Required] public DateTime StartDateTime { get; set; }

    [Required] public DateTime EndDateTime { get; set; }

    [MaxLength(500)] public string? Location { get; set; }

    public Guid? AppointmentTypeId { get; set; }

    public Guid? RecurrenceRuleId { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required] public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    [ForeignKey("OrganizerId")] public virtual User Organizer { get; set; } = null!;

    [ForeignKey("AppointmentTypeId")] public virtual AppointmentType? AppointmentType { get; set; }

    [ForeignKey("RecurrenceRuleId")] public virtual RecurrenceRule? RecurrenceRule { get; set; }

    public virtual ICollection<AppointmentAttendee> Attendees { get; set; } = new List<AppointmentAttendee>();

    // Computed Properties
    [NotMapped] public TimeSpan Duration => EndDateTime - StartDateTime;

    [NotMapped] public bool IsRecurring => RecurrenceRuleId != null;

    [NotMapped]
    public bool IsAllDay => StartDateTime.TimeOfDay == TimeSpan.Zero &&
                            EndDateTime.TimeOfDay == TimeSpan.Zero &&
                            Duration.Days >= 1;

    [NotMapped]
    public string FormattedTimeRange =>
        $"{StartDateTime:HH:mm} - {EndDateTime:HH:mm}";

    [NotMapped]
    public string FormattedDateTimeRange =>
        StartDateTime.Date == EndDateTime.Date
            ? $"{StartDateTime:MMM dd, yyyy} {FormattedTimeRange}"
            : $"{StartDateTime:MMM dd, yyyy HH:mm} - {EndDateTime:MMM dd, yyyy HH:mm}";

    // Helper Methods
    public bool HasConflictWith(Appointment other)
    {
        return StartDateTime < other.EndDateTime && EndDateTime > other.StartDateTime;
    }

    public bool IsOnDate(DateTime date)
    {
        var dateOnly = date.Date;
        return StartDateTime.Date <= dateOnly && EndDateTime.Date >= dateOnly;
    }

    public List<User> GetAllAttendees()
    {
        var attendeeUsers = Attendees.Select(a => a.User).ToList();
        if (!attendeeUsers.Any(u => u.Id == OrganizerId))
        {
            attendeeUsers.Add(Organizer);
        }

        return attendeeUsers;
    }
}