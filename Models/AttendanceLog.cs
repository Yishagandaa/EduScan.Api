using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduScan.Api.Models;

public class AttendanceLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string StudentLrn { get; set; } = string.Empty;

    public DateTime ScannedAt { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // "ON TIME" or "LATE"

    [MaxLength(50)]
    public string GateNumber { get; set; } = "Gate 1";
}
