using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

//[Authorize(Roles = "Admin")]
public class SendRemindersModel : PageModel
{
    private readonly ReminderService _reminderService;

    public SendRemindersModel(ReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    public string? Message { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        await _reminderService.SendAppointmentRemindersAsync();
        Message = "📧 Reminder emails sent for appointments in 24h.";
        return Page();
    }
}