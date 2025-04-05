using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Markdig;

[Authorize]
public class MedicalRecordsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public MedicalRecordsModel(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
        var now = DateTime.Now.ToString("HH:mm");
        history.Add($"<div class='chat-bubble doctor'><strong class='you-label'>👨‍⚕️ You:</strong>{ChatInput}<div class='chat-time'>{now}</div></div>");

        var prompt = new StringBuilder();
        prompt.AppendLine("You are a helpful and expert medical assistant. You are here to assist the doctor in providing medical advice.");
        prompt.AppendLine("You have read all Pubmed and Vidal. You NEED to answer the question in a very concise and precise way.");
        prompt.AppendLine("⚠️ You MUST answer in English ONLY.");
        prompt.AppendLine("Here is the medical history of the patient:");
        prompt.AppendLine($"- Patient: {records.FirstOrDefault()?.Patient?.FullName}");
        foreach (var r in records)
            prompt.AppendLine($"- {r.Date:dd MMM yyyy} – {r.Title}: {r.Description}");
        prompt.AppendLine("\nConversation so far:");
        foreach (var msg in history)
            prompt.AppendLine(Markdig.Markdown.ToPlainText(msg));

        try
        {
            var modelName = _configuration["AISettings:Model"] ?? "qwen2.5:0.5b";
            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate", new
            {
                model = modelName,
                prompt = prompt.ToString(),
                stream = false
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                var reply = json.GetProperty("response").GetString();
                var html = Markdig.Markdown.ToHtml(reply ?? "");
                history.Add($"<div class='chat-bubble ai'><strong>🤖 AI:</strong><br>{html}<div class='chat-time'>{now}</div></div>");
                HttpContext.Session.SetString("ChatHistory", string.Join("|||", history));
            }
            else
            {
                AIDisabled = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ollama error: " + ex.Message);
            AIDisabled = true;
        }

        return Redirect("/MedicalRecords?patientId=" + patientId + "#chat");
    }

    public IActionResult OnPostResetChat(string patientId)
    {
        HttpContext.Session.Remove("ChatHistory");
        return RedirectToPage(new { patientId });
    }
}
