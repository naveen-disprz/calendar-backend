using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.DataAccess;

public class AppointmentDAL : IAppointmentDAL
    {
        private readonly AppDbContext _context;

        public AppointmentDAL(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment> CreateAsync(Appointment appointment)
        {
            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            // Load related data for response
            return appointment;
        }
        public async Task<Appointment> UpdateAsync(Appointment appointment)
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
    
            // Return with loaded navigation properties
            return appointment;
        }

        public async Task<List<Appointment>> GetConflictingAppointmentsAsync(DateTime startDateTime, DateTime endDateTime, Guid userId, Guid? excludeAppointmentId = null)
        {
            var query = _context.Appointments
                .Include(a => a.Organizer)
                .Include(a => a.Attendees)
                .ThenInclude(aa => aa.User)
                .Where(a => !a.IsDeleted &&
                           (a.OrganizerId == userId || a.Attendees.Any(aa => aa.UserId == userId)) &&
                           a.StartDateTime <= endDateTime &&
                           a.EndDateTime >= startDateTime);

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid appointmentId)
        {
            return await _context.Appointments
                .AnyAsync(a => a.Id == appointmentId && !a.IsDeleted);
        }

        public async Task<Appointment?> GetByIdAsync(Guid appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Organizer)
                .Include(a => a.AppointmentType)
                .Include(a => a.RecurrenceRule)
                .Include(a => a.Attendees)
                .ThenInclude(aa => aa.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);
        }

        public async Task<List<Appointment>> GetAppointmentsByDateRangeAsync(Guid userId, DateTime fromDate, DateTime toDate, Guid? appointmentTypeId = null, bool? includeRecurring = true)
        {
            var query = _context.Appointments
                .Include(a => a.Organizer)
                .Include(a => a.AppointmentType)
                .Include(a => a.RecurrenceRule)
                .Include(a => a.Attendees)
                .ThenInclude(aa => aa.User)
                .Where(a => !a.IsDeleted &&
                            (a.OrganizerId == userId || a.Attendees.Any(aa => aa.UserId == userId)));

            // Modified date range logic to include recurring appointments
            query = query.Where(a => 
                    // Non-recurring appointments: use original logic
                    (a.RecurrenceRuleId == null && 
                     ((a.StartDateTime >= fromDate && a.StartDateTime < toDate) ||
                      (a.EndDateTime > fromDate && a.EndDateTime <= toDate) ||
                      (a.StartDateTime <= fromDate && a.EndDateTime >= toDate))) ||
        
                    // Recurring appointments: include if they could have instances in the range
                    (a.RecurrenceRuleId != null && 
                     a.StartDateTime < toDate && // Started before the end of our range
                     (a.RecurrenceRule!.EndDate == null || a.RecurrenceRule.EndDate >= fromDate)) // And either no end or ends after our range starts
            );

            // Filter by appointment type if specified
            if (appointmentTypeId.HasValue)
            {
                query = query.Where(a => a.AppointmentTypeId == appointmentTypeId.Value);
            }

            // Filter recurring appointments if specified
            if (includeRecurring == false)
            {
                query = query.Where(a => a.RecurrenceRuleId == null);
            }

            return await query
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();
        }

        
        public async Task<bool> DeleteAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

            if (appointment == null)
                return false;

            // Soft delete - set IsDeleted flag instead of removing from database
            appointment.IsDeleted = true;
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            return true;
        }
        
        public async Task<List<Appointment>> GetPotentialConflictingAppointmentsAsync(
            DateTime startDateTime, 
            DateTime endDateTime, 
            Guid userId, 
            Guid? excludeAppointmentId = null)
        {
            // Get appointments that:
            // 1. Are not deleted
            // 2. User is either organizer or attendee
            // 3. Either overlap directly OR are recurring (might have instances that overlap)
    
            var query = _context.Appointments
                .Include(a => a.Organizer)
                .Include(a => a.RecurrenceRule)
                .Include(a => a.Attendees)
                .ThenInclude(aa => aa.User)
                .Where(a => !a.IsDeleted &&
                            (a.OrganizerId == userId || a.Attendees.Any(aa => aa.UserId == userId)));

            // Exclude specific appointment if provided
            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            // Get appointments that either:
            // 1. Directly overlap with the time range (non-recurring)
            // 2. Are recurring and could potentially have instances in this range
            query = query.Where(a => 
                    // Non-recurring appointments that overlap
                    (a.RecurrenceRuleId == null && 
                     a.StartDateTime < endDateTime && 
                     a.EndDateTime > startDateTime) ||
        
                    // Recurring appointments that could have instances
                    (a.RecurrenceRuleId != null &&
                     a.StartDateTime.Date <= endDateTime.Date && // Started before or on the end date
                     (a.RecurrenceRule!.EndDate == null || a.RecurrenceRule.EndDate >= startDateTime.Date)) // No end or ends after start
            );

            return await query.ToListAsync();
        }
    }