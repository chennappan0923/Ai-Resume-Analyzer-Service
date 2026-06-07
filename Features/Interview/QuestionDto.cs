using System;

namespace AI_Resume_Analyzer_API.Features.Interview
{
    public class QuestionDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}
