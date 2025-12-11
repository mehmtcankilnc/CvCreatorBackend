using CvCreator.Application.Contracts;
using CvCreator.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

namespace CvCreator.API.Controllers;

[Route("api")]
[ApiController]
public class ResumesController(IResumeService resumeService) : ControllerBase
{
    private readonly IResumeService _resumeService = resumeService;

    [AllowAnonymous]
    [HttpPost("resumes")]
    public async Task<IActionResult> CreateResume([FromBody] ResumeFormValuesModel model, [FromQuery] string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return BadRequest(new { Message = "Lütfen bir şablon adı belirtin." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            byte[] pdfBytes = await _resumeService.CreateResumePdfAsync(model, templateName);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    await _resumeService.SaveResume(pdfBytes, userId, model.PersonalInfo.FullName);
                }
            }

            string cleanName = string.IsNullOrWhiteSpace(model.PersonalInfo.FullName)
                ? "resume"
                : model.PersonalInfo.FullName.Trim();

            string fileName = $"{cleanName}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF Oluşturma Hatası: {ex}");
            return StatusCode(500, new { Message = "PDF oluşturulurken sunucuda bir hata oluştu." });
        }
    }

    [Authorize]
    [HttpGet("resumes")]
    public async Task<IActionResult> GetResumes([FromQuery] string? searchText, [FromQuery] int? number)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new { Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            if (!Guid.TryParse(userIdString, out var userIdAsGuid))
            {
                return BadRequest(new { Message = "Token içindeki ID formatı hatalı." });
            }
            var resumes = await _resumeService.GetResumesAsync(userIdAsGuid, searchText, number);

            return Ok(resumes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "Özgeçmişler çekilirken bir hata oluştu!" });
        }
    }

    [Authorize]
    [HttpGet("resumes/{resumeId}")]
    public async Task<IActionResult> GetResumeById(string resumeId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new { Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            if (!Guid.TryParse(resumeId, out var resumeIdAsGuid))
            {
                return BadRequest(new { Message = "Resume ID formatı hatalı." });
            }

            var signedUrl = await _resumeService.GetResumeByIdAsync(resumeIdAsGuid);

            if (string.IsNullOrEmpty(signedUrl))
            {
                return NotFound(new { Message = "Özgeçmiş bulunamadı." });
            }

            return Ok(new { Url = signedUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Özgeçmiş çekilirken bir hata oluştu!" });
        }
    }


}
