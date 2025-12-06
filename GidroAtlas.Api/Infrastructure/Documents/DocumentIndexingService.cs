using GidroAtlas.Api.Entities;
using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.AI.Chat;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;
using GidroAtlas.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace GidroAtlas.Api.Infrastructure.Documents;

/// <summary>
/// Service for indexing documents into vector embeddings.
/// Supports water objects and PDF/text documents.
/// </summary>
public class DocumentIndexingService : IDocumentIndexingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ILogger<DocumentIndexingService> _logger;

    public DocumentIndexingService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        IPdfTextExtractor pdfTextExtractor,
        ILogger<DocumentIndexingService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _pdfTextExtractor = pdfTextExtractor;
        _logger = logger;
    }

    public async Task<int> IndexAllWaterObjectsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting indexing of all water objects...");

        var waterObjects = await _context.WaterObjects.ToListAsync(cancellationToken);
        var indexedCount = 0;

        // Clear existing water object embeddings
        await _context.WaterObjectEmbeddings.ExecuteDeleteAsync(cancellationToken);

        foreach (var obj in waterObjects)
        {
            try
            {
                var content = GenerateWaterObjectContent(obj);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(content, cancellationToken);

                if (embedding != null)
                {
                    var embeddingEntity = new WaterObjectEmbedding
                    {
                        WaterObjectId = obj.Id,
                        ChunkIndex = 0,
                        ContentType = "main",
                        Content = content,
                        Embedding = new Vector(embedding),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.WaterObjectEmbeddings.Add(embeddingEntity);
                    indexedCount++;

                    _logger.LogDebug("Indexed water object: {Name}", obj.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index water object: {Id}", obj.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Indexing complete. Indexed {Count} water objects", indexedCount);

        return indexedCount;
    }

    public async Task<int> IndexPdfDocumentAsync(
        string pdfPath,
        string documentName,
        int chunkSize = 1000,
        int chunkOverlap = 200,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting PDF indexing: {DocumentName} from {Path}", documentName, pdfPath);

        // Extract text from PDF
        var fullText = await _pdfTextExtractor.ExtractTextAsync(pdfPath, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(fullText))
        {
            _logger.LogWarning("No text extracted from PDF: {Path}", pdfPath);
            return 0;
        }

        _logger.LogInformation("Extracted {Length} characters from PDF", fullText.Length);

        // Split into chunks
        var chunks = SplitIntoChunks(fullText, chunkSize, chunkOverlap);
        _logger.LogInformation("Split into {Count} chunks", chunks.Count);

        var fileName = Path.GetFileName(pdfPath);
        return await IndexDocumentChunksAsync(chunks, documentName, fileName, "pdf", cancellationToken);
    }

    public async Task<int> IndexTextContentAsync(
        string content,
        string documentName,
        string contentType = "reference",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        var chunks = SplitIntoChunks(content, 1000, 200);
        return await IndexDocumentChunksAsync(chunks, documentName, null, contentType, cancellationToken);
    }

    public async Task ClearIndexAsync(string? contentType = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            // Clear all embeddings
            await _context.WaterObjectEmbeddings.ExecuteDeleteAsync(cancellationToken);
            await _context.DocumentEmbeddings.ExecuteDeleteAsync(cancellationToken);
            _logger.LogInformation("Cleared all embeddings");
        }
        else if (contentType == "main")
        {
            // Clear only water object embeddings
            await _context.WaterObjectEmbeddings.ExecuteDeleteAsync(cancellationToken);
            _logger.LogInformation("Cleared water object embeddings");
        }
        else
        {
            // Clear document embeddings of specific type
            var deleted = await _context.DocumentEmbeddings
                .Where(e => e.ContentType == contentType)
                .ExecuteDeleteAsync(cancellationToken);
            _logger.LogInformation("Cleared {Count} document embeddings of type {Type}", deleted, contentType);
        }
    }

    private async Task<int> IndexDocumentChunksAsync(
        List<string> chunks,
        string documentName,
        string? fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var indexedCount = 0;

        for (var i = 0; i < chunks.Count; i++)
        {
            try
            {
                var chunk = chunks[i];
                var chunkWithContext = $"Документ: {documentName}\n\n{chunk}";
                
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkWithContext, cancellationToken);

                if (embedding != null)
                {
                    var embeddingEntity = new DocumentEmbedding
                    {
                        Id = Guid.NewGuid(),
                        DocumentName = documentName,
                        FileName = fileName,
                        ChunkIndex = i,
                        ContentType = contentType,
                        Content = chunkWithContext,
                        Embedding = new Vector(embedding),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.DocumentEmbeddings.Add(embeddingEntity);
                    indexedCount++;

                    if ((i + 1) % 10 == 0)
                    {
                        _logger.LogDebug("Indexed {Count}/{Total} chunks", i + 1, chunks.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to index chunk {Index} of {Document}", i, documentName);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Indexed {Count} chunks for document: {Name}", indexedCount, documentName);

        return indexedCount;
    }

    private static List<string> SplitIntoChunks(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        
        if (string.IsNullOrEmpty(text))
            return chunks;

        // Normalize whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        var position = 0;
        while (position < text.Length)
        {
            var endPosition = Math.Min(position + chunkSize, text.Length);
            
            // Try to end at a sentence boundary
            if (endPosition < text.Length)
            {
                var lastPeriod = text.LastIndexOf('.', endPosition, Math.Min(chunkSize, endPosition));
                var lastNewline = text.LastIndexOf('\n', endPosition, Math.Min(chunkSize, endPosition));
                var bestBreak = Math.Max(lastPeriod, lastNewline);
                
                if (bestBreak > position + chunkSize / 2)
                {
                    endPosition = bestBreak + 1;
                }
            }

            var chunk = text[position..endPosition].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            position = endPosition - overlap;
            if (position >= text.Length - overlap)
            {
                break;
            }
        }

        return chunks;
    }

    private static string GenerateWaterObjectContent(WaterObject obj)
    {
        var conditionText = PromptTemplates.GetConditionDescription(obj.TechnicalCondition);
        var resourceTypeText = obj.ResourceType.GetDisplayName();
        var waterTypeText = obj.WaterType.GetDisplayName();
        var passportAge = (DateTime.UtcNow - obj.PassportDate).Days / 365;
        var priority = (6 - obj.TechnicalCondition) * 3 + passportAge;
        var priorityLevel = PromptTemplates.GetPriorityDescription(priority);

        return string.Format(
            PromptTemplates.WaterObjectSummaryTemplate,
            obj.Name,
            obj.Region,
            resourceTypeText,
            waterTypeText,
            obj.HasFauna ? "да, присутствует" : "нет, отсутствует",
            obj.TechnicalCondition,
            conditionText,
            obj.PassportDate.ToString("dd.MM.yyyy"),
            passportAge,
            priorityLevel,
            priority,
            obj.Latitude,
            obj.Longitude
        );
    }
}
