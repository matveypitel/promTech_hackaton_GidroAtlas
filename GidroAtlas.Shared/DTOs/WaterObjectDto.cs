using GidroAtlas.Shared.Enums;

namespace GidroAtlas.Shared.DTOs;

public class WaterObjectDto
{
    public required string Name { get; set; }

    public required string Region { get; set; }

    public ResourceType ResourceType { get; set; }

    public WaterType WaterType { get; set; }

    public bool HasFauna { get; set; }

    public DateTime PassportDate { get; set; }

    public int TechnicalCondition { get; set; }

    public float Latitude { get; set; }

    public float Longitude { get; set; }

    public required string PdfUrl { get; set; }

    public int Priority { get; set; }
}
