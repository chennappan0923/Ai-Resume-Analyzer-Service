using System;

namespace AI_Resume_Analyzer_API.Features.Resumes
{
    public class ResumeDto
    {
        public int Id { get; set; }
        public string ResumeName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public bool HasAnalysis { get; set; }
    }
}
