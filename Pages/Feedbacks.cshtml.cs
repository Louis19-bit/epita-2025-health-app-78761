using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentSystem.Pages;

[Authorize(Roles = "Admin,Doctor")]
public class FeedbacksModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FeedbacksModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<FeedbackViewModel> Feedbacks { get; set; } = new();

    public class FeedbackViewModel
    {
        public string PatientName { get; set; } = "";
        public string DoctorName { get; set; } = "";
        public DateTime Date { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");

        var query = _context.Feedbacks
            .Include(f => f.Doctor)
            .Include(f => f.Patient)
            .Include(f => f.Appointment)
            .AsQueryable();

        if (isDoctor)
        {
            query = query.Where(f => f.DoctorId == user.Id);
        }

        Feedbacks = await query.Select(f => new FeedbackViewModel
        {
            PatientName = f.Patient.FullName,
            DoctorName = f.Doctor.FullName,
            Date = f.Appointment.StartTime,
            Rating = f.Rating,
            Comment = f.Comment
        }).OrderByDescending(f => f.Date).ToListAsync();
    }
}