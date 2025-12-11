using CvCreator.Application.Contracts;
using CvCreator.Domain.Models;
using CvCreator.Infrastructure;
using CvCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CvCreator.API.Controllers;

[Route("api")]
[ApiController]
public class CoverLettersController
    (AppDbContext appDbContext, ICoverLetterService coverLetterService) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly ICoverLetterService _coverLetterService = coverLetterService;

    [AllowAnonymous]
    [HttpPost("coverletters")]
    public async Task<IActionResult> CreateCoverLetter([FromBody] CoverLetterFormValuesModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            byte[] pdfBytes = await _coverLetterService.CreateCoverLetterPdfAsync(model);

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    await _coverLetterService.SaveCoverLetter(pdfBytes, userId, model.SenderInfo.FullName);
                }
            }

            string cleanName = string.IsNullOrWhiteSpace(model.SenderInfo.FullName)
                ? "coverletter"
                : model.SenderInfo.FullName.Trim();

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
    [HttpGet("coverletters")]
    public async Task<IActionResult> GetMyCoverLetters([FromQuery] string? searchText, [FromQuery] int? number)
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
            var coverletters = await _coverLetterService.GetCoverLettersAsync(userIdAsGuid, searchText, number);

            return Ok(coverletters);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "Mektuplar çekilirken bir hata oluştu!" });
        }
    }

    [Authorize]
    [HttpGet("coverletters/{coverLetterId}")]
    public async Task<IActionResult> GetResumeById(string coverLetterId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new { Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            if (!Guid.TryParse(coverLetterId, out var coverLetterIdAsGuid))
            {
                return BadRequest(new { Message = "CoverLetter ID formatı hatalı." });
            }

            var signedUrl = await _coverLetterService.GetCoverLetterByIdAsync(coverLetterIdAsGuid);

            if (string.IsNullOrEmpty(signedUrl))
            {
                return NotFound(new { Message = "Mektup bulunamadı." });
            }

            return Ok(new { Url = signedUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Mektup çekilirken bir hata oluştu!" });
        }
    }
}
