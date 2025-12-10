using CvCreator.Application.Contracts;
using CvCreator.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                    await _resumeService.SaveResume(model.PersonalInfo.FullName, pdfBytes, userId);
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

    [HttpGet("resumes/{id}")]
    public async Task<IActionResult> GetResumes(string id, [FromQuery] string? searchText, [FromQuery] int? number)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { Message = "Geçersiz kullanıcı id!" });
        }

        try
        {
            Guid.TryParse(id, out var userIdAsGuid);
            var resumes = await _resumeService.GetResumesAsync(userIdAsGuid, searchText, number);

            return Ok(resumes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "Özgeçmişler çekilirken bir hata oluştu!" });
        }
    }
}
