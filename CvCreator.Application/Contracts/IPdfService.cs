using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvCreator.Application.Contracts;

public interface IPdfService
{
    Task<byte[]> GenerateFromHtmlAsync(string html);
}
