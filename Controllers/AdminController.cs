using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HospitalAppointmentSystem.Models;
using HospitalAppointmentSystem.Data;
using System.Linq;

namespace HospitalAppointmentSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // Voir tous les docteurs
        public IActionResult Doctors()
        {
            var doctors = _context.Doctors.Include(d => d.User).ToList();
            return View(doctors);
        }

        // Ajouter un docteur
        [HttpGet]
        public IActionResult AddDoctor()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddDoctor(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                _context.Doctors.Add(doctor);
                _context.SaveChanges();
                return RedirectToAction("Doctors");
            }
            return View(doctor);
        }

        // Supprimer un docteur
        public IActionResult DeleteDoctor(int id)
        {
            var doctor = _context.Doctors.Find(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                _context.SaveChanges();
            }
            return RedirectToAction("Doctors");
        }

        // Voir tous les rendez-vous
        public IActionResult Appointments()
        {
            var appointments = _context.Appointments.Include(a => a.Doctor).ToList();
            return View(appointments);
        }
    }
}
