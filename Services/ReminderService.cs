using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class ReminderService
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;

    public ReminderService(AppDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task SendAppointmentRemindersAsync()
    {
        var now = DateTime.Now;
        var targetTime = now.AddHours(24);

        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a =>
                a.Status == "Approved" &&
                a.ReminderSent == false &&
                a.StartTime > now &&
                a.StartTime <= targetTime)
            .ToListAsync();

        foreach (var appointment in appointments)
        {
            var subject = "⏰ Appointment Reminder - 24h Notice";
            var body = $@"
Hello {appointment.Patient.FullName},<br><br>
This is a reminder for your appointment scheduled in 24 hours:<br>
<strong>Date:</strong> {appointment.StartTime:dddd, MMM dd yyyy HH:mm}<br>
<strong>Doctor:</strong> Dr. {appointment.Doctor.FullName}<br><br>
Thank you,<br>
The Hospital Team.";

            try
            {
                await _emailService.SendAsync(appointment.Patient.Email, subject, body);
                appointment.ReminderSent = true;
            }
            catch
            {
                // Optionnel : log l'erreur
            }
        }

        await _context.SaveChangesAsync();
    }
}