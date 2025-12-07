using GidroAtlas.Shared.DTOs;
using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Web.Services;

public static class MapMockDataService
{
    private static readonly Random _random = new();

    public static List<WaterObjectDto> GetMockData(int count = 30)
    {
        var objects = new List<WaterObjectDto>();
        for (int i = 0; i < count; i++)
        {
            // Only generate HydroTechnicalStructures as requested
            objects.Add(GenerateRandomStructure());
        }
        return objects;
    }

    private static WaterObjectDto GenerateRandomStructure()
    {
        // Kazakhstan coordinates semi-realistic bounding box
        // Latitude: ~41.0 to ~54.0
        // Longitude: ~47.0 to ~86.0
        
        var lat = 41.0 + (_random.NextDouble() * 13.0);
        var lng = 47.0 + (_random.NextDouble() * 39.0);
        
        var region = _random.Next(0, 5) switch {
            0 => "Акмолинская область",
            1 => "Алматинская область",
            2 => "Карагандинская область",
            3 => "Туркестанская область",
            _ => "Восточно-Казахстанская область"
        };
        
        return new WaterObjectDto
        {
            Id = Guid.NewGuid(),
            Name = $"Гидроузел №{_random.Next(100, 999)}",
            Region = region,
            ResourceType = ResourceType.HydroTechnicalStructure,
            WaterType = WaterType.Fresh,
            HasFauna = false,
            PassportDate = DateTime.Now.AddYears(-_random.Next(1, 30)),
            TechnicalCondition = _random.Next(1, 6), // 1 to 5
            Latitude = (float)lat,
            Longitude = (float)lng,
            PdfUrl = "#",
            Priority = _random.Next(1, 100),
            PriorityLevel = (PriorityLevel)_random.Next(0, 3),
            AttentionProbability = _random.NextDouble()
        };
    }
}
