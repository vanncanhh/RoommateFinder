namespace RoommateFinder.Domain.Entities;

public class Area
{
    public int AreaId { get; set; }
    public string Name { get; set; } = null!;
    public string? District { get; set; }
    public string City { get; set; } = null!;
    /// <summary>Tâm khu vực, dùng khi tin đăng không có tọa độ riêng (BR-01).</summary>
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Địa điểm mốc (trường học, công ty) có sẵn tọa độ — dùng cho bộ lọc khoảng cách.</summary>
public class Landmark
{
    public int LandmarkId { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string? Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Amenity
{
    public int AmenityId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class ReportReason
{
    public int ReasonId { get; set; }
    public string Name { get; set; } = null!;
    public string AppliesTo { get; set; } = "both";
    public bool IsActive { get; set; } = true;
}

public class SystemConfig
{
    public string ConfigKey { get; set; } = null!;
    public string ConfigValue { get; set; } = null!;
    public string? Description { get; set; }
}
