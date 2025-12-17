using CvCreator.API.Extensions;
using CvCreator.API.Middlewares;
using CvCreator.Application.Contracts;
using CvCreator.Infrastructure;
using CvCreator.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseSecret))
    };
});

var app = builder.Build();

if (!app.Environment.IsDevelopment()) 
    app.UseHsts();

app.UseHttpsRedirection();

app.UseSecurityHeaders(policyCollection);

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.Run();
