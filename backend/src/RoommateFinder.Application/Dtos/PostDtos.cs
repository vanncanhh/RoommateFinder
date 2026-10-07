namespace RoommateFinder.Application.Dtos;

/// <summary>Dữ liệu form đăng/sửa tin (ảnh truyền riêng dưới dạng FileUpload).</summary>
public class PostFormData
{
    public string PostType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int AreaId { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int CurrentOccupants { get; set; }
    public int NeededOccupants { get; set; } = 1;
    public string? PreferredGender { get; set; }
    public List<int> AmenityIds { get; set; } = new();
    /// <summary>Chỉ dùng khi sửa: Id các ảnh cũ muốn giữ lại.</summary>
    public List<long> KeepImageIds { get; set; } = new();
}

public class PostSearchQuery
{
    public string? Keyword { get; set; }
    public string? PostType { get; set; }
    public int? AreaId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    /// <summary>Giới tính người tìm: lấy tin có PreferredGender bằng giá trị này hoặc 'any'.</summary>
    public string? Gender { get; set; }
    public int? MinNeeded { get; set; }
    public int? LandmarkId { get; set; }
    public double? RadiusKm { get; set; }
    public List<int>? AmenityIds { get; set; }
    /// <summary>newest | price_asc | price_desc | distance</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class PostSummaryDto
{
    public long PostId { get; init; }
    public string PostType { get; init; } = "";
    public string Title { get; init; } = "";
    public decimal Price { get; init; }
    public int AreaId { get; init; }
    public string AreaName { get; init; } = "";
    public string? District { get; init; }
    public string City { get; init; } = "";
    public string? Address { get; init; }
    public int CurrentOccupants { get; init; }
    public int NeededOccupants { get; init; }
    public string? PreferredGender { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string Status { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpiredAt { get; init; }
    public int ViewCount { get; init; }
    public long OwnerId { get; init; }
    public string OwnerName { get; init; } = "";
    public string? OwnerAvatarUrl { get; init; }
    public decimal OwnerRating { get; init; }
    /// <summary>Tọa độ hiệu lực: của tin, hoặc tâm khu vực nếu tin không có.</summary>
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public double? DistanceKm { get; set; }

    // Chỉ có giá trị ở danh sách "Tin của tôi" / hàng đợi duyệt
    public string? RejectReason { get; init; }
    public int ExtendCount { get; init; }
    public int PendingRequestCount { get; init; }
}

public class PostImageDto
{
    public long ImageId { get; init; }
    public string ImageUrl { get; init; } = "";
    public int SortOrder { get; init; }
}

public class PostDetailDto
{
    public long PostId { get; init; }
    public string PostType { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public int AreaId { get; init; }
    public string AreaName { get; init; } = "";
    public string? District { get; init; }
    public string City { get; init; } = "";
    public string? Address { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int CurrentOccupants { get; init; }
    public int NeededOccupants { get; init; }
    public string? PreferredGender { get; init; }
    public string Status { get; init; } = "";
    /// <summary>Chỉ trả cho chủ tin và kiểm duyệt viên (Service xóa với người khác).</summary>
    public string? RejectReason { get; set; }
    public int ExtendCount { get; init; }
    public int ViewCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? ExpiredAt { get; init; }
    public List<PostImageDto> Images { get; init; } = new();
    public List<AmenityDto> Amenities { get; init; } = new();

    public long OwnerId { get; init; }
    public string OwnerName { get; init; } = "";
    public string? OwnerAvatarUrl { get; init; }
    public string? OwnerGender { get; init; }
    public string? OwnerOccupation { get; init; }
    public decimal OwnerRating { get; init; }
    public int OwnerReviewCount { get; init; }
    /// <summary>Chỉ có giá trị khi người xem là chủ tin, moderator/admin, hoặc đã được chấp nhận kết nối (NFR-05).</summary>
    public string? OwnerPhone { get; set; }

    // Thông tin theo người xem
    public bool IsOwner { get; set; }
    public bool IsSaved { get; set; }
    public long? MyRequestId { get; set; }
    public string? MyRequestStatus { get; set; }
    public bool CanSendRequest { get; set; }
}
