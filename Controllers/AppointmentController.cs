using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/appointments")]
[ApiController]
public class AppointmentController : ControllerBase
{
    private readonly AppointmentService _appointmentService;

    public AppointmentController(AppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableTimeSlots([FromQuery] string doctorId, [FromQuery] DateTime date)
    {
        if (string.IsNullOrEmpty(doctorId))
            return BadRequest(new { error = "doctorId is required" });

        var slots = await _appointmentService.GetAvailableTimeSlots(doctorId, date);
        return Ok(slots);
    }

    [HttpPost("book")]
    public async Task<IActionResult> BookAppointment([FromBody] AppointmentRequestDto model)
    {
        if (model == null)
            return BadRequest("Invalid data");

        var success = await _appointmentService.BookAppointment(new Appointment
        {
            DoctorId = model.DoctorId,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Status = "Pending"
        });

        if (!success)
            return BadRequest("Ce créneau est déjà réservé.");

        return Ok("Rendez-vous réservé avec succès !");
    }
}