using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AppointmentService
{
    private readonly AppDbContext _context;

    public AppointmentService(AppDbContext context)
    {
        _context = context;
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

        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
            .Select(a => new { a.StartTime, a.Status })
            .ToListAsync();

        foreach (var slot in allSlots)
        {
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
        return true;
    }
}

public class TimeSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; }
}
