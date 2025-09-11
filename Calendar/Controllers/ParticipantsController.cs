using Calendar.DTOs.Appointment;
using Calendar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calendar.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParticipantsController : ControllerBase
{
    private readonly ParticipantService _participantService;

    public ParticipantsController(ParticipantService participantService)
    {
        _participantService = participantService;
    }

    [HttpGet("all"), Authorize]
    public async Task<IActionResult> GetAllParticipants()
    {
        try
        {
            var users = await _participantService.GetAllAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> GetAppointmentParticipants([FromQuery] Guid AppointmentId)
    {
        try
        {
            var participants = await _participantService.GetByAppointmentIdAsync(AppointmentId);
            return Ok(participants);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("checkAvailability"), Authorize]
    public async Task<IActionResult> CheckAvailability(CheckAvailabilityRequestDto checkAvailabilityRequestDto)
    {
        try
        {
            var result = await _participantService.checkAvailability(checkAvailabilityRequestDto);
            if(!result) return BadRequest(new { message = "User is not available" });
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}