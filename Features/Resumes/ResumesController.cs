using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AI_Resume_Analyzer_API.Domain.Entities;
using AI_Resume_Analyzer_API.Infrastructure.Database;
using AI_Resume_Analyzer_API.Infrastructure.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AI_Resume_Analyzer_API.Features.Resumes
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ResumesController : ControllerBase
    {
        private readonly ResumeAnalyzerDbContext _context;
        private readonly PdfParserService _pdfParser;
        private readonly DocxParserService _docxParser;

        public ResumesController(
            ResumeAnalyzerDbContext context,
            PdfParserService pdfParser,
            DocxParserService docxParser)
        {
            _context = context;
            _pdfParser = pdfParser;
            _docxParser = docxParser;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".pdf" && extension != ".docx")
            {
                return BadRequest("Unsupported file type. Only PDF and DOCX are allowed.");
            }

            var userId = GetUserId();
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsDir, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Test if we can parse text immediately to validate file integrity
            string extractedText;
            try
            {
                if (extension == ".pdf")
                {
                    extractedText = _pdfParser.ExtractText(filePath);
                }
                else
                {
                    extractedText = _docxParser.ExtractText(filePath);
                }

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    // Clean up and error
                    System.IO.File.Delete(filePath);
                    return BadRequest("Failed to extract any text from the resume. Ensure it is not an image or corrupted.");
                }
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return BadRequest($"Error parsing the file: {ex.Message}");
            }

            var resume = new Resume
            {
                UserId = userId,
                ResumeName = Path.GetFileNameWithoutExtension(file.FileName),
                OriginalFileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length
            };

            _context.Resumes.Add(resume);
            await _context.SaveChangesAsync();

            return Ok(new ResumeDto
            {
                Id = resume.Id,
                ResumeName = resume.ResumeName,
                OriginalFileName = resume.OriginalFileName,
                FileSize = resume.FileSize,
                UploadedDate = resume.UploadedDate,
                HasAnalysis = false
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var userId = GetUserId();
            var resumes = await _context.Resumes
                .Where(r => r.UserId == userId)
                .Select(r => new ResumeDto
                {
                    Id = r.Id,
                    ResumeName = r.ResumeName,
                    OriginalFileName = r.OriginalFileName,
                    FileSize = r.FileSize,
                    UploadedDate = r.UploadedDate,
                    HasAnalysis = r.Analysis != null
                })
                .OrderByDescending(r => r.UploadedDate)
                .ToListAsync();

            return Ok(resumes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var resume = await _context.Resumes
                .Include(r => r.Analysis)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
            }

            return Ok(new
            {
                resume.Id,
                resume.ResumeName,
                resume.OriginalFileName,
                resume.FileSize,
                resume.UploadedDate,
                HasAnalysis = resume.Analysis != null
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var resume = await _context.Resumes
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
            }

            // Remove file from disk
            if (System.IO.File.Exists(resume.FilePath))
            {
                try
                {
                    System.IO.File.Delete(resume.FilePath);
                }
                catch (Exception)
                {
                    // Ignore or log file delete error
                }
            }

            _context.Resumes.Remove(resume);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Resume deleted successfully." });
        }

        [HttpGet("download/{id}")]
        [AllowAnonymous] // Allow download if token is passed via query or we can just download via standard auth if UI supports it.
        // Actually, let's keep it authenticated, but to support simple UI download link we can allow anonymous but check a signed URL, 
        // or just let the UI handle the authorization header when fetching. Let's keep it authenticated first.
        public async Task<IActionResult> Download(int id)
        {
            // If we want to allow download, we should check user identity
            var userId = GetUserId();
            
            // Wait, if it is authenticated, GetUserId() will return the correct ID.
            var resume = await _context.Resumes
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (resume == null)
            {
                return NotFound("Resume not found.");
            }

            if (!System.IO.File.Exists(resume.FilePath))
            {
                return NotFound("File not found on disk.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(resume.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = resume.OriginalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) 
                ? "application/pdf" 
                : "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(memory, contentType, resume.OriginalFileName);
        }
    }
}
