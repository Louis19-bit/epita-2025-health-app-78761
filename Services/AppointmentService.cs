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

        // 🔍 Récupération des jours off du médecin
        var doctorDaysOff = await _context.DoctorDaysOff
            .Where(d => d.DoctorId == doctorId && d.Start.Date <= date.Date && d.End.Date >= date.Date)
            .ToListAsync();

        // 🔍 Récupération des rendez-vous existants
        var existingAppointments = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartTime.Date == date.Date)
            .Select(a => new { a.StartTime, a.Status })
            .ToListAsync();

        foreach (var slot in allSlots)
        {
            // ❌ S'il y a un jour off qui couvre ce créneau, on le bloque
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
        // ❌ Empêche de réserver si jour off
        var isDoctorOff = await IsDoctorOff(appointment.DoctorId, appointment.StartTime, appointment.EndTime);
        if (isDoctorOff)
        {
            return false;
        }

        // ❌ Empêche de réserver si conflit avec un autre rendez-vous approuvé
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

    // ✅ Vérifie si un créneau tombe pendant un jour off
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
