using System.ComponentModel.DataAnnotations;

namespace QuakePulse_WebService.Models.Api;

public class FrontendLogRequest
{
    [Required(ErrorMessage = "Message is required.")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 4000 characters.")]
    public string Message { get; set; } = string.Empty;

    [Required(ErrorMessage = "Level is required.")]
    [RegularExpression("^(Info|Warning|Error)$", ErrorMessage = "Level must be Info, Warning, or Error.")]
    public string Level { get; set; } = string.Empty;

    [Required(ErrorMessage = "CorrelationId is required.")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "CorrelationId must be between 1 and 128 characters.")]
    public string CorrelationId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Timestamp is required.")]
    public DateTime? Timestamp { get; set; }
}
