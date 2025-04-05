using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using HospitalAppointmentSystem.Models;

[Authorize]
public class PrescriptionsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PrescriptionsModel(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Prescription> Prescriptions { get; set; } = new();

    public List<ApplicationUser> Patients { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required] public string Medication { get; set; } = "";
        [Required] public string Dosage { get; set; } = "";
        public string Instructions { get; set; } = "";
        [Required] public DateTime IssueDate { get; set; } = DateTime.Today;
        public DateTime? ExpirationDate { get; set; }
        public string PatientId { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isDoctor = await _userManager.IsInRoleAsync(currentUser, "Doctor");
        var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

        if (isDoctor || isAdmin)
        {
            Patients = await _userManager.Users.Where(u => u.Role == "Patient").ToListAsync();
        }

        if (isAdmin)
        {
            Prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .OrderByDescending(p => p.IssueDate)
                .ToListAsync();
        }
        else if (isDoctor)
        {
            Prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Where(p => p.DoctorId == currentUser.Id)
                .OrderByDescending(p => p.IssueDate)
                .ToListAsync();
        }
        else
        {
            Prescriptions = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Where(p => p.PatientId == currentUser.Id)
                .OrderByDescending(p => p.IssueDate)
                .ToListAsync();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var doctor = await _userManager.GetUserAsync(User);
        if (!await _userManager.IsInRoleAsync(doctor, "Doctor"))
            return Forbid();

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        var prescription = new Prescription
        {
            Medication = Input.Medication,
            Dosage = Input.Dosage,
            Instructions = Input.Instructions,
            IssueDate = Input.IssueDate,
            ExpirationDate = Input.ExpirationDate,
            DoctorId = doctor.Id,
            PatientId = Input.PatientId
        };

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

        if (!isAdmin)
        {
            return Forbid();
        }

        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription == null)
        {
            return NotFound();
        }

        _context.Prescriptions.Remove(prescription);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSendToPharmacyAsync(int id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var isDoctor = await _userManager.IsInRoleAsync(currentUser, "Doctor");

        if (!isDoctor)
        {
            return Forbid();
        }

        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (prescription == null)
        {
            return NotFound();
        }

        // Code for sending email to pharmacy directly
        // await _emailService.SendEmailAsync("pharmacy@example.com", "New Prescription", $"Prescription details: {prescription}");

        return RedirectToPage();
    }
}