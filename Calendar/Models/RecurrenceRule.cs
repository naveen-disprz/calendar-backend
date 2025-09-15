using System.ComponentModel.DataAnnotations;

namespace Calendar.Models;

public class RecurrenceRule
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required] [MaxLength(20)] public string Frequency { get; set; } = string.Empty; // DAILY, WEEKLY, MONTHLY

    public int? DaysOfWeekMask { get; set; } // Bitmask: 1=Sun,2=Mon,4=Tue,8=Wed,16=Thu,32=Fri,64=Sat

    public int? DaysOfMonthMask { get; set; } // Bitmask: 1=Day1,2=Day2,4=Day3,...,1073741824=Day31

    public DateTime? EndDate { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    // Helper Methods for Bitmask Operations
    public bool HasDayOfWeek(DayOfWeek dayOfWeek)
    {
        if (DaysOfWeekMask == null) return false;

        int dayMask = dayOfWeek switch
        {
            DayOfWeek.Sunday => 1,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 4,
            DayOfWeek.Wednesday => 8,
            DayOfWeek.Thursday => 16,
            DayOfWeek.Friday => 32,
            DayOfWeek.Saturday => 64,
            _ => 0
        };

        return (DaysOfWeekMask & dayMask) > 0;
    }

    public bool HasDayOfMonth(int day)
    {
        if (DaysOfMonthMask == null || day < 1 || day > 31) return false;

        int dayMask = (int)Math.Pow(2, day - 1);
        return (DaysOfMonthMask & dayMask) > 0;
    }

    public List<DayOfWeek> GetDaysOfWeek()
    {
        var days = new List<DayOfWeek>();
        if (DaysOfWeekMask == null) return days;

        var dayMappings = new Dictionary<int, DayOfWeek>
        {
            { 1, DayOfWeek.Sunday },
            { 2, DayOfWeek.Monday },
            { 4, DayOfWeek.Tuesday },
            { 8, DayOfWeek.Wednesday },
            { 16, DayOfWeek.Thursday },
            { 32, DayOfWeek.Friday },
            { 64, DayOfWeek.Saturday }
        };

        foreach (var mapping in dayMappings)
        {
            if ((DaysOfWeekMask & mapping.Key) > 0)
            {
                days.Add(mapping.Value);
            }
        }

        return days.OrderBy(d => (int)d).ToList();
    }

    public List<int> GetDaysOfMonth()
    {
        var days = new List<int>();
        if (DaysOfMonthMask == null) return days;

        for (int day = 1; day <= 31; day++)
        {
            if (HasDayOfMonth(day))
            {
                days.Add(day);
            }
        }

        return days;
    }

    public void SetDaysOfWeek(IEnumerable<DayOfWeek> days)
    {
        DaysOfWeekMask = 0;
        foreach (var day in days)
        {
            int dayMask = day switch
            {
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 4,
                DayOfWeek.Wednesday => 8,
                DayOfWeek.Thursday => 16,
                DayOfWeek.Friday => 32,
                DayOfWeek.Saturday => 64,
                _ => 0
            };
            DaysOfWeekMask |= dayMask;
        }
    }

    public void SetDaysOfMonth(IEnumerable<int> days)
    {
        DaysOfMonthMask = 0;
        foreach (var day in days.Where(d => d >= 1 && d <= 31))
        {
            int dayMask = (int)Math.Pow(2, day - 1);
            DaysOfMonthMask |= dayMask;
        }
    }
}