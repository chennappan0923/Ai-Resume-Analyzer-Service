using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AI_Resume_Analyzer_API.Domain.Entities;
using AI_Resume_Analyzer_API.Infrastructure.AI;
using AI_Resume_Analyzer_API.Infrastructure.Database;
using AI_Resume_Analyzer_API.Infrastructure.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Resume_Analyzer_API.Features.Interview
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly ResumeAnalyzerDbContext _context;
        private readonly IAIService _aiService;
        private readonly PdfParserService _pdfParser;
        private readonly DocxParserService _docxParser;

        public InterviewController(
            ResumeAnalyzerDbContext context,
            IAIService aiService,
            PdfParserService pdfParser,
            DocxParserService docxParser)
        {
            _context = context;
            _aiService = aiService;
            _pdfParser = pdfParser;
            _docxParser = docxParser;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        [HttpPost("generate/{resumeId}")]
        public async Task<IActionResult> Generate(int resumeId)
        {
            var userId = GetUserId();
            var resume = await _context.Resumes
                .Include(r => r.Analysis)
                .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
            }

            if (resume.Analysis == null)
            {
                return BadRequest("Resume must be analyzed first before generating interview questions.");
            }

            if (!System.IO.File.Exists(resume.FilePath))
            {
                return NotFound("Resume file not found on disk.");
            }

            // Extract text from resume
            string resumeText;
            var extension = Path.GetExtension(resume.FilePath).ToLowerInvariant();
            try
            {
                if (extension == ".pdf")
                {
                    resumeText = _pdfParser.ExtractText(resume.FilePath);
                }
                else
                {
                    resumeText = _docxParser.ExtractText(resume.FilePath);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error parsing file for question generation: {ex.Message}");
            }

            // Stringify analysis for context
            var analysisJson = JsonSerializer.Serialize(new
            {
                resume.Analysis.ResumeScore,
                resume.Analysis.AtsScore,
                resume.Analysis.CareerLevel,
                resume.Analysis.TechnicalSkills,
                resume.Analysis.SoftSkills,
                resume.Analysis.MissingSkills
            });

            // Call Gemini
            string questionsJson;
            List<GeminiQuestion>? questionsList;
            try
            {
                questionsJson = await _aiService.GenerateInterviewQuestionsAsync(resumeText, analysisJson);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                questionsList = JsonSerializer.Deserialize<List<GeminiQuestion>>(questionsJson, options);
                if (questionsList == null)
                {
                    throw new JsonException("Deserialized questions list was null.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"AI Interview Generation Error: {ex.Message}");
            }

            // Delete existing questions for this analysis
            var oldQuestions = await _context.InterviewQuestions
                .Where(q => q.ResumeAnalysisId == resume.Analysis.Id)
                .ToListAsync();
            _context.InterviewQuestions.RemoveRange(oldQuestions);

            // Add new questions
            foreach (var q in questionsList)
            {
                var newQuestion = new InterviewQuestion
                {
                    ResumeAnalysisId = resume.Analysis.Id,
                    Question = q.question,
                    Category = q.category
                };
                _context.InterviewQuestions.Add(newQuestion);
            }

            await _context.SaveChangesAsync();

            var result = await _context.InterviewQuestions
                .Where(q => q.ResumeAnalysisId == resume.Analysis.Id)
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Question = q.Question,
                    Category = q.Category,
                    CreatedDate = q.CreatedDate
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{resumeId}")]
        public async Task<IActionResult> GetQuestions(int resumeId)
        {
            var userId = GetUserId();
            var resume = await _context.Resumes
                .Include(r => r.Analysis)
                .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
            }

            if (resume.Analysis == null)
            {
                return BadRequest("Resume must be analyzed first before retrieving questions.");
            }

            var questions = await _context.InterviewQuestions
                .Where(q => q.ResumeAnalysisId == resume.Analysis.Id)
                .Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Question = q.Question,
                    Category = q.Category,
                    CreatedDate = q.CreatedDate
                })
                .ToListAsync();

            return Ok(questions);
        }
    }
}
