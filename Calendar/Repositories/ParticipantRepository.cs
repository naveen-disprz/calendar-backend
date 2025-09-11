using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;


namespace Calendar.Repositories;

public class ParticipantRepository : IParticipantRepository
{
    private readonly AppDbContext _context;

    public ParticipantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetByAppointmentIdAsync(Guid appointmentId)
    {
        return await _context.Participants
            .Where(p => p.AppointmentId == appointmentId)
            .Select(p => p.User)
            .ToListAsync();
    }
    
    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsByIdAsync(string userId)
    {
        return await _context.Participants.Where(p => p.UserId.ToString() == userId)
            .Include(p => p.Appointment)
            .Select(p => p.Appointment)
            .ToListAsync();
    }
    
    public async Task BulkAddAsync(List<Participant> participants)
    {
        await _context.Participants.AddRangeAsync(participants);
        await _context.SaveChangesAsync();
    }

    public async Task BulkRemoveAsync(Guid appointmentId, List<Guid> userIds)
    {
        var participantsToRemove = await _context.Participants
            .Where(p => p.AppointmentId == appointmentId && userIds.Contains(p.UserId))
            .ToListAsync();

        _context.Participants.RemoveRange(participantsToRemove);
        await _context.SaveChangesAsync();
    }
}