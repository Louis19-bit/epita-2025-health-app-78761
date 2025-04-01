using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Authorize] // Accessible aux utilisateurs connectés
public class ManageAppointmentsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManageAppointmentsModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<AppointmentViewModel> UpcomingAppointments { get; set; } = new();
    public List<AppointmentViewModel> PastAppointments { get; set; } = new();
    public bool IsDoctor { get; set; }
    public bool IsAdmin { get; set; }

    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsValidatedByDoctor { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        IsDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
        IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        var now = DateTime.Now;

        List<Appointment> appointments;

        if (IsAdmin)
        {
            // 👑 Admin : Voir tous les rendez-vous
            appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ToListAsync();
        }
        else if (IsDoctor)
        {
            // 🩺 Docteur : Ses rendez-vous
            appointments = await _context.Appointments
                .Where(a => a.DoctorId == userId)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ToListAsync();
        }
        else
        {
            // 👤 Patient : Ses propres rendez-vous
            appointments = await _context.Appointments
                .Where(a => a.PatientId == userId)
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .ToListAsync();
        }

        var viewModels = appointments.Select(a => new AppointmentViewModel
        {
            Id = a.Id,
            PatientName = a.Patient.FullName,
            DoctorName = $"{a.Doctor.FullName}" + (string.IsNullOrEmpty(a.Doctor.Specialization) ? "" : $" ({a.Doctor.Specialization})"),
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status,
            IsValidatedByDoctor = a.Status == "Approved"
        }).ToList();

        UpcomingAppointments = viewModels.Where(a => a.StartTime >= now).OrderBy(a => a.StartTime).ToList();
        PastAppointments = viewModels.Where(a => a.StartTime < now).OrderByDescending(a => a.StartTime).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

        var isDoctor = appointment.DoctorId == userId;
        var isPatient = appointment.PatientId == userId;

        // ✅ Patients peuvent annuler uniquement 24h avant
        if (!isAdmin && isPatient && (appointment.StartTime - DateTime.Now).TotalHours < 24)
        {
            TempData["ErrorMessage"] = "You can’t cancel less than 24 hours before the appointment.";
            return RedirectToPage();
        }

        if (!(isAdmin || isDoctor || isPatient))
            return Forbid();

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAcceptAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (appointment.DoctorId != userId) return Forbid();

        appointment.Status = "Approved";
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
