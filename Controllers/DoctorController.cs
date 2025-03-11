using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HospitalAppointmentSystem.Models;
using HospitalAppointmentSystem.Data;
using System.Linq;
using System.Security.Claims;

namespace HospitalAppointmentSystem.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly AppDbContext _context;

        public DoctorController(AppDbContext context)
        {
            _context = context;
        }

        // Voir les rendez-vous du docteur connecté
        public IActionResult Appointments()
        {
            var doctorId = _context.Doctors
                .Where(d => d.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                .Select(d => d.DoctorId)
                .FirstOrDefault();

            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .ToList();

            return View(appointments);
        }

        // Approuver ou rejeter un rendez-vous
        public IActionResult UpdateStatus(int id, string status)
        {
            var appointment = _context.Appointments.Find(id);
            if (appointment != null)
            {
                appointment.Status = status;
                _context.SaveChanges();
            }
            return RedirectToAction("Appointments");
        }
    }
}
