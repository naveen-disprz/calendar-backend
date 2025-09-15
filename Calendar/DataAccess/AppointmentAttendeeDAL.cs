using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.DataAccess
{
    public class AppointmentAttendeeDAL : IAppointmentAttendeeDAL
    {
        private readonly AppDbContext _context;

        public AppointmentAttendeeDAL(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppointmentAttendee>> CreateAttendeesAsync(List<AppointmentAttendee> attendees)
        {
            if (!attendees.Any())
                return new List<AppointmentAttendee>();

            await _context.AppointmentAttendees.AddRangeAsync(attendees);
            await _context.SaveChangesAsync();

            // Return with loaded navigation properties
            var appointmentId = attendees.First().AppointmentId;
            return await GetAttendeesByAppointmentIdAsync(appointmentId);
        }

        public async Task<AppointmentAttendee> CreateAttendeeAsync(AppointmentAttendee attendee)
        {
            await _context.AppointmentAttendees.AddAsync(attendee);
            await _context.SaveChangesAsync();

            // Return with loaded navigation properties
            return await _context.AppointmentAttendees
                .Include(aa => aa.User)
                .Include(aa => aa.Appointment)
                .FirstAsync(aa => aa.Id == attendee.Id);
        }

        public async Task<List<AppointmentAttendee>> GetAttendeesByAppointmentIdAsync(Guid appointmentId)
        {
            return await _context.AppointmentAttendees
                .Include(aa => aa.User)
                .Include(aa => aa.Appointment)
                .Where(aa => aa.AppointmentId == appointmentId && !aa.IsOrganizer)
                .OrderBy(aa => aa.IsOrganizer ? 0 : 1) // Organizer first
                .ThenBy(aa => aa.User.FirstName)
                .ToListAsync();
        }

        public async Task<List<AppointmentAttendee>> GetAttendeesByUserIdAsync(Guid userId)
        {
            return await _context.AppointmentAttendees
                .Include(aa => aa.User)
                .Include(aa => aa.Appointment)
                .ThenInclude(a => a.Organizer)
                .Where(aa => aa.UserId == userId && !aa.Appointment.IsDeleted)
                .OrderByDescending(aa => aa.Appointment.StartDateTime)
                .ToListAsync();
        }

        public async Task<AppointmentAttendee?> GetAttendeeAsync(Guid appointmentId, Guid userId)
        {
            return await _context.AppointmentAttendees
                .Include(aa => aa.User)
                .Include(aa => aa.Appointment)
                .FirstOrDefaultAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);
        }

        public async Task<bool> IsOrganizerAsync(Guid appointmentId, Guid userId)
        {
            return await _context.AppointmentAttendees
                .AnyAsync(aa => aa.AppointmentId == appointmentId && 
                               aa.UserId == userId && 
                               aa.IsOrganizer);
        }

        public async Task<bool> IsAttendeeAsync(Guid appointmentId, Guid userId)
        {
            return await _context.AppointmentAttendees
                .AnyAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);
        }

        public async Task<bool> DeleteAttendeeAsync(Guid appointmentId, Guid userId)
        {
            var attendee = await _context.AppointmentAttendees
                .FirstOrDefaultAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);

            if (attendee == null)
                return false;

            // Don't allow removing the organizer
            if (attendee.IsOrganizer)
                return false;

            _context.AppointmentAttendees.Remove(attendee);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllAttendeesAsync(Guid appointmentId)
        {
            var attendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();

            if (!attendees.Any())
                return false;

            _context.AppointmentAttendees.RemoveRange(attendees);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetAttendeeCountAsync(Guid appointmentId)
        {
            return await _context.AppointmentAttendees
                .CountAsync(aa => aa.AppointmentId == appointmentId);
        }

        public async Task<List<AppointmentAttendee>> UpdateAttendeesAsync(Guid appointmentId, List<Guid> newAttendeeIds, Guid organizerId)
        {
            // Get existing attendees
            var existingAttendees = await _context.AppointmentAttendees
                .Where(aa => aa.AppointmentId == appointmentId)
                .ToListAsync();

            // Remove attendees that are no longer in the list (except organizer)
            var attendeesToRemove = existingAttendees
                .Where(aa => !newAttendeeIds.Contains(aa.UserId) && !aa.IsOrganizer)
                .ToList();

            if (attendeesToRemove.Any())
            {
                _context.AppointmentAttendees.RemoveRange(attendeesToRemove);
            }

            // Add new attendees
            var existingUserIds = existingAttendees.Select(aa => aa.UserId).ToList();
            var newUserIds = newAttendeeIds.Except(existingUserIds).ToList();

            var newAttendees = newUserIds.Select(userId => new AppointmentAttendee
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                UserId = userId,
                IsOrganizer = userId == organizerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            if (newAttendees.Any())
            {
                await _context.AppointmentAttendees.AddRangeAsync(newAttendees);
            }

            // Update timestamps for existing attendees
            var attendeesToUpdate = existingAttendees
                .Where(aa => newAttendeeIds.Contains(aa.UserId))
                .ToList();

            foreach (var attendee in attendeesToUpdate)
            {
                attendee.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Return updated list
            return await GetAttendeesByAppointmentIdAsync(appointmentId);
        }

        public async Task<bool> ExistsAsync(Guid appointmentId, Guid userId)
        {
            return await _context.AppointmentAttendees
                .AnyAsync(aa => aa.AppointmentId == appointmentId && aa.UserId == userId);
        }
    }
}
