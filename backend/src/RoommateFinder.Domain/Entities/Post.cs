namespace RoommateFinder.Domain.Entities;

public class Post
{
    public long PostId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>has_room: Price = giá thuê mỗi người; seeking: Price = ngân sách tối đa mỗi người.</summary>
    public string PostType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int CurrentOccupants { get; set; }

    /// <summary>has_room: số chỗ còn trống; seeking: số người trong nhóm đang tìm chỗ.</summary>
    public int NeededOccupants { get; set; } = 1;
    public string? PreferredGender { get; set; }

    public string Status { get; set; } = "pending";
    public string? RejectReason { get; set; }
    public long? ModeratedBy { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public int ExtendCount { get; set; }
    public int ViewCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Thời điểm gửi duyệt gần nhất (tạo hoặc sửa) — dùng tính thời gian duyệt trung bình.</summary>
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }
    /// <summary>rowversion: chặn ghi đè khi chủ tin sửa và kiểm duyệt viên duyệt/ẩn cùng lúc.</summary>
    public byte[] RowVersion { get; set; } = null!;

    public List<PostImage> Images { get; set; } = new();
    public List<PostAmenity> PostAmenities { get; set; } = new();
}

public class PostImage
{
    public long ImageId { get; set; }
    public long PostId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public int SortOrder { get; set; }
}

public class PostAmenity
{
    public long PostId { get; set; }
    public int AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;
}

public class SavedPost
{
    public long SavedId { get; set; }
    public long UserId { get; set; }
    public long PostId { get; set; }
    public Post Post { get; set; } = null!;
    public DateTime SavedAt { get; set; }
}
