using System;
using System.ComponentModel.DataAnnotations;

public class DoctorDayOff
{
    public int Id { get; set; }

    [Required]
    public string DoctorId { get; set; } = null!;

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    // Navigation
    public ApplicationUser Doctor { get; set; } = null!;
}