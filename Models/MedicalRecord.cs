using System;
using System.ComponentModel.DataAnnotations;

public class MedicalRecord
{
    public int Id { get; set; }

    [Required]
    public string PatientId { get; set; } = "";

    [Required]
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public ApplicationUser Patient { get; set; } = null!;
}