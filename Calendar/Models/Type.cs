using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Calendar.Models;

public class Type
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AppointmentTypeId { get; set; }   // Auto-increment ID

    [Required, MaxLength(100)] 
    public string TypeName { get; set; } = string.Empty;

    [Required] 
    public string ColorCode { get; set; } = string.Empty;
}