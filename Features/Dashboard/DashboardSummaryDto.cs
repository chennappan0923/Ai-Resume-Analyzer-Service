using System;
using System.Collections.Generic;

namespace AI_Resume_Analyzer_API.Features.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalResumes { get; set; }
        public double AverageAtsScore { get; set; }
        public int BestResumeScore { get; set; }
        public int TotalAnalyses { get; set; }
        public List<ScoreTrendItem> ScoreTrends { get; set; } = new();
        public List<SkillCountItem> MissingSkills { get; set; } = new();
    }

    public class ScoreTrendItem
    {
        public string ResumeName { get; set; } = string.Empty;
        public int AtsScore { get; set; }
        public int ResumeScore { get; set; }
        public DateTime Date { get; set; }
    }

    public class SkillCountItem
    {
        public string Skill { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
