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
    public string Frequency { get; set; } = string.Empty;

    public int Interval { get; set; } = 1;

    public DateTime? Until { get; set; }

    public int? Count { get; set; }
}