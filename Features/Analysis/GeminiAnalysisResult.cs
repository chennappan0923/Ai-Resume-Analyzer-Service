using System.Collections.Generic;

namespace AI_Resume_Analyzer_API.Features.Analysis
{
    public class GeminiAnalysisResult
    {
        public int resumeScore { get; set; }
        public int atsScore { get; set; }
        public string careerLevel { get; set; } = string.Empty;
        public List<string> strengths { get; set; } = new();
        public List<string> weaknesses { get; set; } = new();
        public List<string> missingSkills { get; set; } = new();
        public List<string> technicalSkills { get; set; } = new();
        public List<string> softSkills { get; set; } = new();
        public List<string> suggestions { get; set; } = new();
        public List<string> recommendedRoles { get; set; } = new();
    }
}
