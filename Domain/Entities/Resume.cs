using System;

namespace AI_Resume_Analyzer_API.Domain.Entities
{
    public class Resume
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ResumeName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public ResumeAnalysis? Analysis { get; set; }
    }
}
