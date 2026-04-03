using System.ComponentModel.DataAnnotations;

namespace ClaudeWebApp.DTOs;

public class SampleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
