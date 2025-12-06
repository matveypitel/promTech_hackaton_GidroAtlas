namespace GidroAtlas.Api.Infrastructure.Documents.Abstractions;

/// <summary>
/// Service for extracting text from PDF documents.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>
    /// Extract all text content from a PDF file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted text content.</returns>
    Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract text from a stream.
    /// </summary>
    Task<string> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
