using CvCreator.Application.Common.Models;

namespace CvCreator.API.Middlewares;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen bir hata oluştu!");

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new Result
            {
                IsSuccess = false,
                Message = "Bir hata oluştu, daha sonra tekrar deneyin."
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
