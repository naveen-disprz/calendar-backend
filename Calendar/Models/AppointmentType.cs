using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Calendar.Models;

public class AppointmentType
{
    [Key] 
    public Guid AppointmentTypeId { get; set; } =  Guid.NewGuid();

    [Required, MaxLength(100)] 
    public string TypeName { get; set; } = string.Empty;

    [Required] 
    public string ColorCode { get; set; } = string.Empty;
}