using Calendar.Models;

namespace Calendar.Utils;

public static class RecurrenceUtils
{
    private static Appointment CreateAppointmentInstance(Appointment original, DateTime instanceStart,
        DateTime instanceEnd)
    {
        var instance = new Appointment
        {
            // Generate a unique ID for this instance
            Id = Guid.NewGuid(),

            // Copy all properties from original
            OrganizerId = original.OrganizerId,
            Title = original.Title,
            Description = original.Description,
            StartDateTime = instanceStart,
            EndDateTime = instanceEnd,
            Location = original.Location,
            AppointmentTypeId = original.AppointmentTypeId,
            RecurrenceRuleId = original.RecurrenceRuleId,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt,
            IsDeleted = original.IsDeleted,

            // Copy navigation properties
            Organizer = original.Organizer,
            AppointmentType = original.AppointmentType,
            RecurrenceRule = original.RecurrenceRule,
            Attendees = original.Attendees
        };

        return instance;
    }

    private static bool ShouldCreateInstance(Appointment appointment, RecurrenceRule rule, DateTime currentDate)
    {
        // Don't create instances before the original appointment date
        if (currentDate < appointment.StartDateTime.Date)
        {
            return false;
        }

        switch (rule.Frequency.ToUpper())
        {
            case "DAILY":
                return true;

            case "WEEKLY":
                var daysOfWeek = rule.GetDaysOfWeek();
                if (!daysOfWeek.Any())
                {
                    // If no specific days selected, use the original appointment's day
                    daysOfWeek = new List<DayOfWeek> { appointment.StartDateTime.DayOfWeek };
                }

                return daysOfWeek.Contains(currentDate.DayOfWeek);

            case "MONTHLY":
                var daysOfMonth = rule.GetDaysOfMonth();
                if (!daysOfMonth.Any())
                {
                    // If no specific days selected, use the original appointment's day
                    daysOfMonth = new List<int> { appointment.StartDateTime.Day };
                }

                // Check if current day is in the list and is valid for this month
                return daysOfMonth.Contains(currentDate.Day) &&
                       currentDate.Day <= DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            default:
                return false;
        }
    }

    public static List<Appointment> ExpandRecurringAppointment(Appointment appointment, DateTime fromDate,
        DateTime toDate)
    {
        var instances = new List<Appointment>();

        if (appointment.RecurrenceRule == null || !appointment.IsRecurring)
        {
            return instances;
        }

        var recurrenceRule = appointment.RecurrenceRule;
        var duration = appointment.EndDateTime - appointment.StartDateTime;

        // Convert times to UTC for consistent handling
        var originalStartTimeOfDay = appointment.StartDateTime.TimeOfDay;

        // Start from the appointment's start date
        var currentDate = appointment.StartDateTime.Date;

        // If fromDate is later, adjust to start checking from fromDate
        if (fromDate.Date > currentDate)
        {
            currentDate = fromDate.Date;
        }

        // End at recurrence end date or toDate, whichever is earlier
        var endDate = toDate.Date;
        if (recurrenceRule.EndDate.HasValue && recurrenceRule.EndDate.Value.Date < endDate)
        {
            endDate = recurrenceRule.EndDate.Value.Date;
        }

        // Safety limit to prevent infinite loops
        var maxIterations = 1000;
        var iterations = 0;

        while (currentDate <= endDate && iterations < maxIterations)
        {
            if (ShouldCreateInstance(appointment, recurrenceRule, currentDate))
            {
                // Create the instance datetime in UTC by combining the date with the original time
                var instanceStart = DateTime.SpecifyKind(
                    currentDate.Date.Add(originalStartTimeOfDay),
                    DateTimeKind.Utc
                );

                var instanceEnd = instanceStart.Add(duration);

                // Only add if instance is within the requested range
                if (instanceStart >= fromDate && instanceStart <= toDate)
                {
                    var instance = CreateAppointmentInstance(appointment, instanceStart, instanceEnd);
                    instances.Add(instance);
                }
            }

            currentDate = GetNextDate(recurrenceRule, currentDate);
            iterations++;
        }

        return instances;
    }

    private static DateTime GetNextDate(RecurrenceRule rule, DateTime currentDate)
    {
        switch (rule.Frequency.ToUpper())
        {
            case "DAILY":
                return currentDate.AddDays(1);

            case "WEEKLY":
                return currentDate.AddDays(1);

            case "MONTHLY":
                var nextDate = currentDate.AddDays(1);

                // If we've moved to the next month, find the first valid day
                if (nextDate.Month != currentDate.Month)
                {
                    var daysOfMonth = rule.GetDaysOfMonth();
                    if (daysOfMonth.Any())
                    {
                        // Find the first valid day that exists in this month
                        var validDaysInMonth = daysOfMonth
                            .Where(d => d <= DateTime.DaysInMonth(nextDate.Year, nextDate.Month))
                            .OrderBy(d => d);

                        if (validDaysInMonth.Any())
                        {
                            nextDate = new DateTime(nextDate.Year, nextDate.Month, validDaysInMonth.First());
                        }
                    }
                }

                return nextDate;

            default:
                return currentDate.AddDays(1);
        }
    }
}