using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

[Authorize]
public class MedicalRecordsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public MedicalRecordsModel(AppDbContext context, UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
    }

    public List<MedicalRecord> Records { get; set; } = new();
    public List<ApplicationUser> AllPatients { get; set; } = new();
    public ApplicationUser? SelectedPatient { get; set; }
    public bool AIDisabled { get; set; } = false;

    [BindProperty]
    public string? ChatInput { get; set; }

    public List<string> ChatHistory { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? patientId)
    {
        var currentUser = await _userManager.GetUserAsync(User);

        if (User.IsInRole("Doctor"))
        {
            AllPatients = await _userManager.Users
                .Where(u => u.Role == "Patient")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            if (!string.IsNullOrEmpty(patientId))
            {
                SelectedPatient = await _userManager.FindByIdAsync(patientId);
                if (SelectedPatient != null)
                {
                    Records = await _context.MedicalRecords
                        .Where(r => r.PatientId == patientId)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();
                }
            }
        }
        else
        {
            Records = await _context.MedicalRecords
                .Where(r => r.PatientId == currentUser.Id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();
        }

        ChatHistory = HttpContext.Session.GetString("ChatHistory")?.Split("|||").ToList() ?? new();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string PatientId, string Title, string Description, DateTime Date)
    {
        if (!User.IsInRole("Doctor")) return Forbid();

        _context.MedicalRecords.Add(new MedicalRecord
        {
            PatientId = PatientId,
            Title = Title,
            Description = Description,
            Date = Date
        });

        await _context.SaveChangesAsync();
        return RedirectToPage(new { patientId = PatientId });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var record = await _context.MedicalRecords.FindAsync(id);
        if (record == null) return NotFound();
        if (!User.IsInRole("Doctor")) return Forbid();

        _context.MedicalRecords.Remove(record);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { patientId = record.PatientId });
    }

    public async Task<IActionResult> OnPostChatAsync(string patientId)
    {
        var records = await _context.MedicalRecords
            .Where(r => r.PatientId == patientId)
            .OrderBy(r => r.Date)
            .ToListAsync();

        var history = HttpContext.Session.GetString("ChatHistory")?.Split("|||").ToList() ?? new();
        history.Add($"👨‍⚕️ {ChatInput}");

        var prompt = new StringBuilder();
        prompt.AppendLine("Patient Medical History:");
        foreach (var r in records)
            prompt.AppendLine($"- {r.Date:dd MMM yyyy} – {r.Title}: {r.Description}");

        prompt.AppendLine("\nConversation:");
        foreach (var msg in history)
            prompt.AppendLine(msg);

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate", new
            {
                model = "qwen2.5:0.5b",
                prompt = prompt.ToString(),
                stream = false
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var reply = json.GetProperty("response").GetString();
                history.Add($"🤖 {reply}");
                HttpContext.Session.SetString("ChatHistory", string.Join("|||", history));
            }
            else AIDisabled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ollama error: " + ex.Message);
            AIDisabled = true;
        }

        return RedirectToPage(new { patientId });
    }

    public IActionResult OnPostResetChat(string patientId)
    {
        HttpContext.Session.Remove("ChatHistory");
        return RedirectToPage(new { patientId });
    }
}