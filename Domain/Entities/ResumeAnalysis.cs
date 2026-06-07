using System;
using System.Collections.Generic;

namespace AI_Resume_Analyzer_API.Domain.Entities
{
    public class ResumeAnalysis
    {
        public int Id { get; set; }
        public int ResumeId { get; set; }
        public int ResumeScore { get; set; }
        public int AtsScore { get; set; }
        public string CareerLevel { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty; // JSON array or text
        public string Weaknesses { get; set; } = string.Empty; // JSON array or text
        public string MissingSkills { get; set; } = string.Empty; // JSON array or text
        public string TechnicalSkills { get; set; } = string.Empty; // JSON array or text
        public string SoftSkills { get; set; } = string.Empty; // JSON array or text
        public string Suggestions { get; set; } = string.Empty; // JSON array or text
        public string RecommendedRoles { get; set; } = string.Empty; // JSON array or text
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Resume? Resume { get; set; }
        public ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();
    }
}
