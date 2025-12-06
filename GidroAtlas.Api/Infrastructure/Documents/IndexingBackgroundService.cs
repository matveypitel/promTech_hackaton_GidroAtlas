using GidroAtlas.Api.Infrastructure.AI.Abstractions;
using GidroAtlas.Api.Infrastructure.Documents.Abstractions;

namespace GidroAtlas.Api.Infrastructure.Documents;

/// <summary>
/// Background service that automatically indexes water objects and PDF documents when the application starts.
/// Waits for Ollama to be available before starting indexing.
/// </summary>
public class IndexingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IndexingBackgroundService> _logger;
    private readonly IWebHostEnvironment _environment;
    
    private const int MaxRetries = 30; // Max 5 minutes waiting for Ollama (30 * 10s)
    private const int RetryDelaySeconds = 10;
    private const string PdfDocsRelativePath = "docs/pdfs";

    public IndexingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<IndexingBackgroundService> logger,
        IWebHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        _logger.LogInformation("IndexingBackgroundService started. Waiting for Ollama to be ready...");

        using var scope = _serviceProvider.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        var indexingService = scope.ServiceProvider.GetRequiredService<IDocumentIndexingService>();
        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();

        // Wait for Ollama embedding service to be available
        var ollamaReady = await WaitForOllamaAsync(embeddingService, stoppingToken);

        if (!ollamaReady)
        {
            _logger.LogWarning("Ollama embedding service is not available after {MaxRetries} attempts. " +
                              "Indexing will not be performed automatically. " +
                              "Use POST /api/chat/index to trigger indexing manually.", MaxRetries);
            return;
        }

        // Check current index status
        try
        {
            var indexedCount = await ragService.GetIndexedDocumentCountAsync(stoppingToken);
            
            if (indexedCount > 0)
            {
                _logger.LogInformation("Found {Count} already indexed documents. Skipping auto-indexing.", 
                    indexedCount);
                return;
            }

            // Index water objects from database
            await IndexWaterObjectsAsync(chatService, stoppingToken);

            // Index PDF documents from docs/pdfs folder
            await IndexPdfDocumentsAsync(indexingService, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic indexing. Use POST /api/chat/index to retry.");
        }
    }

    private async Task<bool> WaitForOllamaAsync(IEmbeddingService embeddingService, CancellationToken stoppingToken)
    {
        for (var i = 0; i < MaxRetries && !stoppingToken.IsCancellationRequested; i++)
        {
            try
            {
                var isReady = await embeddingService.IsAvailableAsync(stoppingToken);
                if (isReady)
                {
                    _logger.LogInformation("Ollama embedding service is ready!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ollama not ready yet, attempt {Attempt}/{MaxRetries}", i + 1, MaxRetries);
            }

            _logger.LogInformation("Waiting for Ollama... attempt {Attempt}/{MaxRetries}", i + 1, MaxRetries);
            await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
        }

        return false;
    }

    private async Task IndexWaterObjectsAsync(IChatService chatService, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting automatic indexing of water objects...");
        
        try
        {
            var indexedCount = await chatService.IndexAllWaterObjectsAsync(stoppingToken);
            _logger.LogInformation("Indexed {Count} water objects.", indexedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index water objects");
        }
    }

    private async Task IndexPdfDocumentsAsync(IDocumentIndexingService indexingService, CancellationToken stoppingToken)
    {
        // Get the path to docs/pdfs folder (relative to content root)
        var contentRoot = _environment.ContentRootPath;
        
        // Try multiple possible locations for the docs folder
        var possiblePaths = new[]
        {
            Path.Combine(contentRoot, PdfDocsRelativePath),
            Path.Combine(contentRoot, "..", PdfDocsRelativePath),
            Path.Combine(contentRoot, "..", "..", PdfDocsRelativePath),
            Path.Combine(Directory.GetCurrentDirectory(), PdfDocsRelativePath),
        };

        string? pdfFolderPath = null;
        foreach (var path in possiblePaths)
        {
            var normalizedPath = Path.GetFullPath(path);
            if (Directory.Exists(normalizedPath))
            {
                pdfFolderPath = normalizedPath;
                break;
            }
        }

        if (pdfFolderPath == null)
        {
            _logger.LogWarning("PDF documents folder not found. Tried paths: {Paths}", 
                string.Join(", ", possiblePaths.Select(Path.GetFullPath)));
            return;
        }

        _logger.LogInformation("Found PDF documents folder: {Path}", pdfFolderPath);

        // Get all PDF files
        var pdfFiles = Directory.GetFiles(pdfFolderPath, "*.pdf", SearchOption.AllDirectories);

        if (pdfFiles.Length == 0)
        {
            _logger.LogInformation("No PDF files found in {Path}", pdfFolderPath);
            return;
        }

        _logger.LogInformation("Found {Count} PDF files to index", pdfFiles.Length);

        var totalChunks = 0;
        var successCount = 0;

        foreach (var pdfFile in pdfFiles)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(pdfFile);
                var documentName = CleanDocumentName(fileName);

                _logger.LogInformation("Indexing PDF: {FileName} as '{DocumentName}'...", 
                    Path.GetFileName(pdfFile), documentName);

                var chunksIndexed = await indexingService.IndexPdfDocumentAsync(
                    pdfFile,
                    documentName,
                    chunkSize: 1000,
                    chunkOverlap: 200,
                    cancellationToken: stoppingToken);

                totalChunks += chunksIndexed;
                successCount++;

                _logger.LogInformation("Indexed PDF '{DocumentName}': {Chunks} chunks", 
                    documentName, chunksIndexed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index PDF: {FileName}", Path.GetFileName(pdfFile));
            }
        }

        _logger.LogInformation(
            "PDF indexing complete. Indexed {SuccessCount}/{TotalCount} files, total {TotalChunks} chunks",
            successCount, pdfFiles.Length, totalChunks);
    }

    /// <summary>
    /// Clean up file name to create a readable document name.
    /// </summary>
    private static string CleanDocumentName(string fileName)
    {
        // Remove common prefixes like "65b8c42763354" (hex IDs)
        var name = fileName;
        
        // If starts with hex-like prefix, remove it
        if (name.Length > 12 && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
        {
            var parts = name.Split(['_', '-'], 2);
            if (parts.Length > 1 && parts[0].All(char.IsLetterOrDigit) && parts[0].Length >= 10)
            {
                name = parts[1];
            }
        }

        // Replace underscores and hyphens with spaces
        name = name.Replace('_', ' ').Replace('-', ' ');
        
        // Capitalize first letter
        if (name.Length > 0)
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        return name.Trim();
    }
}
