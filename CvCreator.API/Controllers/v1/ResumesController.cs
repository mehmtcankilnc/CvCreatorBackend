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
[Route("api/v{apiVersion:apiVersion}/resumes")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class ResumesController(
    IResumeService resumeService, IMemoryCache memoryCache) : ControllerBase
{
    private readonly IResumeService _resumeService = resumeService;
    private readonly IMemoryCache _memoryCache = memoryCache;

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> CreateResume([FromBody] ResumeFormValuesModel model, [FromQuery] string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Lütfen bir şablon adı belirtin." });
        }

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

        byte[] pdfBytes = await _resumeService.GenerateResumeAsync(model, templateName, userId);

        string cleanName = string.IsNullOrWhiteSpace(model.PersonalInfo.FullName)
            ? "resume"
            : model.PersonalInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetResumes([FromQuery] string? searchText, [FromQuery] int? limit)
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

        var dtos = await _resumeService.GetResumesAsync(userIdAsGuid, searchText, limit);

        return Ok(new Result<List<FileResponseDto>> { IsSuccess = true, Message = "Özgeçmişler getirildi.", Data = dtos});
    }

    [Authorize]
    [HttpGet("{resumeId}")]
    public async Task<IActionResult> GetResumeById(string resumeId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(resumeId, out var resumeIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Özgeçmiş ID formatı hatalı." });
        }

        string cacheKey = $"resume_{userIdString}_{resumeId}";

        var entity = await _memoryCache.GetOrCreateAsync(cacheKey, async entry => 
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);

            return await _resumeService.FindResumeByIdAsync(resumeIdAsGuid); 
        });

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        var downloadUrl = $"{baseUrl}/api/v1/resumes/download/{resumeId}";

        return Ok(new Result<object>
        {
            IsSuccess = true,
            Message = "Özgeçmiş getirildi.",
            Data = new { Url = downloadUrl, FormValues = entity }
        });
    }

    [Authorize]
    [HttpGet("download/{resumeId}")]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> DownloadResumeById(Guid resumeId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        string cacheKey = $"resume_file_{userIdString}_{resumeId}";

        var result = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            entry.SlidingExpiration = TimeSpan.FromMinutes(2);

            return await _resumeService.GetResumeFileAsync(resumeId, Guid.Parse(userIdString));
        });

        if (result == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        return File(result.Value.FileContent, "application/pdf", result.Value.FileName);
    }

    [Authorize]
    [HttpDelete("{resumeId}")]
    public async Task<IActionResult> DeleteResumeById(Guid resumeId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var resumeDto = await _resumeService.FindResumeByIdAsync(resumeId);

        if (resumeDto == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (resumeDto.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        await _resumeService.DeleteResumeAsync(resumeDto);

        return NoContent();
    }

    [Authorize]
    [HttpPut("{resumeId}")]
    [EnableRateLimiting(RateLimitPolicies.HeavyResource)]
    public async Task<IActionResult> UpdateResumeById(
        [FromBody] ResumeFormValuesModel model, Guid resumeId, [FromQuery] string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Lütfen bir şablon adı belirtin." });
        }

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

        var resumeDto = await _resumeService.FindResumeByIdAsync(resumeId);

        if (resumeDto == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (resumeDto.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        byte[] pdfBytes = await _resumeService.CreateResumePdfAsync(model, templateName);

        await _resumeService.UpdateResume(pdfBytes, resumeId, model);

        string cleanName = string.IsNullOrWhiteSpace(model.PersonalInfo.FullName)
            ? "coverletter"
            : model.PersonalInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
