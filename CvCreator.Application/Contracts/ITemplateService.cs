namespace CvCreator.Application.Contracts;

public interface ITemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object model);
}
