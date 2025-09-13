using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalendarApp.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(256)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Appointment> OrganizedAppointments { get; set; } = new List<Appointment>();
        public virtual ICollection<AppointmentAttendee> AttendeeAppointments { get; set; } = new List<AppointmentAttendee>();

        // Computed Properties
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string DisplayName => $"{FirstName} {LastName} ({Email})";
    }
}