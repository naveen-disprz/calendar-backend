using Calendar.Data;
using Calendar.Models;

namespace Calendar.DataAccess;

public class RecurrenceRuleDAL: IRecurrenceRuleDAL
{
    private readonly AppDbContext _context;

    public RecurrenceRuleDAL(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RecurrenceRule> CreateAsync(RecurrenceRule recurrenceRule)
    {
        await _context.RecurrenceRules.AddAsync(recurrenceRule);
        await _context.SaveChangesAsync();
        return recurrenceRule;
    }
    
    public async Task<RecurrenceRule?> UpdateAsync(Guid recurrenceRuleId, RecurrenceRule updatedRule)
    {
        var existingRule = await _context.RecurrenceRules.FindAsync(recurrenceRuleId);
        if (existingRule == null)
        {
            return null;
        }

        existingRule.Frequency = updatedRule.Frequency;
        existingRule.DaysOfWeekMask = updatedRule.DaysOfWeekMask;
        existingRule.DaysOfMonthMask = updatedRule.DaysOfMonthMask;
        existingRule.EndDate = updatedRule.EndDate;
        existingRule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingRule;
    }
    
    public async Task<bool> DeleteAsync(Guid recurrenceRuleId)
    {
        var existingRule = await _context.RecurrenceRules.FindAsync(recurrenceRuleId);
        if (existingRule == null)
        {
            return false;
        }

        _context.RecurrenceRules.Remove(existingRule);
        await _context.SaveChangesAsync();
        return true;
    }
    
}