using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentSystem.Pages.Feedback;

[Authorize(Roles = "Patient")]
public class AddFeedbackModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AddFeedbackModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public FeedbackInput Input { get; set; } = new();

    public string DoctorName { get; set; } = "";

    public class FeedbackInput
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public int AppointmentId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int appointmentId)
    {
        var user = await _userManager.GetUserAsync(User);
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == user.Id);

        if (appointment == null || (appointment.Status != "Completed" && appointment.Status != "Approved"))
            return NotFound();

        DoctorName = appointment.Doctor.FullName;

        var already = await _context.Feedbacks.AnyAsync(f => f.AppointmentId == appointmentId);
        if (already)
            return RedirectToPage("/Feedback/AlreadySubmitted");

        Input.AppointmentId = appointmentId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == Input.AppointmentId && a.PatientId == user.Id);

        if (appointment == null || (appointment.Status != "Completed" && appointment.Status != "Approved"))
            return NotFound();

        var already = await _context.Feedbacks.AnyAsync(f => f.AppointmentId == appointment.Id);
        if (already) return RedirectToPage("/Feedback/AlreadySubmitted");

        _context.Feedbacks.Add(new global::Feedback
        {
            Rating = Input.Rating,
            Comment = Input.Comment,
            PatientId = user.Id,
            DoctorId = appointment.DoctorId,
            AppointmentId = appointment.Id
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your feedback has been submitted successfully.";
        //return RedirectToPage("/ManageAppointments");
        return RedirectToPage("/Feedback/Thanks");
    }
}
