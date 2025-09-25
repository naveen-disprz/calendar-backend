using Calendar.Business;
using Calendar.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Calendar.Controllers
{
    [ApiController]
    [Route("api/attendee")]
    [Authorize] // Requires JWT authentication
    public class AttendeeController : ControllerBase
    {
        private readonly IAppointmentAttendeeBL _appointmentAttendeeBL;
        private readonly ILogger<AttendeeController> _logger;

        public AttendeeController(
            IAppointmentAttendeeBL appointmentAttendeeBL,
            ILogger<AttendeeController> logger)
        {
            _appointmentAttendeeBL = appointmentAttendeeBL;
            _logger = logger;
        }

        /// <summary>
        /// Get all available users that can be selected as attendees
        /// </summary>
        /// <param name="search">Optional search term to filter users by name or email</param>
        /// <param name="excludeCurrentUser">Exclude the current user from results (default: false)</param>
        /// <param name="pageNumber">Page number for pagination (default: 1)</param>
        /// <param name="pageSize">Page size for pagination (default: 50, max: 100)</param>
        /// <returns>List of available users</returns>
        [HttpGet]
        public async Task<ActionResult<AttendeeResponseDto>> GetAllAttendees(
            [FromQuery] bool excludeCurrentUser = true)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    return Unauthorized(new ErrorResponseDto("Invalid user token"));
                }
                

                var result = await _appointmentAttendeeBL.GetAvailableAttendeesAsync(
                    excludeCurrentUser ? currentUserId : null);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Get attendees failed - invalid argument: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get attendees failed for user {UserId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new ErrorResponseDto("An error occurred while retrieving attendees"));
            }
        }

        [HttpPost("checkAvailability")]
        public async Task<ActionResult<AvailabilityResponseDto>> CheckAttendeeAvailability(
            [FromBody] CheckAvailabilityRequestDto request)
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

                // Get user ID from JWT token for logging
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    return Unauthorized(new ErrorResponseDto("Invalid user token"));
                }

                var result = await _appointmentAttendeeBL.CheckAttendeeAvailabilityAsync(request);

                _logger.LogInformation("Availability check by user {UserId} for attendee {AttendeeId}: {IsAvailable}",
                    currentUserId, request.AttendeeId, result.IsAvailable);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Availability check failed - invalid argument: {Message}", ex.Message);
                return BadRequest(new ErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Availability check failed for attendee {AttendeeId} by user {UserId}",
                    request.AttendeeId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new ErrorResponseDto("An error occurred while checking attendee availability"));
            }
        }
        
        [HttpGet("appointment/{appointmentId}")]
        public async Task<ActionResult<List<AttendeeResponseDto>>> GetAppointmentAttendees(Guid appointmentId)
        {
            try
            {
                // Get user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    return Unauthorized(new ErrorResponseDto("Invalid user token"));
                }

                var result = await _appointmentAttendeeBL.GetAppointmentAttendeesAsync(appointmentId, currentUserId);

                _logger.LogInformation("Retrieved {Count} attendees for appointment {AppointmentId} by user {UserId}", 
                    result.Count, appointmentId, currentUserId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Get appointment attendees failed - not found: {Message}", ex.Message);
                return NotFound(new ErrorResponseDto(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get appointment attendees failed for appointment {AppointmentId} by user {UserId}", 
                    appointmentId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new ErrorResponseDto("An error occurred while retrieving appointment attendees"));
            }
        }
    }
}