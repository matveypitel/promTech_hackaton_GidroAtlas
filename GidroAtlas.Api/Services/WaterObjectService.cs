using GidroAtlas.Api.Abstractions;
using GidroAtlas.Api.Entities;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Api.Infrastructure.ML;
using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;
using GidroAtlas.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace GidroAtlas.Api.Services;

public class WaterObjectService : IWaterObjectService
{
    private readonly ApplicationDbContext _context;
    private readonly PredictionService _predictionService;

    public WaterObjectService(ApplicationDbContext context, PredictionService predictionService)
    {
        _context = context;
        _predictionService = predictionService;
    }

    public async Task<PagedResponseDto<WaterObjectDto>> GetAllAsync(WaterObjectFilterDto filter)
    {
        var query = _context.WaterObjects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Region))
        {
            query = query.Where(w => w.Region.Contains(filter.Region));
        }

        if (filter.ResourceType.HasValue)
        {
            query = query.Where(w => w.ResourceType == filter.ResourceType.Value);
        }

        if (filter.WaterType.HasValue)
        {
            query = query.Where(w => w.WaterType == filter.WaterType.Value);
        }

        if (filter.HasFauna.HasValue)
        {
            query = query.Where(w => w.HasFauna == filter.HasFauna.Value);
        }

        if (filter.PassportDateFrom.HasValue)
        {
            var dateFrom = filter.PassportDateFrom.Value.Kind == DateTimeKind.Utc 
                ? filter.PassportDateFrom.Value 
                : DateTime.SpecifyKind(filter.PassportDateFrom.Value, DateTimeKind.Utc);
            query = query.Where(w => w.PassportDate >= dateFrom);
        }

        if (filter.PassportDateTo.HasValue)
        {
            var dateTo = filter.PassportDateTo.Value.Kind == DateTimeKind.Utc 
                ? filter.PassportDateTo.Value 
                : DateTime.SpecifyKind(filter.PassportDateTo.Value, DateTimeKind.Utc);
            query = query.Where(w => w.PassportDate <= dateTo);
        }

        if (filter.TechnicalCondition.HasValue)
        {
            query = query.Where(w => w.TechnicalCondition == filter.TechnicalCondition.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var searchLower = filter.SearchQuery.ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(searchLower) || 
                                     w.Region.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResponseDto<WaterObjectDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<WaterObjectDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.WaterObjects.FindAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<string>> GetRegionsAsync()
    {
        return await _context.WaterObjects
            .Select(w => w.Region)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();
    }

    public int CalculatePriority(int technicalCondition, DateTime passportDate)
    {
        var passportAgeYears = (DateTime.UtcNow - passportDate).Days / 365;
        return (PriorityConstants.TechnicalConditionBase - technicalCondition) * PriorityConstants.TechnicalConditionMultiplier + passportAgeYears;
    }

    /// <summary>
    /// Calculates priority (1-5) using ML model's attention probability.
    /// 5 = highest priority (needs attention), 1 = lowest priority.
    /// If ML model is unavailable, falls back to formula-based calculation.
    /// </summary>
    public int CalculateMlPriority(WaterObject entity)
    {
        var attentionProbability = _predictionService.GetAttentionProbability(entity);
        
        if (attentionProbability.HasValue)
        {
            // Convert probability (0.0-1.0) to priority (1-5)
            // Higher probability = higher priority (needs more attention)
            // 0.0-0.2 -> 1, 0.2-0.4 -> 2, 0.4-0.6 -> 3, 0.6-0.8 -> 4, 0.8-1.0 -> 5
            // Formula: priority = floor(probability * 5) + 1, clamped to 1-5
            var priority = (int)Math.Floor(attentionProbability.Value * 5) + 1;
            return Math.Clamp(priority, 1, 5);
        }
        
        // Fallback: convert old formula-based priority to 1-5 scale
        var oldPriority = CalculatePriority(entity.TechnicalCondition, entity.PassportDate);
        return ConvertOldPriorityToNewScale(oldPriority);
    }

    /// <summary>
    /// Converts old priority score to new 1-5 scale.
    /// </summary>
    private static int ConvertOldPriorityToNewScale(int oldPriority)
    {
        return oldPriority switch
        {
            >= PriorityConstants.HighPriorityThreshold => 5,
            >= PriorityConstants.MediumPriorityThreshold => 3,
            _ => 1
        };
    }

    public PriorityLevel GetPriorityLevel(int priority)
    {
        // Now priority is 1-5 scale (ML-based)
        return priority switch
        {
            >= 4 => PriorityLevel.High,    // 4-5
            >= 2 => PriorityLevel.Medium,  // 2-3
            _ => PriorityLevel.Low         // 1
        };
    }

    private WaterObjectDto MapToDto(WaterObject entity)
    {
        // Use ML-based priority (1-5 scale)
        var priority = CalculateMlPriority(entity);
        var attentionProbability = _predictionService.GetAttentionProbability(entity);
        
        return new WaterObjectDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Region = entity.Region,
            ResourceType = entity.ResourceType,
            WaterType = entity.WaterType,
            HasFauna = entity.HasFauna,
            PassportDate = entity.PassportDate,
            TechnicalCondition = entity.TechnicalCondition,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            PdfUrl = entity.PdfUrl,
            Priority = priority,
            PriorityLevel = GetPriorityLevel(priority),
            AttentionProbability = attentionProbability
        };
    }

    public async Task<ObjectPriorityDto?> GetObjectPriorityAsync(Guid id)
    {
        var entity = await _context.WaterObjects.FindAsync(id);
        if (entity == null) return null;

        // Use ML-based priority (1-5 scale)
        var priority = CalculateMlPriority(entity);
        var passportAgeYears = (DateTime.UtcNow - entity.PassportDate).Days / 365;
        var attentionProbability = _predictionService.GetAttentionProbability(entity);

        return new ObjectPriorityDto
        {
            ObjectId = entity.Id,
            Name = entity.Name,
            PriorityScore = priority,
            PriorityLevel = GetPriorityLevel(priority),
            AttentionProbability = attentionProbability,
            TechnicalCondition = entity.TechnicalCondition,
            PassportAgeYears = passportAgeYears,
            PassportDate = entity.PassportDate,
            IsMlPredictionAvailable = _predictionService.IsModelAvailable
        };
    }

    public async Task<WaterObjectDto?> UpdateAsync(Guid id, UpdateWaterObjectDto updateDto)
    {
        var entity = await _context.WaterObjects.FindAsync(id);
        if (entity == null) return null;

        // Update entity properties
        entity.Name = updateDto.Name;
        entity.Region = updateDto.Region;
        entity.ResourceType = updateDto.ResourceType;
        entity.WaterType = updateDto.WaterType;
        entity.HasFauna = updateDto.HasFauna;
        entity.PassportDate = updateDto.PassportDate.Kind == DateTimeKind.Utc 
            ? updateDto.PassportDate 
            : DateTime.SpecifyKind(updateDto.PassportDate, DateTimeKind.Utc);
        entity.TechnicalCondition = updateDto.TechnicalCondition;
        entity.Latitude = updateDto.Latitude;
        entity.Longitude = updateDto.Longitude;

        // Recalculate priority using ML model (1-5 scale)
        entity.Priority = CalculateMlPriority(entity);

        await _context.SaveChangesAsync();

        return MapToDto(entity);
    }

    private static IQueryable<WaterObject> ApplySorting(IQueryable<WaterObject> query, SortField? sortBy, bool descending)
    {
        return sortBy switch
        {
            SortField.Name => descending ? query.OrderByDescending(w => w.Name) : query.OrderBy(w => w.Name),
            SortField.Region => descending ? query.OrderByDescending(w => w.Region) : query.OrderBy(w => w.Region),
            SortField.ResourceType => descending ? query.OrderByDescending(w => w.ResourceType) : query.OrderBy(w => w.ResourceType),
            SortField.WaterType => descending ? query.OrderByDescending(w => w.WaterType) : query.OrderBy(w => w.WaterType),
            SortField.HasFauna => descending ? query.OrderByDescending(w => w.HasFauna) : query.OrderBy(w => w.HasFauna),
            SortField.PassportDate => descending ? query.OrderByDescending(w => w.PassportDate) : query.OrderBy(w => w.PassportDate),
            SortField.TechnicalCondition => descending ? query.OrderByDescending(w => w.TechnicalCondition) : query.OrderBy(w => w.TechnicalCondition),
            SortField.Priority => descending ? query.OrderByDescending(w => w.Priority) : query.OrderBy(w => w.Priority),
            _ => query.OrderBy(w => w.Name)
        };
    }

    public async Task<WaterObjectDto> CreateAsync(CreateWaterObjectDto createDto)
    {
        var passportDate = createDto.PassportDate.Kind == DateTimeKind.Utc 
            ? createDto.PassportDate 
            : DateTime.SpecifyKind(createDto.PassportDate, DateTimeKind.Utc);

        // Create entity first (needed for ML prediction)
        var entity = new WaterObject
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Region = createDto.Region,
            ResourceType = createDto.ResourceType,
            WaterType = createDto.WaterType,
            HasFauna = createDto.HasFauna,
            PassportDate = passportDate,
            TechnicalCondition = createDto.TechnicalCondition,
            Latitude = createDto.Latitude,
            Longitude = createDto.Longitude,
            PdfUrl = createDto.PdfUrl,
            Priority = 1 // Temporary, will be calculated below
        };

        // Calculate priority using ML model (1-5 scale) - cannot be set manually
        entity.Priority = CalculateMlPriority(entity);

        _context.WaterObjects.Add(entity);
        await _context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.WaterObjects.FindAsync(id);
        if (entity == null) return false;

        _context.WaterObjects.Remove(entity);
        await _context.SaveChangesAsync();

        return true;
    }
}

