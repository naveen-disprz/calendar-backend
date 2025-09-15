using Calendar.Models;

namespace Calendar.DataAccess;

public interface IRecurrenceRuleDAL
{
    Task<RecurrenceRule?> UpdateAsync(Guid recurrenceRuleId, RecurrenceRule updatedRule);
    Task<RecurrenceRule> CreateAsync(RecurrenceRule recurrenceRule);
    Task<bool> DeleteAsync(Guid recurrenceRuleId);
}