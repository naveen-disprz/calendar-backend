using Calendar.DataAccess;
using Calendar.DTOs;

namespace Calendar.Business;

public class AppointmentAttendeeBL : IAppointmentAttendeeBL
{
    private readonly ILogger<AppointmentAttendeeBL> _logger;
    private readonly IUserDAL _userDAL;
    private readonly IAppointmentDAL _appointmentDAL;
    private readonly IAppointmentAttendeeDAL _appointmentAttendeeDAL;

    public AppointmentAttendeeBL(IUserDAL userDAL, IAppointmentDAL appointmentDAL,
        IAppointmentAttendeeDAL appointmentAttendeeDAL,
        ILogger<AppointmentAttendeeBL> logger)
    {
        _logger = logger;
        _userDAL = userDAL;
        _appointmentDAL = appointmentDAL;
        _appointmentAttendeeDAL = appointmentAttendeeDAL;
    }

    public async Task<List<AttendeeResponseDto>> GetAvailableAttendeesAsync(Guid? excludeUserId)
    {
        try
        {
            var attendees = await _userDAL.GetAllAsync(excludeUserId);

            return attendees.Select(u => new AttendeeResponseDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = $"{u.FirstName} {u.LastName}",
                DisplayName = $"{u.FirstName} {u.LastName}",
                Email = u.Email,
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available attendees");
            throw;
        }
    }

    public async Task<AvailabilityResponseDto> CheckAttendeeAvailabilityAsync(CheckAvailabilityRequestDto request)
    {
        try
        {
            // Validate attendee exists
            var attendee = await _userDAL.GetByIdAsync(request.AttendeeId);
            if (attendee == null)
            {
                throw new ArgumentException("Attendee not found or inactive.");
            }

            // Get conflicting appointments for the attendee
            var conflictingAppointments = await _appointmentDAL.GetConflictingAppointmentsAsync(
                request.StartDateTime,
                request.EndDateTime,
                request.AttendeeId,
                request.ExcludeAppointmentId);

            var isAvailable = !conflictingAppointments.Any();
            var message = isAvailable
                ? $"{attendee.FullName} is available for the requested time slot."
                : $"{attendee.FullName} has {conflictingAppointments.Count} conflicting appointment(s) during the requested time.";

            var response = new AvailabilityResponseDto
            {
                IsAvailable = isAvailable,
                Message = message,
                Attendee = new AttendeeResponseDto
                {
                    Id = attendee.Id,
                    Email = attendee.Email,
                    FirstName = attendee.FirstName,
                    LastName = attendee.LastName,
                    FullName = $"{attendee.FirstName} {attendee.LastName}",
                    DisplayName = $"{attendee.FirstName} {attendee.LastName}",
                },
                RequestedStartTime = request.StartDateTime,
                RequestedEndTime = request.EndDateTime,
            };

            _logger.LogInformation(
                "Availability check for attendee {AttendeeId}: {IsAvailable} ({ConflictCount} conflicts)",
                request.AttendeeId, isAvailable, conflictingAppointments.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for attendee {AttendeeId}", request.AttendeeId);
            throw;
        }
    }

    public async Task<BulkAvailabilityResponseDto> CheckBulkAttendeeAvailabilityAsync(
        BulkAvailabilityRequestDto request)
    {
        try
        {
            var availabilities = new List<AvailabilityResponseDto>();

            foreach (var attendeeId in request.AttendeeIds)
            {
                var availabilityRequest = new CheckAvailabilityRequestDto
                {
                    StartDateTime = request.StartDateTime,
                    EndDateTime = request.EndDateTime,
                    AttendeeId = attendeeId,
                    ExcludeAppointmentId = request.ExcludeAppointmentId
                };

                try
                {
                    var availability = await CheckAttendeeAvailabilityAsync(availabilityRequest);
                    availabilities.Add(availability);
                }
                catch (ArgumentException ex)
                {
                    // Handle case where attendee doesn't exist
                    availabilities.Add(new AvailabilityResponseDto
                    {
                        IsAvailable = false,
                        Message = ex.Message,
                        Attendee = new AttendeeResponseDto { Id = attendeeId },
                        RequestedStartTime = request.StartDateTime,
                        RequestedEndTime = request.EndDateTime
                    });
                }
            }

            var availableCount = availabilities.Count(a => a.IsAvailable);
            var unavailableCount = availabilities.Count - availableCount;

            var response = new BulkAvailabilityResponseDto
            {
                AttendeeAvailabilities = availabilities,
                AllAvailable = availableCount == availabilities.Count,
                AvailableCount = availableCount,
                UnavailableCount = unavailableCount,
                RequestedStartTime = request.StartDateTime,
                RequestedEndTime = request.EndDateTime
            };

            _logger.LogInformation("Bulk availability check: {Available}/{Total} attendees available",
                availableCount, availabilities.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bulk availability for {Count} attendees", request.AttendeeIds.Count);
            throw;
        }
    }

    public async Task<List<AttendeeResponseDto>> GetAppointmentAttendeesAsync(Guid appointmentId, Guid currentUserId)
    {
        try
        {
            // Verify appointment exists
            var appointment = await _appointmentDAL.GetByIdAsync(appointmentId);
            if (appointment == null)
            {
                throw new ArgumentException("Appointment not found.");
            }

            // Get attendees
            var attendees = await _appointmentAttendeeDAL.GetAttendeesByAppointmentIdAsync(appointmentId);

            _logger.LogInformation("Retrieved {Count} attendees for appointment {AppointmentId}",
                attendees.Count, appointmentId);

            return attendees.Select(aa => new AttendeeResponseDto
            {
                Id = aa.User.Id,
                Email = aa.User.Email,
                FirstName = aa.User.FirstName,
                LastName = aa.User.LastName,
                FullName = aa.User.FullName,
                DisplayName = aa.User.DisplayName,
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendees for appointment {AppointmentId}", appointmentId);
            throw;
        }
    }
}