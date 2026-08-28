using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduScan.Api.Models;

public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Lrn { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Grade { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Section { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Track { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? GuardianPhone { get; set; }

    public bool IsActiveUser { get; set; } = false;

    public DateTime? LastActiveAt { get; set; }
}
