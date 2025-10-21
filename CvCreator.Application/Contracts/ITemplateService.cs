using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvCreator.Application.Contracts;

public interface ITemplateService
{
    Task<string> RenderTemplateAsync(string templateName, object model);
}
