// File: Pages/Prescriptions/ExportPrescriptions.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.IO;
using HospitalAppointmentSystem.Models;
using iText.Bouncycastleconnector;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

[Authorize(Roles = "Doctor,Admin")]
public class ExportPrescriptionsModel : PageModel
{
    private readonly AppDbContext _context;

    public ExportPrescriptionsModel(AppDbContext context)
    {
        _context = context;
    }

    public List<ApplicationUser> Patients { get; set; } = new();

    // Propriété pour afficher dynamiquement les prescriptions filtrées
    public List<Prescription> FilteredPrescriptions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? PatientId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndDate { get; set; }

    public async Task OnGetAsync()
    {
        Patients = await _context.Users
            .Where(u => u.Role == "Patient")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        FilteredPrescriptions = await GetFilteredPrescriptionsAsync();
    }

    private async Task<List<Prescription>> GetFilteredPrescriptionsAsync()
    {
        var query = _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .AsQueryable();

        if (!string.IsNullOrEmpty(PatientId))
            query = query.Where(p => p.PatientId == PatientId);

        if (StartDate.HasValue)
            query = query.Where(p => p.IssueDate >= StartDate.Value);

        if (EndDate.HasValue)
            query = query.Where(p => p.IssueDate <= EndDate.Value);

        return await query.OrderByDescending(p => p.IssueDate).ToListAsync();
    }

    public async Task<IActionResult> OnGetDownloadCsvAsync()
    {
        var prescriptions = await GetFilteredPrescriptionsAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Patient,Doctor,Medication,Dosage,Instructions,IssueDate,Status");

        foreach (var p in prescriptions)
        {
            sb.AppendLine($"\"{p.Patient.FullName}\",\"{p.Doctor.FullName}\",\"{p.Medication}\",\"{p.Dosage}\",\"{p.Instructions}\",\"{p.IssueDate:yyyy-MM-dd}\",\"{p.Status}\"");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "prescriptions.csv");
    }

    public async Task<IActionResult> OnGetDownloadPdfAsync()
    {
        var prescriptions = await GetFilteredPrescriptionsAsync();

        using var stream = new MemoryStream();
        var writer = new PdfWriter(stream, new WriterProperties().SetCompressionLevel(CompressionConstants.BEST_COMPRESSION));
        var pdf = new PdfDocument(writer);
        var doc = new Document(pdf);

        doc.Add(new Paragraph("Prescription Export").SetFontSize(16));
        doc.Add(new Paragraph($"Exported at: {DateTime.Now:yyyy-MM-dd HH:mm}").SetFontSize(10));

        foreach (var p in prescriptions)
        {
            doc.Add(new Paragraph($"Patient: {p.Patient.FullName}"));
            doc.Add(new Paragraph($"Doctor: {p.Doctor.FullName}"));
            doc.Add(new Paragraph($"Medication: {p.Medication}"));
            doc.Add(new Paragraph($"Dosage: {p.Dosage}"));
            doc.Add(new Paragraph($"Instructions: {p.Instructions}"));
            doc.Add(new Paragraph($"Date: {p.IssueDate:yyyy-MM-dd} – Status: {p.Status}"));
            doc.Add(new Paragraph("--------------------------"));
        }

        doc.Close();
        return File(stream.ToArray(), "application/pdf", "prescriptions.pdf");
    }
}