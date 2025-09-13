using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class RecurrenceRule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RecurrenceRuleId { get; set; }

    [Required, MaxLength(50)]
    public string Frequency { get; set; } = string.Empty; // e.g., "Daily", "Weekly", "Monthly"

    public DateOnly Until { get; set; } // The date until the recurrence is valid
}