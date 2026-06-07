using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AI_Resume_Analyzer_API.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Resume_Analyzer_API.Features.Dashboard
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ResumeAnalyzerDbContext _context;

        public DashboardController(ResumeAnalyzerDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetUserId();

            var totalResumes = await _context.Resumes
                .CountAsync(r => r.UserId == userId);

            var analyses = await _context.ResumeAnalyses
                .Include(a => a.Resume)
                .Where(a => a.Resume!.UserId == userId)
                .ToListAsync();

            var totalAnalyses = analyses.Count;

            var avgAtsScore = totalAnalyses > 0 
                ? Math.Round(analyses.Average(a => a.AtsScore), 1) 
                : 0;

            var bestResumeScore = totalAnalyses > 0 
                ? analyses.Max(a => a.ResumeScore) 
                : 0;

            // Compute score trends
            var scoreTrends = analyses
                .OrderBy(a => a.CreatedDate)
                .Select(a => new ScoreTrendItem
                {
                    ResumeName = a.Resume!.ResumeName,
                    AtsScore = a.AtsScore,
                    ResumeScore = a.ResumeScore,
                    Date = a.CreatedDate
                })
                .ToList();

            // Compute top missing skills
            var missingSkillsCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var analysis in analyses)
            {
                if (!string.IsNullOrWhiteSpace(analysis.MissingSkills))
                {
                    try
                    {
                        var skills = JsonSerializer.Deserialize<List<string>>(analysis.MissingSkills, options);
                        if (skills != null)
                        {
                            foreach (var skill in skills)
                            {
                                var cleanSkill = skill.Trim();
                                if (!string.IsNullOrEmpty(cleanSkill))
                                {
                                    if (missingSkillsCount.ContainsKey(cleanSkill))
                                        missingSkillsCount[cleanSkill]++;
                                    else
                                        missingSkillsCount[cleanSkill] = 1;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore JSON parsing errors for a single record
                    }
                }
            }

            var topMissingSkills = missingSkillsCount
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new SkillCountItem
                {
                    Skill = kv.Key,
                    Count = kv.Value
                })
                .ToList();

            var summary = new DashboardSummaryDto
            {
                TotalResumes = totalResumes,
                AverageAtsScore = avgAtsScore,
                BestResumeScore = bestResumeScore,
                TotalAnalyses = totalAnalyses,
                ScoreTrends = scoreTrends,
                MissingSkills = topMissingSkills
            };

            return Ok(summary);
        }
    }
}
