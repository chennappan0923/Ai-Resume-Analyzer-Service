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

namespace AI_Resume_Analyzer_API.Features.Analysis
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly ResumeAnalyzerDbContext _context;
        private readonly IAIService _aiService;
        private readonly PdfParserService _pdfParser;
        private readonly DocxParserService _docxParser;

        public AnalysisController(
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

        [HttpPost("{resumeId}")]
        public async Task<IActionResult> Analyze(int resumeId)
        {
            var userId = GetUserId();
            var resume = await _context.Resumes
                .Include(r => r.Analysis)
                .FirstOrDefaultAsync(r => r.Id == resumeId && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
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
                return BadRequest($"Error parsing file for analysis: {ex.Message}");
            }

            // Call Gemini
            string analysisJson;
            GeminiAnalysisResult? resultObj;
            try
            {
                analysisJson = await _aiService.AnalyzeResumeAsync(resumeText);
                
                // Validate that response parses to expected object
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                resultObj = JsonSerializer.Deserialize<GeminiAnalysisResult>(analysisJson, options);
                if (resultObj == null)
                {
                    throw new JsonException("Deserialized analysis object was null.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"AI Analysis Service Error: {ex.Message}");
            }

            // Remove existing analysis if present
            if (resume.Analysis != null)
            {
                _context.ResumeAnalyses.Remove(resume.Analysis);
            }

            var analysis = new ResumeAnalysis
            {
                ResumeId = resumeId,
                ResumeScore = resultObj.resumeScore,
                AtsScore = resultObj.atsScore,
                CareerLevel = resultObj.careerLevel,
                Strengths = JsonSerializer.Serialize(resultObj.strengths),
                Weaknesses = JsonSerializer.Serialize(resultObj.weaknesses),
                MissingSkills = JsonSerializer.Serialize(resultObj.missingSkills),
                TechnicalSkills = JsonSerializer.Serialize(resultObj.technicalSkills),
                SoftSkills = JsonSerializer.Serialize(resultObj.softSkills),
                Suggestions = JsonSerializer.Serialize(resultObj.suggestions),
                RecommendedRoles = JsonSerializer.Serialize(resultObj.recommendedRoles)
            };

            _context.ResumeAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(analysis, resume.OriginalFileName));
        }

        [HttpGet("{resumeId}")]
        public async Task<IActionResult> GetAnalysis(int resumeId)
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
                return NotFound("No analysis exists for this resume yet. Please trigger an analysis first.");
            }

            return Ok(MapToDto(resume.Analysis, resume.OriginalFileName));
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetUserId();
            var history = await _context.ResumeAnalyses
                .Include(a => a.Resume)
                .Where(a => a.Resume!.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .Select(a => new
                {
                    a.Id,
                    a.ResumeId,
                    ResumeName = a.Resume!.ResumeName,
                    a.ResumeScore,
                    a.AtsScore,
                    a.CareerLevel,
                    a.CreatedDate
                })
                .ToListAsync();

            return Ok(history);
        }

        private AnalysisResponseDto MapToDto(ResumeAnalysis analysis, string resumeName)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            return new AnalysisResponseDto
            {
                Id = analysis.Id,
                ResumeId = analysis.ResumeId,
                ResumeName = resumeName,
                ResumeScore = analysis.ResumeScore,
                AtsScore = analysis.AtsScore,
                CareerLevel = analysis.CareerLevel,
                Strengths = JsonSerializer.Deserialize<List<string>>(analysis.Strengths, options) ?? new(),
                Weaknesses = JsonSerializer.Deserialize<List<string>>(analysis.Weaknesses, options) ?? new(),
                MissingSkills = JsonSerializer.Deserialize<List<string>>(analysis.MissingSkills, options) ?? new(),
                TechnicalSkills = JsonSerializer.Deserialize<List<string>>(analysis.TechnicalSkills, options) ?? new(),
                SoftSkills = JsonSerializer.Deserialize<List<string>>(analysis.SoftSkills, options) ?? new(),
                Suggestions = JsonSerializer.Deserialize<List<string>>(analysis.Suggestions, options) ?? new(),
                RecommendedRoles = JsonSerializer.Deserialize<List<string>>(analysis.RecommendedRoles, options) ?? new(),
                CreatedDate = analysis.CreatedDate
            };
        }
    }
}
