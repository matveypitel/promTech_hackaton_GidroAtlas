using GidroAtlas.Api.Abstractions;
using GidroAtlas.Api.Entities;
using GidroAtlas.Api.Infrastructure.Database;
using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace GidroAtlas.Api.Services;

public class WaterObjectService : IWaterObjectService
{
    private readonly ApplicationDbContext _context;

    public WaterObjectService(ApplicationDbContext context)
    {
        _context = context;
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
            query = query.Where(w => w.PassportDate >= filter.PassportDateFrom.Value);
        }

        if (filter.PassportDateTo.HasValue)
        {
            query = query.Where(w => w.PassportDate <= filter.PassportDateTo.Value);
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
        return (6 - technicalCondition) * 3 + passportAgeYears;
    }

    public PriorityLevel GetPriorityLevel(int priority)
    {
        return priority switch
        {
            >= 12 => PriorityLevel.High,
            >= 6 => PriorityLevel.Medium,
            _ => PriorityLevel.Low
        };
    }

    private WaterObjectDto MapToDto(WaterObject entity)
    {
        var priority = CalculatePriority(entity.TechnicalCondition, entity.PassportDate);
        
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
            PriorityLevel = GetPriorityLevel(priority)
        };
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
}
