using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AppointmentService
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;

    public AppointmentService(AppDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<List<TimeSlotDto>> GetAvailableTimeSlots(string doctorId, DateTime date)
    {
        var startOfDay = date.Date.AddHours(9);  // Horaires : 9h - 17h
        var endOfDay = date.Date.AddHours(17);
        var allSlots = new List<TimeSlotDto>();

        for (var time = startOfDay; time < endOfDay; time = time.AddMinutes(30))
        {
            allSlots.Add(new TimeSlotDto
            {
                StartTime = time,
                EndTime = time.AddMinutes(30),
                Status = "Available"
            });
        }

        var doctorDaysOff = await _context.DoctorDaysOff
            .Where(d => d.DoctorId == doctorId && d.Start.Date <= date.Date && d.End.Date >= date.Date)
            .ToListAsync();

        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
            .Select(a => new { a.StartTime, a.Status })
            .ToListAsync();

        foreach (var slot in allSlots)
        {
            bool isOff = doctorDaysOff.Any(off => slot.StartTime < off.End && slot.EndTime > off.Start);
            if (isOff)
            {
                slot.Status = "Unavailable";
                continue;
            }

            var existing = existingAppointments.FirstOrDefault(a => a.StartTime == slot.StartTime);
            if (existing != null)
            {
                slot.Status = existing.Status == "Approved" ? "Booked" : "Pending";
            }
        }

        return allSlots;
    }

    public async Task<bool> BookAppointment(Appointment appointment)
    {
        var isDoctorOff = await IsDoctorOff(appointment.DoctorId, appointment.StartTime, appointment.EndTime);
        if (isDoctorOff)
        {
            return false;
        }

        var isSlotTaken = await _context.Appointments
            .AnyAsync(a => a.DoctorId == appointment.DoctorId &&
                           a.StartTime < appointment.EndTime &&
                           a.EndTime > appointment.StartTime &&
                           a.Status == "Approved");

        if (isSlotTaken)
        {
            return false;
        }

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        /*
        // ✅ Email de confirmation
        var patient = await _context.Users.FindAsync(appointment.PatientId);
        var doctor = await _context.Users.FindAsync(appointment.DoctorId);

        if (patient != null && patient.ReceiveEmailNotifications)
        {
            await _emailService.SendAsync(patient.Email, "📅 Appointment Confirmed",
                $@"Hello {patient.FullName},<br><br>
                Your appointment with Dr. {doctor?.FullName} on <strong>{appointment.StartTime:dddd dd MMM yyyy HH:mm}</strong> has been successfully booked.<br><br>
                Regards,<br><strong>MedLife Hospital</strong>");
        }

        if (doctor != null && doctor.ReceiveEmailNotifications)
        {
            await _emailService.SendAsync(doctor.Email, "👨‍⚕️ New Appointment Scheduled",
                $@"Dear Dr. {doctor.FullName},<br><br>
                You have a new appointment with <strong>{patient?.FullName}</strong> scheduled on <strong>{appointment.StartTime:dddd dd MMM yyyy HH:mm}</strong>.<br><br>
                Please log in to your dashboard for more details.<br><br>
                Regards,<br><strong>MedLife Hospital</strong>");
        }
        */

        return true;
    }

    public async Task<bool> IsDoctorOff(string doctorId, DateTime start, DateTime end)
    {
        return await _context.DoctorDaysOff
            .AnyAsync(off => off.DoctorId == doctorId &&
                             start < off.End &&
                             end > off.Start);
    }
}

public class TimeSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; }
}
