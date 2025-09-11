using Calendar.DTOs.Appointment;
using Calendar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentsController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> GetAppointments([FromQuery] GetAppointmentRequestDto getAppointmentRequestDto)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var appointments = await _appointmentService.GetAsync(getAppointmentRequestDto, userId);
            return Ok(appointments);
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

    [Authorize, HttpPost]
    public async Task<IActionResult> AddAppointment([FromBody] AddAppointmentRequestDto addAppointmentRequestDto)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var appointment = await _appointmentService.AddAsync(addAppointmentRequestDto, userId);
            return Ok(new { appointmentId = appointment.AppointmentId });
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


    [Authorize, HttpPut("{appointmentId}")]
    public async Task<IActionResult> EditAppointment(
        [FromRoute] string appointmentId,
        [FromBody] AddAppointmentRequestDto appointmentDto)
    {
        try
        {
            var success = await _appointmentService.EditAsync(Guid.Parse(appointmentId), appointmentDto);
            if (!success)
            {
                return NotFound(new { message = "Appointment not found" });
            }
            return NoContent(); // 204 No Content
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize, HttpDelete("{appointmentId}")]
    public async Task<IActionResult> DeleteAppointment(Guid appointmentId)
    {
        try
        {
            await _appointmentService.DeleteAsync(appointmentId);
            return NoContent(); // 204 No Content
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize, HttpGet("types")]
    public async Task<IActionResult> GetAppointmentTypes()
    {
        try
        {
            var types = await _appointmentService.GetTypesAsync();
            return Ok(types);
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