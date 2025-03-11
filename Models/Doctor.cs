using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HospitalAppointmentSystem.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public IdentityUser User { get; set; } = new IdentityUser();
        
        [Required]
        public string Specialization { get; set; } = string.Empty;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
