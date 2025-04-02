using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

[Authorize(Roles = "Patient,Admin")]
public class BookAppointmentModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;

    public BookAppointmentModel(AppDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    public List<DoctorViewModel> Doctors { get; set; } = new();
    public List<TimeSlot> AvailableTimeSlots { get; set; } = new();
    public string? Message { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string DoctorId { get; set; } = string.Empty;
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public DateTime TimeSlot { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var allUsers = await _context.Users.ToListAsync();
        Doctors = allUsers
            .Where(u => _userManager.IsInRoleAsync(u, "Doctor").Result)
            .Select(u => new DoctorViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Specialization = u.Specialization ?? "General"
            }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnGetLoadAvailableSlotsAsync(string doctorId, DateTime date)
    {
        if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            return new JsonResult(new List<TimeSlot>());

        var slots = new List<TimeSlot>();
        var start = date.Date.AddHours(9);
        var end = date.Date.AddHours(17);

        for (var t = start; t < end; t = t.AddMinutes(30))
        {
            if (t.Hour == 12) continue; // pause midi
            slots.Add(new TimeSlot { StartTime = t, EndTime = t.AddMinutes(30), Status = "Available" });
        }

        var doctorDaysOff = await _context.DoctorDaysOff
            .Where(d => d.DoctorId == doctorId && d.End > date.Date && d.Start < date.Date.AddDays(1))
            .ToListAsync();

        var appointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
            .ToListAsync();

        foreach (var slot in slots)
        {
            if (doctorDaysOff.Any(d => slot.StartTime < d.End && slot.EndTime > d.Start))
                slot.Status = "Unavailable";
            else if (appointments.Any(a => a.StartTime == slot.StartTime && a.Status == "Approved"))
                slot.Status = "Booked";
        }

        return new JsonResult(slots);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var patientId = _userManager.GetUserId(User);
        if (patientId == null) return Unauthorized();

        var startTime = Input.TimeSlot;
        var endTime = startTime.AddMinutes(30);

        var isTaken = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == Input.DoctorId &&
            a.Status == "Approved" &&
            a.StartTime < endTime && a.EndTime > startTime);

        var isDoctorOff = await _context.DoctorDaysOff
            .AnyAsync(d => d.DoctorId == Input.DoctorId && startTime < d.End && endTime > d.Start);

        if (isTaken || isDoctorOff)
        {
            ModelState.AddModelError("", "This time slot is already booked or the doctor is unavailable.");
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

        // ✅ Envoi en arrière-plan
        _ = Task.Run(async () =>
        {
            var patient = await _context.Users.FindAsync(patientId);
            var doctor = await _context.Users.FindAsync(Input.DoctorId);

            string html = $@"
            <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #eee; border-radius: 8px;'>
                <h2 style='color: #1b6ec2;'>Appointment Confirmation</h2>

                <p>Hello <strong>{patient?.FullName}</strong>,</p>

                <p style='line-height: 1.6;'>
                    We’re pleased to inform you that your appointment with 
                    <strong>Dr. {doctor?.FullName}</strong> has been <span style='color: green;'>successfully booked</span> for:
                </p>

                <p style='font-size: 17px; font-weight: bold; margin: 15px 0;'>
                    📅 {startTime:dddd dd MMM yyyy} at {startTime:HH:mm}
                </p>

                <p style='margin-top: 10px;'>
                    📍 <strong>Location:</strong> MedLife Hospital
                </p>

                <p style='margin-top: 20px;'>
                    You’ll find a calendar invite attached to easily add this appointment to your agenda.
                </p>

                <hr style='margin: 30px 0;' />

                <p style='color: #555;'>
                    ⏳ Once the appointment is <strong>validated by your doctor</strong>, you’ll receive another confirmation email.
                </p>

                <p style='margin-top: 30px; font-size: 14px; color: #888;'>
                    If you have any questions, feel free to reach out to our team at 
                    <a href='mailto:support@medlife.com'>support@medlife.com</a>.
                </p>

                <p style='margin-top: 40px; font-size: 13px; color: #aaa;'>
                    — The MedLife Hospital Team 🏥
                </p>
            </div>";


            string ics = GenerateIcs(appointment, $"Appointment with Dr. {doctor?.FullName}", "MedLife Hospital");

            if (patient?.Email != null)
                await _emailService.SendWithAttachmentAsync(patient.Email, "📅 Appointment Confirmation", html, ics, "appointment.ics");
            // notification to doctor we could create a new html template but not necessary
            if (doctor?.Email != null)
                await _emailService.SendWithAttachmentAsync(doctor.Email, "👨‍⚕️ New Appointment Scheduled", html, ics, "appointment.ics");
        });
        

        Message = "✅ Your appointment has been booked successfully! You will receive an email confirmation if enabled, and another one once approved.";
        return Page();
    }

    private string GenerateIcs(Appointment appt, string summary, string location)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("BEGIN:VEVENT");
        sb.AppendLine($"UID:{Guid.NewGuid()}");
        sb.AppendLine($"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTSTART:{appt.StartTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"DTEND:{appt.EndTime.ToUniversalTime():yyyyMMddTHHmmssZ}");
        sb.AppendLine($"SUMMARY:{summary}");
        sb.AppendLine($"LOCATION:{location}");
        sb.AppendLine("DESCRIPTION:Appointment scheduled at MedLife Hospital");
        sb.AppendLine("END:VEVENT");
        sb.AppendLine("END:VCALENDAR");
        return sb.ToString();
    }
}

public class TimeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Available";
}

public class DoctorViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = "General";
}
