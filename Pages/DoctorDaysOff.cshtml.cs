using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Authorize(Roles = "Doctor,Admin")]
public class DoctorDaysOffModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DoctorDaysOffModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<ApplicationUser> Doctors { get; set; } = new();
    public List<DoctorDayOff> ExistingDaysOff { get; set; } = new();
    public string? Message { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public DateTime Start { get; set; }

        [Required]
        public DateTime End { get; set; }

        public string? DoctorId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (User.IsInRole("Admin"))
        {
            Doctors = await _userManager.Users.Where(u => u.Role == "Doctor").ToListAsync();
            ExistingDaysOff = await _context.DoctorDaysOff.Include(d => d.Doctor).ToListAsync();
        }
        else
        {
            ExistingDaysOff = await _context.DoctorDaysOff
                .Where(d => d.DoctorId == userId)
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        string doctorId = User.IsInRole("Admin") ? Input.DoctorId! : userId!;

        if (Input.Start < DateTime.Now)
        {
            ModelState.AddModelError(string.Empty, "You cannot add a day off in the past.");
            if (User.IsInRole("Admin"))
                Doctors = await _userManager.Users.Where(u => u.Role == "Doctor").ToListAsync();
            ExistingDaysOff = await _context.DoctorDaysOff.Include(d => d.Doctor).ToListAsync();
            return Page();
        }

        var overlaps = await _context.DoctorDaysOff
            .Where(d => d.DoctorId == doctorId && Input.Start < d.End && Input.End > d.Start)
            .AnyAsync();

        if (overlaps)
        {
            ModelState.AddModelError(string.Empty, "The selected time range overlaps with an existing day off.");
            if (User.IsInRole("Admin"))
                Doctors = await _userManager.Users.Where(u => u.Role == "Doctor").ToListAsync();

            ExistingDaysOff = await _context.DoctorDaysOff.Include(d => d.Doctor).ToListAsync();
            return Page();
        }

        var newDayOff = new DoctorDayOff
        {
            DoctorId = doctorId,
            Start = Input.Start,
            End = Input.End
        };

        _context.DoctorDaysOff.Add(newDayOff);
        await _context.SaveChangesAsync();
        Message = "Day off saved successfully!";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var off = await _context.DoctorDaysOff.FindAsync(id);
        if (off == null) return NotFound();

        _context.DoctorDaysOff.Remove(off);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}
