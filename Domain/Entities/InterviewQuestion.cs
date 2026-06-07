using System;

namespace AI_Resume_Analyzer_API.Domain.Entities
{
    public class InterviewQuestion
    {
        public int Id { get; set; }
        public int ResumeAnalysisId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Technical, HR, Behavioral, Scenario
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ResumeAnalysis? ResumeAnalysis { get; set; }
    }
}
