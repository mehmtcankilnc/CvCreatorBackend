using CvCreator.Application.Contracts;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CvCreator.Infrastructure.Services;

public class PlaywrightPdfService(IPlaywright playwright) : IPdfService
{
    private readonly IPlaywright _playwright = playwright;

    public async Task<byte[]> GenerateFromHtmlAsync(string html)
    {
        await using var browser = await _playwright.Chromium.LaunchAsync(new () { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);

        byte[] pdfBytes = await page.PdfAsync(new()
        {
            Format = "A4",
            PrintBackground = true,
        });

        await browser.CloseAsync();
        return pdfBytes;
    }
}
