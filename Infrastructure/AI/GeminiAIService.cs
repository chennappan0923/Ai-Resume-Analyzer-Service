using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Resume_Analyzer_API.Infrastructure.AI
{
    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GeminiAIService> _logger;

        public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<string> AnalyzeResumeAsync(string resumeText)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var prompt = $@"
You are an expert ATS (Applicant Tracking System) scanner and professional HR Recruiter.
Analyze the following resume text and provide a detailed analysis report.

Resume Text:
---
{resumeText}
---

Your response MUST be a single valid JSON object. Do not wrap it in markdown code blocks like ```json or anything else. It must match this exact schema:
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

            return await CallGeminiApiAsync(prompt);
        }

        public async Task<string> GenerateInterviewQuestionsAsync(string resumeText, string analysisJson)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var prompt = $@"
You are a Senior Technical Interviewer and HR Manager.
Generate a set of high-quality interview questions tailored for this candidate.
Generate at least 8-12 questions total.
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

            return await CallGeminiApiAsync(prompt);
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json"
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
                
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var candidateContent) &&
                        candidateContent.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return text ?? string.Empty;
                    }
                }

                throw new HttpRequestException("Failed to extract content from Gemini API response structure.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calling Gemini API.");
                throw;
            }
        }

        private string GetMockAnalysisResponse()
        {
            return @"
{
  ""resumeScore"": 78,
  ""atsScore"": 72,
  ""careerLevel"": ""Mid-Level"",
  ""strengths"": [
    ""Strong technical foundation in backend technologies"",
    ""Clear educational background in Computer Science"",
    ""Active experience with relational databases""
  ],
  ""weaknesses"": [
    ""Lack of modern frontend framework projects listed"",
    ""No cloud platform experience explicitly mentioned"",
    ""Minimal information on automated testing (CI/CD)""
  ],
  ""missingSkills"": [
    ""Docker"",
    ""Kubernetes"",
    ""AWS or Azure"",
    ""xUnit/NUnit"",
    ""GitHub Actions""
  ],
  ""technicalSkills"": [
    ""C#"",
    "".NET Core"",
    ""SQL Server"",
    ""Entity Framework Core"",
    ""RESTful APIs""
  ],
  ""softSkills"": [
    ""Problem Solving"",
    ""Team Collaboration"",
    ""Clear Communication""
  ],
  ""suggestions"": [
    ""Add projects utilizing Docker or containerization"",
    ""Highlight experience or certifications with Azure/AWS"",
    ""Incorporate bullet points detailing your testing methodologies""
  ],
  ""recommendedRoles"": [
    ""Software Engineer - .NET Backend"",
    ""Full Stack C# Developer""
  ]
}";
        }

        private string GetMockQuestionsResponse()
        {
            return @"
[
  {
    ""question"": ""Explain the difference between transient, scoped, and singleton service lifetimes in .NET Core. When would you use each?"",
    ""category"": ""Technical""
  },
  {
    ""question"": ""What is the difference between Eager Loading and Lazy Loading in Entity Framework Core, and what are the performance trade-offs?"",
    ""category"": ""Technical""
  },
  {
    ""question"": ""Describe a complex technical challenge you faced in your last project and the steps you took to resolve it."",
    ""category"": ""Scenario""
  },
  {
    ""question"": ""How do you ensure security in a Web API? What is your experience with JWT-based authentication?"",
    ""category"": ""Technical""
  },
  {
    ""question"": ""Describe a situation where you had a disagreement with a technical lead or team member about architectural design. How did you handle it?"",
    ""category"" : ""Behavioral""
  },
  {
    ""question"": ""Why are you interested in joining our company, and where do you see your technical skills growing over the next two years?"",
    ""category"": ""HR""
  },
  {
    ""question"": ""Your application starts experiencing memory leaks in production, but everything runs fine in local environment. What is your troubleshooting process?"",
    ""category"": ""Scenario""
  },
  {
    ""question"": ""What is your approach to handling feedback during a code review, especially if you disagree with the feedback?"",
    ""category"": ""Behavioral""
  }
]";
        }
    }
}
