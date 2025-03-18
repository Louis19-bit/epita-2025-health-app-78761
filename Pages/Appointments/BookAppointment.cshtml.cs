using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

public class BookAppointmentModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BookAppointmentModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<DoctorViewModel> Doctors { get; set; } = new();
    public List<TimeSlot> AvailableTimeSlots { get; set; } = new(); // Liste des créneaux disponibles
    public string? Message { get; set; } // ✅ Correction : Définition de Message

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime TimeSlot { get; set; } // ✅ Correction : Ajout de TimeSlot
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var allUsers = await _context.Users.ToListAsync();
        var doctorList = new List<DoctorViewModel>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Doctor"))
            {
                doctorList.Add(new DoctorViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Specialization = user.Specialization ?? "General"
                });
            }
        }

        Doctors = doctorList;
        return Page();
    }

    public async Task<IActionResult> OnGetLoadAvailableSlotsAsync(string doctorId, DateTime date)
    {
        var startOfDay = date.Date.AddHours(9);
        var endOfDay = date.Date.AddHours(17);
        var allSlots = new List<TimeSlot>();

        for (var time = startOfDay; time < endOfDay; time = time.AddMinutes(30))
        {
            allSlots.Add(new TimeSlot { StartTime = time, EndTime = time.AddMinutes(30), Status = "Available" });
        }

        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
            .Select(a => new { a.StartTime, a.Status })
            .ToListAsync();

        var slotsWithStatus = allSlots.Select(slot =>
        {
            var existing = existingAppointments.FirstOrDefault(a => a.StartTime == slot.StartTime);
            if (existing != null)
            {
                slot.Status = existing.Status == "Approved" ? "Booked" : "Pending";
            }
            return slot;
        }).ToList();

        return new JsonResult(slotsWithStatus);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var patientId = _userManager.GetUserId(User);
        if (patientId == null) return Unauthorized();

        var startTime = Input.TimeSlot;
        var endTime = startTime.AddMinutes(30);

        bool isAvailable = !_context.Appointments
            .Any(a => a.DoctorId == Input.DoctorId &&
                      a.Status == "Approved" &&
                      ((startTime >= a.StartTime && startTime < a.EndTime) ||
                       (endTime > a.StartTime && endTime <= a.EndTime) ||
                       (startTime <= a.StartTime && endTime >= a.EndTime)));

        if (!isAvailable)
        {
            ModelState.AddModelError("", "This time slot is already booked.");
            return Page();
        }

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = Input.DoctorId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "Pending"
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        Message = "Appointment booked successfully!";
        return Page();
    }
}

// ✅ Correction : Ajout de la classe `TimeSlot`
public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Available";
}

// ✅ Correction : Ajout de `DoctorViewModel`
public class DoctorViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = "General";
}