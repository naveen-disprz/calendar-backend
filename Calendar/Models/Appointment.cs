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

    // 🔑 Date of the appointment
    [Required]
    public DateOnly AppointmentDate { get; set; }

    // 🔑 Start and end times (time-only)
    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
    
    // Timestamps
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [ForeignKey(nameof(Organizer))] 
    public Guid OrganizerId { get; set; }
    
    public User Organizer { get; set; }

    [ForeignKey(nameof(RecurrenceRule))] 
    public int? RecurrenceRuleId { get; set; }
    
    public RecurrenceRule? RecurrenceRule { get; set; }

    [ForeignKey(nameof(AppointmentType))] 
    public int? AppointmentTypeId { get; set; }
    
    public Type? AppointmentType { get; set; }
    
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
} 