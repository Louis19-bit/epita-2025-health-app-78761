public class Feedback
{
    public int Id { get; set; }

    public string PatientId { get; set; }
    public ApplicationUser Patient { get; set; }

    public string DoctorId { get; set; }
    public ApplicationUser Doctor { get; set; }

    public int Rating { get; set; } // entre 1 et 5

    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; }
}