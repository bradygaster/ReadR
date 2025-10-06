using Markdig;

namespace ReadR.Frontend.Services;

public interface IMarkdownService
{
    string ConvertToHtml(string markdown);
}

public class MarkdownService : IMarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // Includes tables, emoji, emphasis extra, etc.
        .UseSoftlineBreakAsHardlineBreak() // Convert soft line breaks to hard breaks
        .UseAutoLinks() // Auto-link URLs
        .DisableHtml() // Disable raw HTML for security
        .Build();

    public string ConvertToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        try
        {
            return Markdown.ToHtml(markdown, Pipeline);
        }
        catch (Exception)
        {
            // If markdown conversion fails, return the original text wrapped in paragraphs
            return $"<p>{System.Net.WebUtility.HtmlEncode(markdown)}</p>";
        }
    }
}
