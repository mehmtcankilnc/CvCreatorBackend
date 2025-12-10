using CvCreator.Application.Contracts;
using CvCreator.Domain.Models;
using CvCreator.Infrastructure;
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
                    await _coverLetterService.SaveCoverLetter(model.SenderInfo.FullName, pdfBytes, userId);
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

    [HttpGet("coverletters/{id}")]
    public async Task<IActionResult> GetCoverLetters(string id, [FromQuery] string? searchText, int? number)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { Message = "Geçersiz kullanıcı id!" });
        }

        try
        {
            Guid.TryParse(id, out var userIdAsGuid);
            var coverletters = await _coverLetterService.GetCoverLettersAsync(userIdAsGuid, searchText, number);

            return Ok(coverletters);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "Mektuplar çekilirken bir hata oluştu!" });
        }
    }
}
