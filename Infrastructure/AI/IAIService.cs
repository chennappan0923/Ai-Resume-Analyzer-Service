using System.Threading.Tasks;

namespace AI_Resume_Analyzer_API.Infrastructure.AI
{
    public interface IAIService
    {
        Task<string> AnalyzeResumeAsync(string resumeText);
        Task<string> GenerateInterviewQuestionsAsync(string resumeText, string analysisJson);
    }
}
