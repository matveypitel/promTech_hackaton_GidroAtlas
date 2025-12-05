using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;
using GidroAtlas.Web.Interfaces;
using System.Net.Http.Headers;

namespace GidroAtlas.Web.Services;

/// <summary>
/// Implementation of water objects API service
/// </summary>
public class WaterObjectApiService : IWaterObjectApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WaterObjectApiService> _logger;

    public WaterObjectApiService(HttpClient httpClient, ILogger<WaterObjectApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public void SetAuthToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<PagedResponseDto<WaterObjectDto>?> GetAllAsync(WaterObjectFilterDto filter)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(filter.Region))
                queryParams.Add($"region={Uri.EscapeDataString(filter.Region)}");
            
            if (filter.ResourceType.HasValue)
                queryParams.Add($"resourceType={filter.ResourceType}");
            
            if (filter.WaterType.HasValue)
                queryParams.Add($"waterType={filter.WaterType}");
            
            if (filter.HasFauna.HasValue)
                queryParams.Add($"hasFauna={filter.HasFauna}");
            
            if (filter.PassportDateFrom.HasValue)
                queryParams.Add($"passportDateFrom={filter.PassportDateFrom:O}");
            
            if (filter.PassportDateTo.HasValue)
                queryParams.Add($"passportDateTo={filter.PassportDateTo:O}");
            
            if (filter.TechnicalCondition.HasValue)
                queryParams.Add($"technicalCondition={filter.TechnicalCondition}");
            
            if (!string.IsNullOrEmpty(filter.SearchQuery))
                queryParams.Add($"search={Uri.EscapeDataString(filter.SearchQuery)}");
            
            if (filter.SortBy.HasValue)
                queryParams.Add($"sortBy={filter.SortBy}");
            
            queryParams.Add($"sortDesc={filter.SortDescending}");
            queryParams.Add($"page={filter.Page}");
            queryParams.Add($"pageSize={filter.PageSize}");
            
            var queryString = string.Join("&", queryParams);
            var url = $"api/waterobjects?{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PagedResponseDto<WaterObjectDto>>();
            }
            
            _logger.LogWarning("GetAll failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting water objects");
            return null;
        }
    }

    public async Task<WaterObjectDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/waterobjects/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<WaterObjectDto>();
            }
            
            _logger.LogWarning("GetById failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting water object by id: {Id}", id);
            return null;
        }
    }

    public async Task<List<string>?> GetRegionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/waterobjects/regions");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<string>>();
            }
            
            _logger.LogWarning("GetRegions failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regions");
            return null;
        }
    }

    public async Task<PagedResponseDto<WaterObjectDto>?> GetPrioritiesAsync(
        SortField? sortBy = null, 
        bool sortDesc = true, 
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (sortBy.HasValue)
                queryParams.Add($"sortBy={sortBy}");
            
            queryParams.Add($"sortDesc={sortDesc}");
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");
            
            var queryString = string.Join("&", queryParams);
            var url = $"api/waterobjects/priorities?{queryString}";
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PagedResponseDto<WaterObjectDto>>();
            }
            
            _logger.LogWarning("GetPriorities failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priorities");
            return null;
        }
    }

    public async Task<PrioritySummaryDto?> GetPrioritySummaryAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/waterobjects/priorities/summary");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PrioritySummaryDto>();
            }
            
            _logger.LogWarning("GetPrioritySummary failed with status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting priority summary");
            return null;
        }
    }
}
