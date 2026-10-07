namespace RoommateFinder.Domain.Entities;

public class User
{
    public long UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Role { get; set; } = "user";
    public string? Occupation { get; set; }
    public string? SchoolOrCompany { get; set; }
    public int? LandmarkId { get; set; }
    public Landmark? Landmark { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    // Thông tin sinh hoạt
    public string? SleepSchedule { get; set; }
    public bool? IsSmoker { get; set; }
    public bool? HasPet { get; set; }
    public byte? CleanlinessLevel { get; set; }

    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>active / suspended (có thời hạn SuspendedUntil) / banned (vĩnh viễn).</summary>
    public string Status { get; set; } = "active";
    public DateTime? SuspendedUntil { get; set; }

    /// <summary>Khóa tạm do đăng nhập sai, độc lập với Status.</summary>
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }

    /// <summary>Đổi mỗi khi đổi/đặt lại mật khẩu; JWT mang giá trị này, khác thì token cũ bị từ chối.</summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Khóa do vi phạm còn hiệu lực (banned, hoặc suspended chưa hết hạn) — dùng chung cho mọi nơi kiểm tra.</summary>
    public static bool IsLockedStatus(string status, DateTime? suspendedUntil, DateTime now) =>
        status == "banned" || (status == "suspended" && suspendedUntil > now);
}

public class PasswordResetToken
{
    public long TokenId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
