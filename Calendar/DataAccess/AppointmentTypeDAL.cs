using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.DataAccess;

public class AppointmentTypeDAL: IAppointmentTypeDAL
{
    private readonly AppDbContext _context;
    
    public AppointmentTypeDAL(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<AppointmentType?> GetByIdAsync(Guid appointmentTypeId)
    {
        return await _context.AppointmentTypes
            .FirstOrDefaultAsync(at => at.Id == appointmentTypeId);
    }

    public Task<List<AppointmentType>> GetAllAsync()
    {
        return _context.AppointmentTypes
            .ToListAsync();
    }
}