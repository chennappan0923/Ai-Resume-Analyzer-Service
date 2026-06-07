using System;
using System.Collections.Generic;

namespace AI_Resume_Analyzer_API.Features.Analysis
{
    public class AnalysisResponseDto
    {
        public int Id { get; set; }
        public int ResumeId { get; set; }
        public string ResumeName { get; set; } = string.Empty;
        public int ResumeScore { get; set; }
        public int AtsScore { get; set; }
        public string CareerLevel { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> MissingSkills { get; set; } = new();
        public List<string> TechnicalSkills { get; set; } = new();
        public List<string> SoftSkills { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public List<string> RecommendedRoles { get; set; } = new();
        public DateTime CreatedDate { get; set; }
    }
}
