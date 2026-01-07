using Asp.Versioning;
using CvCreator.API.Constants;
using CvCreator.Application.Common.Models;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace CvCreator.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{apiVersion:apiVersion}/coverletters")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class CoverLettersController
    (ICoverLetterService coverLetterService) : ControllerBase
{
    private readonly ICoverLetterService _coverLetterService = coverLetterService;

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> CreateCoverLetter([FromBody] CoverLetterFormValuesModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            var fullErrorMessage = string.Join("; ", errors);

            return BadRequest(new Result
            {
                IsSuccess = false,
                Message = $"Validasyon hatası: {fullErrorMessage}"
            });
        }

        Guid? userId = null;
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var parsedId))
        {
            userId = parsedId;
        }

        byte[] pdfBytes = await _coverLetterService.GenerateCoverLetterAsync(model, userId);

        string cleanName = string.IsNullOrWhiteSpace(model.SenderInfo.FullName)
            ? "coverletter"
            : model.SenderInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyCoverLetters([FromQuery] string? searchText, [FromQuery] int? limit)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(userIdString, out var userIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Token içindeki ID formatı hatalı." });
        }
        var coverletters = await _coverLetterService.GetCoverLettersAsync(userIdAsGuid, searchText, limit);

        var dtos = coverletters.Select(r => new FileResponseDto
        {
            Id = r.Id,
            FileName = r.FileName,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        }).ToList();

        return Ok(new Result<List<FileResponseDto>> { IsSuccess = true, Message = "Ön yazılar getirildi.", Data = dtos });
    }

    [Authorize]
    [HttpGet("{coverLetterId}")]
    public async Task<IActionResult> GetCoverLetterById(string coverLetterId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(coverLetterId, out var coverLetterIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Ön yazı ID formatı hatalı." });
        }

        var entity = await _coverLetterService.FindCoverLetterByIdAsync(coverLetterIdAsGuid);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var signedUrl = await _coverLetterService.CreateCoverLetterSignedUrlByIdAsync(coverLetterIdAsGuid);

        if (string.IsNullOrEmpty(signedUrl))
        {
            return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });
        }

        return Ok(new Result<object>
        {
            IsSuccess = true,
            Message = "Ön yazı getirildi.",
            Data = new { Url = signedUrl, FormValues = entity }
        });
    }

    [Authorize]
    [HttpGet("download/{coverLetterId}")]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> DownloadCoverLetterById(Guid coverLetterId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var entity = await _coverLetterService.FindCoverLetterByIdAsync(coverLetterId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var fileDto = await _coverLetterService.DownloadCoverLetterAsync(coverLetterId);

        if (fileDto.FileLength.HasValue)
        {
            Response.Headers.ContentLength = fileDto.FileLength.Value;
        }

        return File(fileDto.Stream, fileDto.ContentType);
    }

    [Authorize]
    [HttpDelete("{coverLetterId}")]
    public async Task<IActionResult> DeleteCoverLetterById(Guid coverLetterId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var entity = await _coverLetterService.FindCoverLetterByIdAsync(coverLetterId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        await _coverLetterService.DeleteCoverLetterAsync(entity);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{coverLetterId}")]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> UpdateCoverLetterById(
        [FromBody] CoverLetterFormValuesModel model, Guid coverLetterId)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            var fullErrorMessage = string.Join("; ", errors);

            return BadRequest(new Result
            {
                IsSuccess = false,
                Message = $"Validasyon hatası: {fullErrorMessage}"
            });
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var entity = await _coverLetterService.FindCoverLetterByIdAsync(coverLetterId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        entity.FileName = model.SenderInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.CoverLetterFormValues = model;

        byte[] pdfBytes = await _coverLetterService.CreateCoverLetterPdfAsync(model);
        
        await _coverLetterService.UpdateCoverLetter(pdfBytes, userIdString, entity);

        string cleanName = string.IsNullOrWhiteSpace(model.SenderInfo.FullName)
            ? "coverletter"
            : model.SenderInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
