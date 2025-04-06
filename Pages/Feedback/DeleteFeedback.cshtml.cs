using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentSystem.Pages.Feedback;

[Authorize(Roles = "Patient")]
public class DeleteFeedbackModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteFeedbackModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public int AppointmentId { get; set; }

    public string DoctorName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int appointmentId)
    {
        var user = await _userManager.GetUserAsync(User);
        var feedback = await _context.Feedbacks
            .Include(f => f.Appointment)
            .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(f => f.AppointmentId == appointmentId && f.PatientId == user.Id);

        if (feedback == null) return NotFound();

        DoctorName = feedback.Appointment.Doctor.FullName;
        AppointmentId = appointmentId;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var feedback = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.AppointmentId == AppointmentId && f.PatientId == user.Id);

        if (feedback == null) return NotFound();

        _context.Feedbacks.Remove(feedback);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your feedback has been deleted.";
        return RedirectToPage("/Appointments/ManageAppointments");
    }
}