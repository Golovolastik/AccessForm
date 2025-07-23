using System.ComponentModel.DataAnnotations;

namespace AccessForm.Models;

public class RequestType
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Request> AccessRequests { get; set; } = new List<Request>();
} 