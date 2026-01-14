using Asp.Versioning;
using CvCreator.API.Constants;
using CvCreator.Application.Common.Models;
using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace CvCreator.API.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{apiVersion:apiVersion}/coverletters")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class CoverLettersController
    (ICoverLetterService coverLetterService, IMemoryCache memoryCache) : ControllerBase
{
    private readonly ICoverLetterService _coverLetterService = coverLetterService;
    private readonly IMemoryCache _memoryCache = memoryCache;

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
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(userIdString, out var userIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Token içindeki ID formatı hatalı." });
        }

        var dtos = await _coverLetterService.GetCoverLettersAsync(userIdAsGuid, searchText, limit);

        return Ok(new Result<List<FileResponseDto>> { IsSuccess = true, Message = "Ön yazılar getirildi.", Data = dtos });
    }

    [Authorize]
    [HttpGet("{coverLetterId}")]
    public async Task<IActionResult> GetCoverLetterById(string coverLetterId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(coverLetterId, out var coverLetterIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Ön yazı ID formatı hatalı." });
        }

        string cacheKey = $"coverletter_{userIdString}_{coverLetterId}";

        var entity = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);

            return await _coverLetterService.FindCoverLetterByIdAsync(coverLetterIdAsGuid);
        });

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var downloadUrl = $"{baseUrl}/api/v1/coverletters/download/{coverLetterId}";

        return Ok(new Result<object>
        {
            IsSuccess = true,
            Message = "Ön yazı getirildi.",
            Data = new { Url = downloadUrl, FormValues = entity }
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

        string cacheKey = $"coverletter_file_{userIdString}_{coverLetterId}";

        var result = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);

            return await _coverLetterService.GetCoverLetterFileAsync(coverLetterId, Guid.Parse(userIdString));
        });

        if (result == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        return File(result.Value.FileContent, "application/pdf", result.Value.FileName);
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

        var coverLetterDto = await _coverLetterService.FindCoverLetterByIdAsync(coverLetterId);

        if (coverLetterDto == null) return NotFound(new Result { IsSuccess = false, Message = "Ön yazı bulunamadı." });

        if (coverLetterDto.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        byte[] pdfBytes = await _coverLetterService.CreateCoverLetterPdfAsync(model);
        
        await _coverLetterService.UpdateCoverLetter(pdfBytes, coverLetterId, model);

        string cleanName = string.IsNullOrWhiteSpace(model.SenderInfo.FullName)
            ? "coverletter"
            : model.SenderInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
