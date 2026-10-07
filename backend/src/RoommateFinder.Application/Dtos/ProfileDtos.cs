namespace RoommateFinder.Application.Dtos;

public class ProfileDto
{
    public long UserId { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public string? Gender { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string Role { get; init; } = "";
    public string? Occupation { get; init; }
    public string? SchoolOrCompany { get; init; }
    public int? LandmarkId { get; init; }
    public string? LandmarkName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public string? SleepSchedule { get; init; }
    public bool? IsSmoker { get; init; }
    public bool? HasPet { get; init; }
    public byte? CleanlinessLevel { get; init; }
    public decimal AvgRating { get; init; }
    public int ReviewCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>Không có Role, Status, AvgRating — chống over-posting.</summary>
public class UpdateProfileRequest
{
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Occupation { get; set; }
    public string? SchoolOrCompany { get; set; }
    public int? LandmarkId { get; set; }
    public string? Bio { get; set; }
    public string? SleepSchedule { get; set; }
    public bool? IsSmoker { get; set; }
    public bool? HasPet { get; set; }
    public byte? CleanlinessLevel { get; set; }
}

public class PublicProfileDto
{
    public long UserId { get; init; }
    public string FullName { get; init; } = "";
    public string? AvatarUrl { get; init; }
    public string? Gender { get; init; }
    public string? Occupation { get; init; }
    public string? SchoolOrCompany { get; init; }
    public string? Bio { get; init; }
    public string? SleepSchedule { get; init; }
    public bool? IsSmoker { get; init; }
    public bool? HasPet { get; init; }
    public byte? CleanlinessLevel { get; init; }
    public decimal AvgRating { get; init; }
    public int ReviewCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class ReviewDto
{
    public long ReviewId { get; init; }
    public long RequestId { get; init; }
    public long ReviewerId { get; init; }
    public string ReviewerName { get; init; } = "";
    public string? ReviewerAvatarUrl { get; init; }
    public long RevieweeId { get; init; }
    public string RevieweeName { get; init; } = "";
    public byte Rating { get; init; }
    public string? Comment { get; init; }
    public string? PostTitle { get; init; }
    public bool IsHidden { get; init; }
    public DateTime CreatedAt { get; init; }
}

public class CreateReviewRequest
{
    public long RequestId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}
