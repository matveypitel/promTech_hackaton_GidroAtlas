using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Api.Services;

public interface IWaterObjectService
{
    Task<PagedResponseDto<WaterObjectDto>> GetAllAsync(WaterObjectFilterDto filter);
    Task<WaterObjectDto?> GetByIdAsync(Guid id);
    Task<List<string>> GetRegionsAsync();
    int CalculatePriority(int technicalCondition, DateTime passportDate);
    PriorityLevel GetPriorityLevel(int priority);
}
