using System.ComponentModel.DataAnnotations;

namespace AI_Resume_Analyzer_API.Features.Profile
{
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
    }
}
