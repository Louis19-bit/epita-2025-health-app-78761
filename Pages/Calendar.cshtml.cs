using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class CalendarModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<ApplicationUser> Doctors { get; set; } = new();
    public List<string> Specializations { get; set; } = new();
    public string? SelectedDoctorId { get; set; }
    public string? SelectedStatus { get; set; }
    public string? SelectedSpecialization { get; set; }

    public async Task<IActionResult> OnGetAsync(string? doctorId, string? specialization, string? status)
    {
        Doctors = await _context.Users
            .Where(u => u.Role == "Doctor")
            .OrderBy(d => d.FullName)
            .ToListAsync();

        Specializations = Doctors
            .Where(d => !string.IsNullOrEmpty(d.Specialization))
            .Select(d => d.Specialization!)
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        SelectedDoctorId = doctorId;
        SelectedStatus = status;
        SelectedSpecialization = specialization;

        return Page();
    }

    public async Task<IActionResult> OnGetEventsAsync(string? doctorId, string? specialization, string? status)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = User.IsInRole("Admin");
            var isDoctor = User.IsInRole("Doctor");
            var isPatient = User.IsInRole("Patient");

            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsQueryable();

            // 🔒 Restriction des rendez-vous visibles selon le rôle
            if (!isAdmin)
            {
                if (isDoctor)
                {
                    query = query.Where(a => a.DoctorId == user.Id);
                }
                else if (isPatient)
                {
                    query = query.Where(a => a.PatientId == user.Id);
                }
            }
            else if (!string.IsNullOrEmpty(doctorId))
            {
                query = query.Where(a => a.DoctorId == doctorId);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(specialization))
                query = query.Where(a => a.Doctor.Specialization == specialization);

            // ✅ Génération des événements avec title & props sûrs
            var appointmentEvents = await query.Select(a => new
            {
                id = a.Id,
                title =
                    isAdmin ? $"🧑 {a.Patient.FullName} – {a.Status}" :
                    isDoctor ? $"Appointment with {a.Patient.FullName}" :
                    $"Appointment with Dr. {a.Doctor.FullName}",
                start = a.StartTime,
                end = a.EndTime,
                color = a.Status == "Approved" ? "#198754" :
                        a.Status == "Pending" ? "#ffc107" :
                        a.Status == "Rejected" ? "#dc3545" : "#6c757d",
                extendedProps = new
                {
                    doctor = a.Doctor.FullName ?? "Unknown",
                    patient = a.Patient.FullName ?? "Unknown",
                    specialization = a.Doctor.Specialization ?? "N/A",
                    status = a.Status
                }
            }).ToListAsync();

            // ✅ Ajouter Lunch Breaks uniquement pour les docteurs
            var lunchBreaks = new List<object>();
            if (isDoctor)
            {
                for (int i = -365; i <= 365; i++)
                {
                    var date = DateTime.Today.AddDays(i);
                    lunchBreaks.Add(new
                    {
                        start = date.AddHours(12),
                        end = date.AddHours(13),
                        title = "Lunch Break",
                        color = "#ffcc00",
                        display = "background"
                    });
                }
            }

            var events = appointmentEvents.Concat(lunchBreaks);
            return new JsonResult(events);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching events: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", details = ex.Message });
        }
    }

    public async Task<IActionResult> OnGetDoctorDetailsAsync(string doctorId)
    {
        if (string.IsNullOrEmpty(doctorId))
            return BadRequest("Doctor ID is required.");

        var daysOff = await _context.DoctorDaysOff
            .Where(d => d.DoctorId == doctorId)
            .Select(d => new
            {
                start = d.Start,
                end = d.End,
                title = "Day Off",
                color = "#e0e0e0",
                display = "background"
            })
            .ToListAsync();

        return new JsonResult(daysOff);
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
