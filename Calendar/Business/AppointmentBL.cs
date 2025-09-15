using Calendar.DataAccess;
using Calendar.DTOs;
using Calendar.Models;

namespace Calendar.Business;

public class AppointmentBL : IAppointmentBL
{
    private readonly IAppointmentDAL _appointmentDAL;
    private readonly IUserDAL _userDAL;
    private readonly IAppointmentTypeDAL _appointmentTypeDAL;
    private readonly IRecurrenceRuleDAL _recurrenceRuleDAL;
    private readonly IAppointmentAttendeeDAL _appointmentAttendeeDAL;
    private readonly ILogger<AppointmentBL> _logger;

    public AppointmentBL(
        IAppointmentDAL appointmentDAL,
        IAppointmentAttendeeDAL appointmentAttendeeDAL,
        IUserDAL userDAL,
        IRecurrenceRuleDAL recurrenceRuleDAL,
        IAppointmentTypeDAL appointmentTypeDAL,
        ILogger<AppointmentBL> logger)
    {
        _appointmentDAL = appointmentDAL;
        _appointmentAttendeeDAL = appointmentAttendeeDAL;
        _logger = logger;
        _userDAL = userDAL;
        _appointmentTypeDAL = appointmentTypeDAL;
        _recurrenceRuleDAL = recurrenceRuleDAL;
    }

    public async Task<AppointmentResponseDto> CreateAppointmentAsync(CreateAppointmentRequestDto request, Guid userId)
    {
        try
        {
            // Validate organizer exists
            var organizer = await _userDAL.GetByIdAsync(userId);
            if (organizer == null)
            {
                throw new UnauthorizedAccessException("User not found or inactive.");
            }

            // Validate appointment type if provided
            AppointmentType? appointmentType = null;
            if (request.AppointmentTypeId.HasValue)
            {
                appointmentType = await _appointmentTypeDAL.GetByIdAsync(request.AppointmentTypeId.Value);
                if (appointmentType == null)
                {
                    throw new ArgumentException("Invalid appointment type.");
                }
            }

            // Validate attendees if provided
            var attendees = new List<User>();
            if (request.AttendeeIds.Any())
            {
                attendees = await _userDAL.GetByIdsAsync(request.AttendeeIds);
                var invalidAttendeeIds = request.AttendeeIds.Except(attendees.Select(a => a.Id)).ToList();
                if (invalidAttendeeIds.Any())
                {
                    throw new ArgumentException($"Invalid attendee IDs: {string.Join(", ", invalidAttendeeIds)}");
                }
            }

            attendees.Add((await _userDAL.GetByIdAsync(userId))!);


            // Check for conflicts
            var conflictingAppointments = await _appointmentDAL.GetConflictingAppointmentsAsync(
                request.StartDateTime,
                request.EndDateTime,
                userId);

            if (conflictingAppointments.Any())
            {
                var conflictTitles = string.Join(", ", conflictingAppointments.Select(a => a.Title));
                throw new InvalidOperationException(
                    $"Appointment conflicts with existing appointments: {conflictTitles}");
            }

            // Create recurrence rule if provided
            RecurrenceRule? recurrenceRule = null;
            if (request.Recurrence != null)
            {
                recurrenceRule = new RecurrenceRule
                {
                    Frequency = request.Recurrence.Frequency,
                    EndDate = request.Recurrence.EndDate
                };

                if (request.Recurrence.DaysOfWeek.Any())
                {
                    recurrenceRule.SetDaysOfWeek(request.Recurrence.DaysOfWeek);
                }

                if (request.Recurrence.DaysOfMonth.Any())
                {
                    recurrenceRule.SetDaysOfMonth(request.Recurrence.DaysOfMonth);
                }
            }

            // Create appointment
            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                OrganizerId = userId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                StartDateTime = request.StartDateTime,
                EndDateTime = request.EndDateTime,
                Location = request.Location?.Trim(),
                AppointmentTypeId = request.AppointmentTypeId,
                RecurrenceRule = recurrenceRule,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            // Create appointment
            var createdAppointment = await _appointmentDAL.CreateAsync(appointment);

            // Add attendees
            if (attendees.Any())
            {
                var appointmentAttendees = attendees.Select(attendee => new AppointmentAttendee
                {
                    Id = Guid.NewGuid(),
                    AppointmentId = createdAppointment.Id,
                    UserId = attendee.Id,
                    IsOrganizer = attendee.Id == userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                }).ToList();

                await _appointmentAttendeeDAL.CreateAttendeesAsync(appointmentAttendees);
            }

            _logger.LogInformation("Appointment created successfully: {AppointmentId} by user {UserId}",
                createdAppointment.Id, userId);

            return MapToAppointmentResponseDto(createdAppointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for user {UserId}", userId);
            throw;
        }
    }

    public async Task<AppointmentResponseDto> UpdateAppointmentAsync(Guid appointmentId,
        UpdateAppointmentRequestDto request, Guid userId)
    {
        try
        {
            // Get existing appointment
            var existingAppointment = await _appointmentDAL.GetByIdAsync(appointmentId);
            if (existingAppointment == null)
            {
                throw new ArgumentException("Appointment not found.");
            }

            // Check if user is the organizer
            var isOrganizer = await _appointmentAttendeeDAL.IsOrganizerAsync(appointmentId, userId);

            if (!isOrganizer)
            {
                throw new UnauthorizedAccessException("Only the organizer can update the appointment.");
            }

            // Validate appointment type if provided
            if (request.AppointmentTypeId.HasValue)
            {
                var appointmentType = await _appointmentTypeDAL.GetByIdAsync(request.AppointmentTypeId.Value);
                if (appointmentType == null)
                {
                    throw new ArgumentException("Invalid appointment type.");
                }
            }

            // Validate attendees if provided
            if (request.AttendeeIds.Any())
            {
                var attendees = await _userDAL.GetByIdsAsync(request.AttendeeIds);
                var invalidAttendeeIds = request.AttendeeIds.Except(attendees.Select(a => a.Id)).ToList();
                if (invalidAttendeeIds.Any())
                {
                    throw new ArgumentException($"Invalid attendee IDs: {string.Join(", ", invalidAttendeeIds)}");
                }
            }

            // Check for conflicts (excluding current appointment)
            var conflictingAppointments = await _appointmentDAL.GetConflictingAppointmentsAsync(
                request.StartDateTime,
                request.EndDateTime,
                userId,
                appointmentId);

            if (conflictingAppointments.Any())
            {
                var conflictTitles = string.Join(", ", conflictingAppointments.Select(a => a.Title));
                throw new InvalidOperationException(
                    $"Appointment conflicts with existing appointments: {conflictTitles}");
            }

            Console.WriteLine(request.EndDateTime);

            // Update appointment properties
            existingAppointment.Title = request.Title.Trim();
            existingAppointment.Description = request.Description?.Trim();
            existingAppointment.StartDateTime = request.StartDateTime;
            existingAppointment.EndDateTime = request.EndDateTime;
            existingAppointment.Location = request.Location?.Trim();
            existingAppointment.AppointmentTypeId = request.AppointmentTypeId;
            existingAppointment.UpdatedAt = DateTime.UtcNow;

            RecurrenceRule? recurrenceRule = null;
            if (request.Recurrence != null)
            {
                recurrenceRule = new RecurrenceRule
                {
                    Frequency = request.Recurrence.Frequency,
                    EndDate = request.Recurrence.EndDate
                };

                if (request.Recurrence.DaysOfWeek.Any())
                {
                    recurrenceRule.SetDaysOfWeek(request.Recurrence.DaysOfWeek);
                }

                if (request.Recurrence.DaysOfMonth.Any())
                {
                    recurrenceRule.SetDaysOfMonth(request.Recurrence.DaysOfMonth);
                }
            }
            
            if (existingAppointment.RecurrenceRuleId != null && request.Recurrence != null)
            {
                await _recurrenceRuleDAL.UpdateAsync((Guid)(existingAppointment.RecurrenceRuleId), recurrenceRule);
            } else if (existingAppointment.RecurrenceRuleId != null && request.Recurrence == null)
            {
                await _recurrenceRuleDAL.DeleteAsync((Guid)(existingAppointment.RecurrenceRuleId));
                existingAppointment.RecurrenceRuleId = null;
            }
            else
            {
                var newRule = await _recurrenceRuleDAL.CreateAsync(recurrenceRule!);
                existingAppointment.RecurrenceRule = newRule;
            }


            // Update appointment in database
            var updatedAppointment = await _appointmentDAL.UpdateAsync(existingAppointment);

            // Update attendees
            var allAttendeeIds = request.AttendeeIds.ToList();
            if (!allAttendeeIds.Contains(userId))
            {
                allAttendeeIds.Add(userId); // Ensure organizer is included
            }

            await _appointmentAttendeeDAL.UpdateAttendeesAsync(appointmentId, allAttendeeIds, userId);

            _logger.LogInformation("Appointment updated successfully: {AppointmentId} by user {UserId}",
                appointmentId, userId);

            // Get the complete updated appointment
            var completeAppointment = await _appointmentDAL.GetByIdAsync(appointmentId);
            return MapToAppointmentResponseDto(completeAppointment!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId} for user {UserId}", appointmentId, userId);
            throw;
        }
    }

    public async Task<List<AppointmentResponseDto>> GetAppointmentsAsync(Guid userId, DateTime fromDate,
        DateTime toDate, Guid? appointmentTypeId = null, bool? includeRecurring = true)
    {
        try
        {
            // Validate user exists
            var user = await _userDAL.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found or inactive.");
            }

            // Validate appointment type if provided
            if (appointmentTypeId.HasValue)
            {
                var appointmentType = await _appointmentTypeDAL.GetByIdAsync(appointmentTypeId.Value);
                if (appointmentType == null)
                {
                    throw new ArgumentException("Invalid appointment type.");
                }
            }

            // Get appointments from database
            var appointments = await _appointmentDAL.GetAppointmentsByDateRangeAsync(
                userId,
                fromDate,
                toDate,
                appointmentTypeId,
                includeRecurring);

            _logger.LogInformation("Retrieved {Count} appointments for user {UserId} from {FromDate} to {ToDate}",
                appointments.Count, userId, fromDate, toDate);

            // Map to response DTOs
            return appointments.Select(MapToAppointmentResponseDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving appointments for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteAppointmentAsync(Guid appointmentId, Guid userId)
    {
        try
        {
            // Get existing appointment
            var existingAppointment = await _appointmentDAL.GetByIdAsync(appointmentId);
            if (existingAppointment == null)
            {
                throw new ArgumentException("Appointment not found.");
            }

            // Check if user is the organizer
            var isOrganizer = await _appointmentAttendeeDAL.IsOrganizerAsync(appointmentId, userId);
            if (!isOrganizer)
            {
                throw new UnauthorizedAccessException("Only the organizer can delete the appointment.");
            }

            // Soft delete the appointment
            var deleted = await _appointmentDAL.DeleteAsync(appointmentId);

            if (deleted)
            {
                // Remove all attendees when appointment is deleted
                await _appointmentAttendeeDAL.DeleteAllAttendeesAsync(appointmentId);

                _logger.LogInformation("Appointment deleted successfully: {AppointmentId} by user {UserId}",
                    appointmentId, userId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment {AppointmentId} for user {UserId}", appointmentId, userId);
            throw;
        }
    }

    public async Task<List<AppointmentType>> GetAppointmentTypesAsync()
    {
        return await _appointmentTypeDAL.GetAllAsync();
    }

    private static AppointmentResponseDto MapToAppointmentResponseDto(Appointment appointment)
    {
        return new AppointmentResponseDto
        {
            Id = appointment.Id,
            Title = appointment.Title,
            Description = appointment.Description,
            StartDateTime = appointment.StartDateTime,
            EndDateTime = appointment.EndDateTime,
            Location = appointment.Location,
            OrganizerId = appointment.OrganizerId,
            OrganizerName = appointment.Organizer?.FullName ?? string.Empty,
            IsRecurring = appointment.IsRecurring,
            FormattedTimeRange = appointment.FormattedTimeRange,
            FormattedDateTimeRange = appointment.FormattedDateTimeRange,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt,
            AppointmentType = appointment.AppointmentType != null
                ? new AppointmentTypeResponseDto
                {
                    Id = appointment.AppointmentType.Id,
                    Name = appointment.AppointmentType.Name,
                    Color = appointment.AppointmentType.Color
                }
                : null,
            Recurrence = appointment.RecurrenceRule != null
                ? new RecurrenceResponseDto
                {
                    Id = appointment.RecurrenceRule.Id,
                    Frequency = appointment.RecurrenceRule.Frequency,
                    DaysOfWeek = appointment.RecurrenceRule.GetDaysOfWeek(),
                    DaysOfMonth = appointment.RecurrenceRule.GetDaysOfMonth(),
                    EndDate = appointment.RecurrenceRule.EndDate
                }
                : null
        };
    }
}