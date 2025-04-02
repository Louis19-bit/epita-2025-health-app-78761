using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

[Authorize(Roles = "Admin")]
public class SendEmailModel : PageModel
{
    private readonly EmailService _emailService;

    public SendEmailModel(EmailService emailService)
    {
        _emailService = emailService;
    }

    [BindProperty]
    public EmailInput Input { get; set; } = new();

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public class EmailInput
    {
        [Required, EmailAddress]
        public string To { get; set; } = "";

        [Required]
        public string Subject { get; set; } = "";

        [Required]
        public string Body { get; set; } = "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        try
        {
            await _emailService.SendAsync(Input.To, Input.Subject, Input.Body);
            SuccessMessage = "✅ Email sent successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"❌ Failed to send email: {ex.Message}";
        }

        return Page();
    }
}