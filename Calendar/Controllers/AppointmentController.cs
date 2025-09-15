using Calendar.Business;
using Calendar.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calendar.Controllers;

[ApiController]
[Route("api/appointment")]
[Authorize] // Requires JWT authentication
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentBL _appointmentBL;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(
        IAppointmentBL appointmentBL,
        ILogger<AppointmentController> logger)
    {
        _appointmentBL = appointmentBL;
        _logger = logger;
    }

    /// <summary>
    /// Create a new appointment
    /// </summary>
    /// <param name="request">Create appointment request</param>
    /// <returns>Created appointment details</returns>
    [HttpPost]
    public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment(
        [FromBody] CreateAppointmentRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ErrorResponseDto("Validation failed", errors));
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponseDto("Invalid user token"));
            }

            var result = await _appointmentBL.CreateAppointmentAsync(request, userId);

            _logger.LogInformation("Appointment created successfully: {AppointmentId} by user {UserId}",
                result.Id, userId);

            return CreatedAtAction(
                nameof(CreateAppointment),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Appointment creation failed - conflict: {Message}", ex.Message);
            return Conflict(new ErrorResponseDto(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Appointment creation failed - invalid argument: {Message}", ex.Message);
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Appointment creation failed - unauthorized: {Message}", ex.Message);
            return Unauthorized(new ErrorResponseDto(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment creation failed for user {UserId}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, new ErrorResponseDto("An error occurred while creating the appointment"));
        }
    }

    /// <summary>
    /// Update an existing appointment
    /// </summary>
    /// <param name="id">Appointment ID</param>
    /// <param name="request">Update appointment request</param>
    /// <returns>Updated appointment details</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(Guid id,
        [FromBody] UpdateAppointmentRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new ErrorResponseDto("Validation failed", errors));
            }

            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponseDto("Invalid user token"));
            }

            var result = await _appointmentBL.UpdateAppointmentAsync(id, request, userId);

            _logger.LogInformation("Appointment updated successfully: {AppointmentId} by user {UserId}",
                id, userId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Appointment update failed - not found or invalid argument: {Message}", ex.Message);
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Appointment update failed - unauthorized: {Message}", ex.Message);
            return Unauthorized(new ErrorResponseDto(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Appointment update failed - conflict: {Message}", ex.Message);
            return Conflict(new ErrorResponseDto(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment update failed for appointment {AppointmentId} by user {UserId}",
                id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, new ErrorResponseDto("An error occurred while updating the appointment"));
        }
    }
    
    [HttpGet]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetAppointments(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] Guid? appointmentTypeId = null,
            [FromQuery] bool? includeRecurring = true)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new ErrorResponseDto("Invalid user token"));
                }

                // Set default date range if not provided
                var startDate = fromDate ?? DateTime.Today;
                var endDate = toDate ?? startDate.AddDays(7);

                Console.WriteLine(startDate);
                Console.WriteLine(endDate);
                // Validate date range
                if (startDate > endDate)
                {
                    return BadRequest(new ErrorResponseDto("fromDate cannot be greater than toDate"));
                }

                // Limit date range to prevent excessive queries (e.g., max 1 year)
                if ((endDate - startDate).TotalDays > 365)
                {
                    return BadRequest(new ErrorResponseDto("Date range cannot exceed 365 days"));
                }

                var result = await _appointmentBL.GetAppointmentsAsync(
                    userId, 
                    startDate, 
                    endDate, 
                    appointmentTypeId, 
                    includeRecurring);

                _logger.LogInformation("Retrieved {Count} appointments for user {UserId} from {FromDate} to {ToDate}", 
                    result.Count, userId, startDate, endDate);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Get appointments failed - invalid argument: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get appointments failed for user {UserId}", 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new ErrorResponseDto("An error occurred while retrieving appointments"));
            }
        }
        
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAppointment(Guid id)
    {
        try
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponseDto("Invalid user token"));
            }

            var result = await _appointmentBL.DeleteAppointmentAsync(id, userId);

            if (result)
            {
                _logger.LogInformation("Appointment deleted successfully: {AppointmentId} by user {UserId}", 
                    id, userId);
                return NoContent(); // 204 No Content - successful deletion
            }
            else
            {
                _logger.LogWarning("Appointment deletion failed - appointment not found: {AppointmentId}", id);
                return NotFound(new ErrorResponseDto("Appointment not found"));
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Appointment deletion failed - not found: {Message}", ex.Message);
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Appointment deletion failed - unauthorized: {Message}", ex.Message);
            return Unauthorized(new ErrorResponseDto(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Appointment deletion failed for appointment {AppointmentId} by user {UserId}", 
                id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            return StatusCode(500, new ErrorResponseDto("An error occurred while deleting the appointment"));
        }
    }
    
    [HttpGet("types")]
    public async Task<ActionResult> GetAppointmentTypes()
    {
        try
        {
            // Get user ID from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponseDto("Invalid user token"));
            }

            var types = await _appointmentBL.GetAppointmentTypesAsync();
            
            return Ok(types);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponseDto("An error occurred while fetching the appointment types"));
        }
    }
}