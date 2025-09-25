namespace Calendar.DTOs;

// Attendee Response DTO
public class AttendeeResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

// Check Availability Request DTO
public class CheckAvailabilityRequestDto
{
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public Guid AttendeeId { get; set; }
    public Guid? ExcludeAppointmentId { get; set; } // For updating existing appointments
}

public class AvailabilityResponseDto
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public AttendeeResponseDto Attendee { get; set; } = new();
    public DateTime RequestedStartTime { get; set; }
    public DateTime RequestedEndTime { get; set; }
}