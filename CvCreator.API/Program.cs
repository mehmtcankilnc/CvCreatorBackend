using Asp.Versioning;
using CvCreator.API.Extensions;
using CvCreator.API.Middlewares;
using CvCreator.Application.Contracts;
using CvCreator.Application.Mappings;
using CvCreator.Infrastructure;
using CvCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

    builder.Services.AddHttpClient<SupabaseStorageService>(client =>
    {
        var config = builder.Configuration;
        var url = config["Supabase:Url"];
        var key = config["Supabase:ServiceKey"];

        client.BaseAddress = new Uri($"{url}/storage/v1/");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", key);
    });
    builder.Services.AddScoped<IResumeService, ResumeService>();
    builder.Services.AddScoped<ITemplateService, FileSystemTemplateService>();
    builder.Services.AddScoped<IPdfService, PlaywrightPdfService>();
    builder.Services.AddScoped<ICoverLetterService, CoverLetterService>();
    builder.Services.AddScoped<IUserService, UserService>();

    builder.Services.AddCustomRateLimiting();

    builder.Services.AddSingleton<IPlaywright>(sp =>
    {
        return Playwright.CreateAsync().GetAwaiter().GetResult();
    });

    var policyCollection = new HeaderPolicyCollection().AddDefaultSecurityHeaders();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.EnableDynamicJson();

    var dataSource = dataSourceBuilder.Build();

    builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(dataSource));

    var supabaseUrl = builder.Configuration["Supabase:Url"];
    var supabaseSecret = builder.Configuration["Supabase:JwtSecret"];

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",

            ValidateAudience = true,
            ValidAudience = "authenticated",

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseSecret!))
        };
    });

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString!,
            name: "PostgreSQL",
            tags: ["database"]
        );

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://localhost:4317");
                });
        })
        .WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();
        });

    builder.Services.AddMemoryCache();
    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
        cfg.AddProfile<MappingProfile>();
    });

    var app = builder.Build();

    app.MapPrometheusScrapingEndpoint();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    app.UseHttpsRedirection();

    app.UseSecurityHeaders(policyCollection);

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                Status = report.Status.ToString(),
                Details = report.Entries.Select(x => new
                {
                    Component = x.Key,
                    Status = x.Value.Status.ToString(),
                    x.Value.Description,
                    x.Value.Duration,
                })
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Uygulama beklenmedik bir şekilde durdu!");
}
finally
{
    Log.CloseAndFlush();
}
