namespace RoommateFinder.Application.Dtos;

public class AreaDto
{
    public int AreaId { get; init; }
    public string Name { get; init; } = "";
    public string? District { get; init; }
    public string City { get; init; } = "";
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public bool IsActive { get; init; }
}

public class LandmarkDto
{
    public int LandmarkId { get; init; }
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public int AreaId { get; init; }
    public string AreaName { get; init; } = "";
    public string? Address { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public bool IsActive { get; init; }
}

public class AmenityDto
{
    public int AmenityId { get; init; }
    public string Name { get; init; } = "";
    public bool IsActive { get; init; }
}

public class ReportReasonDto
{
    public int ReasonId { get; init; }
    public string Name { get; init; } = "";
    public string AppliesTo { get; init; } = "";
    public bool IsActive { get; init; }
}

public class AreaUpsertRequest
{
    public string Name { get; set; } = "";
    public string? District { get; set; }
    public string City { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LandmarkUpsertRequest
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "school";
    public int AreaId { get; set; }
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AmenityUpsertRequest
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class ReportReasonUpsertRequest
{
    public string Name { get; set; } = "";
    public string AppliesTo { get; set; } = "both";
    public bool IsActive { get; set; } = true;
}
