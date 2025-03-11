using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalAppointmentSystem.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        [Required]
        public string PatientId { get; set; } = string.Empty;

        [Required]
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = new Doctor();

        [Required]
        public DateTime AppointmentDate { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
