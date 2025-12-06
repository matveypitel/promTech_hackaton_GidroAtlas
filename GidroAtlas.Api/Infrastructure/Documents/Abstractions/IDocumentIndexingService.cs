namespace GidroAtlas.Api.Infrastructure.Documents.Abstractions;

/// <summary>
/// Service for indexing documents into vector embeddings.
/// Supports water objects and PDF documents.
/// </summary>
public interface IDocumentIndexingService
{
    /// <summary>
    /// Index all water objects from the database.
    /// </summary>
    /// <returns>Number of indexed objects.</returns>
    Task<int> IndexAllWaterObjectsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Index a PDF document by extracting text and creating embeddings.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="documentName">Name/title of the document.</param>
    /// <param name="chunkSize">Size of text chunks for embedding.</param>
    /// <param name="chunkOverlap">Overlap between chunks.</param>
    /// <returns>Number of chunks indexed.</returns>
    Task<int> IndexPdfDocumentAsync(
        string pdfPath,
        string documentName,
        int chunkSize = 1000,
        int chunkOverlap = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Index plain text content.
    /// </summary>
    /// <param name="content">Text content to index.</param>
    /// <param name="documentName">Name of the document.</param>
    /// <param name="contentType">Type of content (e.g., "reference", "manual").</param>
    /// <returns>Number of chunks indexed.</returns>
    Task<int> IndexTextContentAsync(
        string content,
        string documentName,
        string contentType = "reference",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all indexed documents of a specific type.
    /// </summary>
    Task ClearIndexAsync(string? contentType = null, CancellationToken cancellationToken = default);
}
