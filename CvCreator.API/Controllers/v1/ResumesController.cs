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
[Route("api/v{apiVersion:apiVersion}/resumes")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.StandardTraffic)]
public class ResumesController(IResumeService resumeService) : ControllerBase
{
    private readonly IResumeService _resumeService = resumeService;

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
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized(new Result { IsSuccess = false, Message = "Kullanıcı kimliği doğrulanamadı." });
        }

        if (!Guid.TryParse(userIdString, out var userIdAsGuid))
        {
            return BadRequest(new Result { IsSuccess = false, Message = "Token içindeki ID formatı hatalı." });
        }

        var resumes = await _resumeService.GetResumesAsync(userIdAsGuid, searchText, limit);

        var dtos = resumes.Select(r => new FileResponseDto
        {
            Id = r.Id,
            FileName = r.FileName,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        }).ToList();

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

        var entity = await _resumeService.FindResumeByIdAsync(resumeIdAsGuid);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var signedUrl = await _resumeService.CreateResumeSignedUrlByIdAsync(resumeIdAsGuid);

        if (string.IsNullOrEmpty(signedUrl))
        {
            return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });
        }

        return Ok(new Result<object>
        {
            IsSuccess = true,
            Message = "Özgeçmiş getirildi.",
            Data = new { Url = signedUrl, FormValues = entity }
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

        var entity = await _resumeService.FindResumeByIdAsync(resumeId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        var fileDto = await _resumeService.DownloadResumeAsync(resumeId);

        if (fileDto.FileLength.HasValue)
        {
            Response.Headers.ContentLength = fileDto.FileLength.Value;
        }

        return File(fileDto.Stream, fileDto.ContentType);
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

        var entity = await _resumeService.FindResumeByIdAsync(resumeId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        await _resumeService.DeleteResumeAsync(entity);

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

        var entity = await _resumeService.FindResumeByIdAsync(resumeId);

        if (entity == null) return NotFound(new Result { IsSuccess = false, Message = "Özgeçmiş bulunamadı." });

        if (entity.UserId != Guid.Parse(userIdString))
        {
            return Forbid();
        }

        entity.FileName = model.PersonalInfo.FullName;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.ResumeFormValues = model;

        byte[] pdfBytes = await _resumeService.CreateResumePdfAsync(model, templateName);

        await _resumeService.UpdateResume(pdfBytes, userIdString, entity);

        string cleanName = string.IsNullOrWhiteSpace(model.PersonalInfo.FullName)
            ? "coverletter"
            : model.PersonalInfo.FullName.Trim();

        string fileName = $"{cleanName}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }
}
