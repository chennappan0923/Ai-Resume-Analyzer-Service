using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AI_Resume_Analyzer_API.Infrastructure.AI
{
    public class FallbackAIService : IAIService
    {
        private readonly GeminiAIService _geminiService;
        private readonly OllamaAIService _ollamaService;
        private readonly ILogger<FallbackAIService> _logger;

        public FallbackAIService(
            GeminiAIService geminiService,
            OllamaAIService ollamaService,
            ILogger<FallbackAIService> logger)
        {
            _geminiService = geminiService;
            _ollamaService = ollamaService;
            _logger = logger;
        }

        public async Task<string> AnalyzeResumeAsync(string resumeText)
        {
            try
            {
                _logger.LogInformation("Attempting resume analysis using Google Gemini.");
                return await _geminiService.AnalyzeResumeAsync(resumeText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Gemini resume analysis failed. Falling back to local Ollama service.");
                return await _ollamaService.AnalyzeResumeAsync(resumeText);
            }
        }

        public async Task<string> GenerateInterviewQuestionsAsync(string resumeText, string analysisJson)
        {
            try
            {
                _logger.LogInformation("Attempting interview question generation using Google Gemini.");
                return await _geminiService.GenerateInterviewQuestionsAsync(resumeText, analysisJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Gemini interview question generation failed. Falling back to local Ollama service.");
                return await _ollamaService.GenerateInterviewQuestionsAsync(resumeText, analysisJson);
            }
        }
    }
}
