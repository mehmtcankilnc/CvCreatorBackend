using CvCreator.Application.Contracts;
using CvCreator.Application.Services;
using CvCreator.Infrastructure;
using CvCreator.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<ITemplateService, FileSystemTemplateService>();
builder.Services.AddScoped<IPdfService, PlaywrightPdfService>();

builder.Services.AddSingleton<IPlaywright>(sp =>
{
    return Playwright.CreateAsync().GetAwaiter().GetResult();
});

//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt => 
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
