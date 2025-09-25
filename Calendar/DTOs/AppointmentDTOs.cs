using System.ComponentModel.DataAnnotations;

namespace Calendar.DTOs;

// Create Appointment Request
public class CreateAppointmentRequestDto
{
    [MaxLength(200)] public string Title { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Description { get; set; }

    [Required] public DateTime StartDateTime { get; set; }

    [Required] public DateTime EndDateTime { get; set; }

    [MaxLength(500)] public string? Location { get; set; }

    public Guid? AppointmentTypeId { get; set; }

    public List<Guid> AttendeeIds { get; set; } = new();

    // Recurrence properties (optional)
    public RecurrenceRequestDto? Recurrence { get; set; }
}

// Recurrence Request (for recurring appointments)
public class RecurrenceRequestDto
{
    [Required]
    [RegularExpression(@"^(DAILY|WEEKLY|MONTHLY)$", ErrorMessage = "Frequency must be DAILY, WEEKLY, or MONTHLY")]
    public string Frequency { get; set; } = string.Empty;

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public List<int> DaysOfMonth { get; set; } = new();

    public DateTime? EndDate { get; set; }

}

// Update Appointment Request
public class UpdateAppointmentRequestDto
{
    [MaxLength(200)] public string Title { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Description { get; set; }

    [Required] public DateTime StartDateTime { get; set; }

    [Required] public DateTime EndDateTime { get; set; }

    [MaxLength(500)] public string? Location { get; set; }

    public Guid? AppointmentTypeId { get; set; }

    public List<Guid> AttendeeIds { get; set; } = new();
    
    public RecurrenceRequestDto? Recurrence { get; set; }
    
}

// Appointment Response
public class AppointmentResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Location { get; set; }
    public Guid OrganizerId { get; set; }
    public string OrganizerName { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string FormattedTimeRange { get; set; } = string.Empty;
    public string FormattedDateTimeRange { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Appointment Type
    public AppointmentTypeResponseDto? AppointmentType { get; set; }

    // Recurrence
    public RecurrenceResponseDto? Recurrence { get; set; }
    
    // Add these properties for recurring instance tracking
    public bool IsRecurringInstance { get; set; }
    public Guid? ParentAppointmentId { get; set; }
    public DateTime? ParentAppointmentStartDateTime { get; set; }
    public DateTime? ParentAppointmentEndDateTime { get; set; }
}

// Appointment Type Response
public class AppointmentTypeResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

// Recurrence Response
public class RecurrenceResponseDto
{
    public Guid Id { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public List<int> DaysOfMonth { get; set; } = new();
    public DateTime? EndDate { get; set; }
}
