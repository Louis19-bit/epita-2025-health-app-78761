using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HospitalAppointmentSystem.Models;
using HospitalAppointmentSystem.Data;
using System.Linq;
using System.Security.Claims;

namespace HospitalAppointmentSystem.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentController(AppDbContext context)
        {
            _context = context;
        }

        // Voir tous les rendez-vous du patient connecté
        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appointments = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == userId)
                .ToList();
            return View(appointments);
        }

        // Formulaire de réservation de rendez-vous
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Doctors = _context.Doctors.ToList();
            return View();
        }

        // Traite la réservation de rendez-vous
        [HttpPost]
        public IActionResult Create(Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                var conflict = _context.Appointments
                    .Any(a => a.DoctorId == appointment.DoctorId && 
                              a.AppointmentDate == appointment.AppointmentDate);

                if (!conflict)
                {
                    appointment.PatientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    _context.Add(appointment);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError("", "Ce créneau est déjà réservé !");
            }
            ViewBag.Doctors = _context.Doctors.ToList();
            return View(appointment);
        }

        // Annuler un rendez-vous
        public IActionResult Cancel(int id)
        {
            var appointment = _context.Appointments.Find(id);
            if (appointment != null && appointment.PatientId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                _context.Appointments.Remove(appointment);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
