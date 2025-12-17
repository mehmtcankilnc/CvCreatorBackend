using CvCreator.API.Constants;
using CvCreator.Application.Common.Models;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace CvCreator.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(RateLimitPolicies.StandardTraffic, context =>
            {
                string userKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetTokenBucketLimiter(userKey, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 50,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                    TokensPerPeriod = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2,
                });
            });

            options.AddPolicy(RateLimitPolicies.HeavyResource, context =>
            {
                return RateLimitPartition.GetConcurrencyLimiter("global-heavy-limit", _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = 20,
                    QueueLimit = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var response = new Result
                {
                    IsSuccess = false,
                    Message = "Limit aşıldı, biraz bekleyip tekrar deneyin."
                };

                await context.HttpContext.Response.WriteAsJsonAsync(response, token);
            };
        });

        return services;
    }
}
