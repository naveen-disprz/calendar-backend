using Calendar.Data;
using Calendar.Models;
using Microsoft.EntityFrameworkCore;

namespace Calendar.Repositories;

public class RecurrenceRuleRepository :  IRecurrenceRuleRepository
{
    private readonly AppDbContext _context;
    public RecurrenceRuleRepository(AppDbContext context)
    {
        _context = context;
    }
    public Task AddAsync(RecurrenceRule recurrenceRule)
    {
        throw new NotImplementedException();
    }

    public async Task<RecurrenceRule> GetOrCreateRecurrenceRuleAsync(RecurrenceRule recurrenceRule)
    {
        // Check if a rule with the same properties already exists
        var existingRule = await _context.RecurrenceRules
            .FirstOrDefaultAsync(r => r.Frequency == recurrenceRule.Frequency &&
                                      r.Until == recurrenceRule.Until);

        if (existingRule != null)
        {
            // Return the existing rule if found
            return existingRule;
        }

        // Add the new rule to the database
        await _context.RecurrenceRules.AddAsync(recurrenceRule);
        await _context.SaveChangesAsync();

        // Return the newly created rule
        return recurrenceRule;
    }
}