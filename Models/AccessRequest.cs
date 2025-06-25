using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessForm.Models;

public class AccessRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string Position { get; set; } = string.Empty;
    
    [Required]
    public DateTime EmploymentDate { get; set; }
    
    [Required]
    public string DocumentPath { get; set; } = string.Empty;
    
    [Required]
    public int RequestTypeId { get; set; }
    
    [ForeignKey("RequestTypeId")]
    public RequestType RequestType { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; } // IPv6 совместимость
} 