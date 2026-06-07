using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Resume_Analyzer_API.Infrastructure.AI
{
    public class OllamaAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;
        private readonly ILogger<OllamaAIService> _logger;

        public OllamaAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            _model = configuration["Ollama:Model"] ?? "llama3.1:8b";
        }

        public async Task<string> AnalyzeResumeAsync(string resumeText)
        {
            var prompt = $@"
You are an expert ATS (Applicant Tracking System) scanner and professional HR Recruiter.
Analyze the following resume text and provide a detailed analysis report.

Resume Text:
---
{resumeText}
---

Your response MUST be a single valid JSON object. Do not wrap it in markdown code blocks like ```json or anything else. 
To ensure fast generation, keep the descriptions short and limit list counts:
- ""strengths"": maximum 3 items, concise (1 sentence each)
- ""weaknesses"": maximum 3 items, concise (1 sentence each)
- ""missingSkills"": maximum 3 items
- ""technicalSkills"": maximum 5 items
- ""softSkills"": maximum 4 items
- ""suggestions"": maximum 3 items, concise (1 sentence each)
- ""recommendedRoles"": maximum 2 items

It must match this exact schema:
{{
  ""resumeScore"": 0 to 100 integer representing general resume quality (formatting, readability, action verbs),
  ""atsScore"": 0 to 100 integer representing how well the resume is optimized for ATS parsing and keyword matching,
  ""careerLevel"": ""Entry-Level"" or ""Mid-Level"" or ""Senior"" or ""Executive"",
  ""strengths"": [""strength 1"", ""strength 2"", ...],
  ""weaknesses"": [""weakness 1"", ""weakness 2"", ...],
  ""missingSkills"": [""missing skill 1"", ""missing skill 2"", ...],
  ""technicalSkills"": [""tech skill 1"", ""tech skill 2"", ...],
  ""softSkills"": [""soft skill 1"", ""soft skill 2"", ...],
  ""suggestions"": [""suggestion 1"", ""suggestion 2"", ...],
  ""recommendedRoles"": [""role 1"", ""role 2"", ...]
}}
";

            return await CallOllamaApiAsync(prompt);
        }

        public async Task<string> GenerateInterviewQuestionsAsync(string resumeText, string analysisJson)
        {
            var prompt = $@"
You are a Senior Technical Interviewer and HR Manager.
Generate a set of high-quality interview questions tailored for this candidate.
Generate exactly 4-6 questions total.
Distribute the questions across these categories: ""Technical"", ""Behavioral"", ""HR"", and ""Scenario"".

Candidate's Resume:
---
{resumeText}
---

Candidate's Resume Analysis:
---
{analysisJson}
---

Your response MUST be a single valid JSON array of objects. Do not wrap it in markdown code blocks like ```json or anything else. It must match this exact schema:
[
  {{
    ""question"": ""Write the full question text here"",
    ""category"": ""Technical"" or ""Behavioral"" or ""HR"" or ""Scenario""
  }},
  ...
]
";

            return await CallOllamaApiAsync(prompt);
        }

        private async Task<string> CallOllamaApiAsync(string prompt)
        {
            var url = $"{_baseUrl.TrimEnd('/')}/api/generate";

            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                format = "json",
                options = new
                {
                    temperature = 0.1
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                
                if (doc.RootElement.TryGetProperty("response", out var responseText))
                {
                    return responseText.GetString() ?? string.Empty;
                }

                throw new HttpRequestException("Failed to extract content from Ollama API response structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling Ollama API.");
                throw;
            }
        }
    }
}
