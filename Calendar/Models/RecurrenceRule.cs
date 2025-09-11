using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Calendar.Models;

public class RecurrenceRule
{
    [Key] 
    public Guid RecurrenceRuleId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)] 
    public string Frequency { get; set; } = string.Empty;

    public int Interval { get; set; } = 1;

    public DateTime? Until { get; set; }

    public int? Count { get; set; }
}