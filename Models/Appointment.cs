public class Appointment
{
    public int Id { get; set; }

    public string PatientId { get; set; } // Clé étrangère vers ApplicationUser
    public ApplicationUser Patient { get; set; }

    public string DoctorId { get; set; } // Clé étrangère vers le médecin
    public ApplicationUser Doctor { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } // Pending, Approved, Rejected, Completed
}