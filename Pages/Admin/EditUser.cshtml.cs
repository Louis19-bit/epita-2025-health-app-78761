using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

public class EditUserModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public EditUserModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Role { get; set; }
        public string Specialization { get; set; } // Only for Doctors
        public string NewPassword { get; set; } // Optional: Only change if provided
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        Input = new InputModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Specialization = user.Specialization
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user == null) return NotFound();

        user.FullName = Input.FullName;
        user.Role = Input.Role;
        user.Specialization = Input.Role == "Doctor" ? Input.Specialization : null; // Set to NULL for non-doctors

        // ✅ Ensure password change only if provided
        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }
        }

        await _userManager.UpdateAsync(user);
        return RedirectToPage("ManageUsers");
    }
}