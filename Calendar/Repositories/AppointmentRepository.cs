using Calendar.Data;
using Calendar.DTOs.Appointment;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Appointment>> GetByUserIdAsync(string userId, DateOnly fromDate, DateOnly toDate)
    {
        var userGuid = Guid.Parse(userId);
        return await _context.Appointments
            .Where(a => a.OrganizerId == userGuid &&
                        a.AppointmentDate >= fromDate &&
                        a.AppointmentDate <= toDate)
            .ToListAsync();
    }

    public async Task AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByIdAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UpdateAsync(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task<Appointment> GetByIdAsync(Guid appointmentId)
    {
        return await _context.Appointments.FindAsync(appointmentId);
    }
}