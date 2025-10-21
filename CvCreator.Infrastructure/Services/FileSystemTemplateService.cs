using CvCreator.Application.Contracts;
using HandlebarsDotNet;

namespace CvCreator.Infrastructure.Services;

public class FileSystemTemplateService : ITemplateService
{
    private readonly string _templateFolderPath = Path.Combine(AppContext.BaseDirectory, "Templates");

    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        string safeTemplateName = Path.GetFileName(templateName) + ".html";
        string templatePath = Path.Combine(_templateFolderPath, safeTemplateName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"{templateName} adlı şablon bulunamadı.");
        }

        string htmlTemplate = await File.ReadAllTextAsync(templatePath);

        var template = Handlebars.Compile(htmlTemplate);
        string finalHtml = template(model);

        return finalHtml;
    }
}
