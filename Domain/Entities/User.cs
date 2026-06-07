using System;
using System.Collections.Generic;

namespace AI_Resume_Analyzer_API.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    }
}
