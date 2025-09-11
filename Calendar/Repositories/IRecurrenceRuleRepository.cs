using Calendar.Models;

namespace Calendar.Repositories;

public interface IRecurrenceRuleRepository
{
    Task AddAsync(RecurrenceRule recurrenceRule);
}