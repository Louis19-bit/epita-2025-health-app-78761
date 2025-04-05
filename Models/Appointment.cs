public class Appointment
{
    public int Id { get; set; }

    public string PatientId { get; set; } // Foreign key to ApplicationUser
    public ApplicationUser Patient { get; set; }

    public string DoctorId { get; set; } // Foreign key to Doctor
    public ApplicationUser Doctor { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } // Pending, Approved, Rejected, Completed
    
    public bool ReminderSent { get; set; } = false; // Reminder service by email 24h before appointment
}