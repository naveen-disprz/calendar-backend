using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class Appointment
{
    [Key] 
    public Guid AppointmentId { get; set; } =  Guid.NewGuid();

    [Required, MaxLength(200)] 
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required] 
    public DateTime StartTime { get; set; }

    [Required] 
    public DateTime EndTime { get; set; }
    
    // Timestamps
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [ForeignKey(nameof(CreatedByUser))] 
    public Guid CreatedByUserId { get; set; }
    
    public User CreatedByUser { get; set; }

    [ForeignKey(nameof(RecurrenceRule))] 
    public Guid? RecurrenceRuleId { get; set; }
    
    public RecurrenceRule? RecurrenceRule { get; set; }

    [ForeignKey(nameof(AppointmentType))] 
    public Guid? AppointmentTypeId { get; set; }
    
    public AppointmentType? AppointmentType { get; set; }
} 
