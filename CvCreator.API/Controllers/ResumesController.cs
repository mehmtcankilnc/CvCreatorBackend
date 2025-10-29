using CvCreator.Application.Contracts;
using CvCreator.Domain.Entities;
using CvCreator.Domain.Models;
using CvCreator.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.API.Controllers;

[Route("api")]
[ApiController]
public class ResumesController(
    AppDbContext appDbContext, IResumeService resumeService) : ControllerBase
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly IResumeService _resumeService = resumeService;

    [HttpGet("resumes")]
    public async Task<IActionResult> GetAllResumes()
    {
        return Ok(await _appDbContext.Resumes.ToListAsync());
    }

    [HttpPost("resumes")]
    public async Task<IActionResult> CreateResume([FromBody] ResumeFormValuesModel model, [FromQuery] string templateName, string userId)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            return BadRequest(new { Message = "Lütfen bir şablon adı belirtin" });
        }

        try
        {
            byte[] pdfBytes = await _resumeService.CreateResumePdfAsync(model, templateName);
            await _resumeService.SaveResume(model.PersonalInfo.FullName, pdfBytes, userId);

            return File(pdfBytes, "application/pdf", model.PersonalInfo.FullName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "PDF oluşturulurken sunucuda bir hata oluştu."});
        }
    }

    [HttpGet("resumes/{id}")]
    public async Task<IActionResult> GetResumes(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            return BadRequest(new { Message = "Geçersiz kullanıcı id!" });
        }

        try
        {
            var resumes = await _resumeService.GetResumesAsync(id);

            return Ok(resumes);
        } catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return StatusCode(500, new { Message = "Özgeçmişler çekilirken bir hata oluştu!" });
        }
    }
}
