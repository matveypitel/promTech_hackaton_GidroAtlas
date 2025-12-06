using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;

namespace GidroAtlas.Api.Infrastructure.Documents;

/// <summary>
/// PDF text extractor using PdfPig library.
/// Supports extraction from files and streams.
/// </summary>
public class PdfPigTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfPigTextExtractor> _logger;

    public PdfPigTextExtractor(ILogger<PdfPigTextExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(pdfPath))
        {
            _logger.LogError("PDF file not found: {Path}", pdfPath);
            throw new FileNotFoundException("PDF file not found", pdfPath);
        }

        // Read file async and then process
        var bytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        using var stream = new MemoryStream(bytes);
        
        return await ExtractTextAsync(stream, cancellationToken);
    }

    public Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var textBuilder = new StringBuilder();

            try
            {
                using var document = PdfDocument.Open(pdfStream);
                
                _logger.LogInformation("Processing PDF with {PageCount} pages", document.NumberOfPages);

                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var pageText = ExtractPageText(page);
                        
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            textBuilder.AppendLine($"--- Страница {page.Number} ---");
                            textBuilder.AppendLine(pageText);
                            textBuilder.AppendLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from page {PageNumber}", page.Number);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF document");
                throw;
            }

            return textBuilder.ToString();
        }, cancellationToken);
    }

    private static string ExtractPageText(Page page)
    {
        var words = page.GetWords().ToList();
        
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var textBuilder = new StringBuilder();
        var lastY = words[0].BoundingBox.Bottom;
        const double lineThreshold = 5; // Points threshold for new line detection

        foreach (var word in words)
        {
            // Detect line breaks based on Y position change
            if (Math.Abs(word.BoundingBox.Bottom - lastY) > lineThreshold)
            {
                textBuilder.AppendLine();
                lastY = word.BoundingBox.Bottom;
            }
            else
            {
                textBuilder.Append(' ');
            }

            textBuilder.Append(word.Text);
        }

        return textBuilder.ToString().Trim();
    }
}
