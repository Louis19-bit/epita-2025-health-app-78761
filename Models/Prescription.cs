namespace HospitalAppointmentSystem.Models;

using System;
using System.ComponentModel.DataAnnotations;

public class Prescription
{
    public int Id { get; set; }

    [Required]
    public string Medication { get; set; } = "";

    [Required]
    public string Dosage { get; set; } = "";

    public string Instructions { get; set; } = "";

    [Required]
    public DateTime IssueDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    [Required]
    public string PatientId { get; set; }

    public ApplicationUser? Patient { get; set; }

    [Required]
    public string DoctorId { get; set; }

    public ApplicationUser? Doctor { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";
}