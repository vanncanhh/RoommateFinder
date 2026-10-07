namespace RoommateFinder.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int total) =>
        new() { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
}

public static class Paging
{
    public const int MaxPageSize = 50;
    /// <summary>Chặn OFFSET tràn số (page × pageSize vượt int) — trang lớn hơn thì trả trang rỗng hợp lệ.</summary>
    public const int MaxPage = 100_000;

    public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultSize = 12)
    {
        if (page < 1) page = 1;
        if (page > MaxPage) page = MaxPage;
        if (pageSize < 1) pageSize = defaultSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return (page, pageSize);
    }
}

/// <summary>Người đang gọi API, lấy từ JWT ở Controller.</summary>
public record CurrentUser(long UserId, string Role)
{
    public bool IsModerator => Role is Domain.Constants.Roles.Moderator or Domain.Constants.Roles.Admin;
    public bool IsAdmin => Role == Domain.Constants.Roles.Admin;
}

/// <summary>Tệp người dùng tải lên, tách khỏi IFormFile để Application không phụ thuộc ASP.NET.</summary>
public record FileUpload(string FileName, string ContentType, long Length, Stream Content);
