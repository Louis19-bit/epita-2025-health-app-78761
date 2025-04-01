using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class EditUserModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EditUserModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<string> AllRoles { get; set; } = new();

    public class InputModel
    {
        public string Id { get; set; } = "";
        [Required]
        public string FullName { get; set; } = "";
        [Required]
        public string Email { get; set; } = "";
        [Required]
        public string Role { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string? NewPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        Input = new InputModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = userRoles.FirstOrDefault() ?? "Patient", // fallback
            Specialization = user.Specialization
        };

        AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(Input.Id);
        if (user == null) return NotFound();

        AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(Input.Role))
        {
            var remove = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!remove.Succeeded)
            {
                foreach (var error in remove.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            var add = await _userManager.AddToRoleAsync(user, Input.Role);
            if (!add.Succeeded)
            {
                foreach (var error in add.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }
        }

        user.FullName = Input.FullName;
        user.Specialization = Input.Role == "Doctor" ? Input.Specialization : null;
        user.Role = Input.Role;

        if (!string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }
        }

        await _userManager.UpdateAsync(user);
        return RedirectToPage("ManageUsers");
    }
}
