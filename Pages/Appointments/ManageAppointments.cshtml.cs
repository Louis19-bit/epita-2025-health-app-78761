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

    public class AppointmentViewModel
    {
        public int Id { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        IsDoctor = await _userManager.IsInRoleAsync(await _userManager.FindByIdAsync(userId), "Doctor");
        var now = DateTime.Now;

        if (IsDoctor)
        {
            // 🩺 Docteur : Ses rendez-vous
            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == userId)
                .Include(a => a.Patient)
                .Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    PatientName = a.Patient.FullName,
                    DoctorName = "Me",
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = a.Status
                })
                .ToListAsync();

            UpcomingAppointments = appointments.Where(a => a.StartTime >= now).ToList();
            PastAppointments = appointments.Where(a => a.StartTime < now).ToList();
        }
        else
        {
            // 👤 Patient : Ses propres rendez-vous
            var appointments = await _context.Appointments
                .Where(a => a.PatientId == userId)
                .Include(a => a.Doctor)
                .Select(a => new AppointmentViewModel
                {
                    Id = a.Id,
                    PatientName = "Me",
                    DoctorName = a.Doctor.FullName,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Status = a.Status
                })
                .ToListAsync();

            UpcomingAppointments = appointments.Where(a => a.StartTime >= now).ToList();
            PastAppointments = appointments.Where(a => a.StartTime < now).ToList();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (appointment.PatientId != userId && appointment.DoctorId != userId)
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
