namespace GidroAtlas.Api.Infrastructure.Chat;

/// <summary>
/// Background service that automatically indexes water objects when the application starts.
/// Waits for Ollama to be available before starting indexing.
/// </summary>
public class IndexingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IndexingBackgroundService> _logger;
    
    private const int MaxRetries = 30; // Max 5 minutes waiting for Ollama (30 * 10s)
    private const int RetryDelaySeconds = 10;

    public IndexingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<IndexingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        
        _logger.LogInformation("IndexingBackgroundService started. Waiting for Ollama to be ready...");

        using var scope = _serviceProvider.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();

        // Wait for Ollama embedding service to be available
        var ollamaReady = false;
        for (var i = 0; i < MaxRetries && !stoppingToken.IsCancellationRequested; i++)
        {
            try
            {
                ollamaReady = await embeddingService.IsAvailableAsync(stoppingToken);
                if (ollamaReady)
                {
                    _logger.LogInformation("Ollama embedding service is ready!");
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Ollama not ready yet, attempt {Attempt}/{MaxRetries}", i + 1, MaxRetries);
            }

            _logger.LogInformation("Waiting for Ollama... attempt {Attempt}/{MaxRetries}", i + 1, MaxRetries);
            await Task.Delay(TimeSpan.FromSeconds(RetryDelaySeconds), stoppingToken);
        }

        if (!ollamaReady)
        {
            _logger.LogWarning("Ollama embedding service is not available after {MaxRetries} attempts. " +
                              "Indexing will not be performed automatically. " +
                              "Use POST /api/chat/index to trigger indexing manually.", MaxRetries);
            return;
        }

        // Check if indexing is needed
        try
        {
            var status = await chatService.GetStatusAsync(stoppingToken);
            
            if (status.IndexedObjectsCount > 0)
            {
                _logger.LogInformation("Found {Count} already indexed water objects. Skipping auto-indexing.", 
                    status.IndexedObjectsCount);
                return;
            }

            _logger.LogInformation("No indexed water objects found. Starting automatic indexing...");
            
            var indexedCount = await chatService.IndexAllWaterObjectsAsync(stoppingToken);
            
            _logger.LogInformation("Automatic indexing complete. Indexed {Count} water objects.", indexedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic indexing. Use POST /api/chat/index to retry.");
        }
    }
}
