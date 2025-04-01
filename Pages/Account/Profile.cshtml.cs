using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;

    public ProfileModel(UserManager<ApplicationUser> userManager,
                        SignInManager<ApplicationUser> signInManager,
                        AppDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    public List<string> MedicalHistory { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Id { get; set; } = "";
        [Required]
        public string FullName { get; set; } = "";
        [EmailAddress]
        public string Email { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public bool ReceiveEmailNotifications { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        Input = new InputModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ReceiveEmailNotifications = user.ReceiveEmailNotifications
        };

        if (User.IsInRole("Patient"))
        {
            MedicalHistory = await GetMedicalSummary(user.Id);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user == null) return NotFound();

        user.FullName = Input.FullName;
        user.ReceiveEmailNotifications = Input.ReceiveEmailNotifications;

        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }
        }

        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        await _userManager.DeleteAsync(user);
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }

    private async Task<List<string>> GetMedicalSummary(string userId)
    {
        return await _context.MedicalRecords
            .Where(r => r.PatientId == userId)
            .OrderByDescending(r => r.Date)
            .Select(r => $"{r.Date:dd MMM yyyy} – {r.Title}: {r.Description}")
            .ToListAsync();
    }
}
