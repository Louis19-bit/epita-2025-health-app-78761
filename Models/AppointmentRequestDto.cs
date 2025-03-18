using System;
using System.ComponentModel.DataAnnotations;

public class AppointmentRequestDto
{
    [Required]
    public string DoctorId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [Required]
    public string Status { get; set; }
}