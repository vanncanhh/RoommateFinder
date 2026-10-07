using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Api.Infrastructure;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;
using RoommateFinder.Domain.Constants;

namespace RoommateFinder.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[ServiceFilter(typeof(AuditLogFilter))]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AdminService _admin;
    private readonly CatalogService _catalog;

    public AdminController(AdminService admin, CatalogService catalog)
    {
        _admin = admin;
        _catalog = catalog;
    }

    // ---------------------------------------------------------------- Người dùng
    [HttpGet("users")]
    public Task<PagedResult<AdminUserDto>> Users([FromQuery] AdminUserQuery query, CancellationToken ct) => _admin.GetUsersAsync(query, ct);

    [HttpPut("users/{id:long}/role")]
    public Task<AdminUserDto> ChangeRole(long id, UpdateRoleRequest req, CancellationToken ct) =>
        _admin.ChangeRoleAsync(User.ToCurrentUser(), id, req, ct);

    [HttpPut("users/{id:long}/status")]
    public Task<AdminUserDto> ChangeStatus(long id, UpdateUserStatusRequest req, CancellationToken ct) =>
        _admin.ChangeStatusAsync(User.ToCurrentUser(), id, req, ct);

    // ---------------------------------------------------------------- Danh mục (không xóa cứng)
    [HttpGet("areas")]
    public Task<List<AreaDto>> Areas(CancellationToken ct) => _catalog.GetAreasAsync(true, ct);
    [HttpPost("areas")]
    public Task<AreaDto> CreateArea(AreaUpsertRequest req, CancellationToken ct) => _catalog.SaveAreaAsync(null, req, ct);
    [HttpPut("areas/{id:int}")]
    public Task<AreaDto> UpdateArea(int id, AreaUpsertRequest req, CancellationToken ct) => _catalog.SaveAreaAsync(id, req, ct);

    [HttpGet("landmarks")]
    public Task<List<LandmarkDto>> Landmarks([FromQuery] int? areaId, CancellationToken ct) => _catalog.GetLandmarksAsync(areaId, true, ct);
    [HttpPost("landmarks")]
    public Task<LandmarkDto> CreateLandmark(LandmarkUpsertRequest req, CancellationToken ct) => _catalog.SaveLandmarkAsync(null, req, ct);
    [HttpPut("landmarks/{id:int}")]
    public Task<LandmarkDto> UpdateLandmark(int id, LandmarkUpsertRequest req, CancellationToken ct) => _catalog.SaveLandmarkAsync(id, req, ct);

    [HttpGet("amenities")]
    public Task<List<AmenityDto>> Amenities(CancellationToken ct) => _catalog.GetAmenitiesAsync(true, ct);
    [HttpPost("amenities")]
    public Task<AmenityDto> CreateAmenity(AmenityUpsertRequest req, CancellationToken ct) => _catalog.SaveAmenityAsync(null, req, ct);
    [HttpPut("amenities/{id:int}")]
    public Task<AmenityDto> UpdateAmenity(int id, AmenityUpsertRequest req, CancellationToken ct) => _catalog.SaveAmenityAsync(id, req, ct);

    [HttpGet("report-reasons")]
    public Task<List<ReportReasonDto>> ReportReasons(CancellationToken ct) => _catalog.GetReportReasonsAsync(null, true, ct);
    [HttpPost("report-reasons")]
    public Task<ReportReasonDto> CreateReportReason(ReportReasonUpsertRequest req, CancellationToken ct) => _catalog.SaveReportReasonAsync(null, req, ct);
    [HttpPut("report-reasons/{id:int}")]
    public Task<ReportReasonDto> UpdateReportReason(int id, ReportReasonUpsertRequest req, CancellationToken ct) => _catalog.SaveReportReasonAsync(id, req, ct);

    // ---------------------------------------------------------------- Cấu hình & thống kê
    [HttpGet("configs")]
    public Task<List<SystemConfigDto>> Configs(CancellationToken ct) => _admin.GetConfigsAsync(ct);

    [HttpPut("configs/{key}")]
    public Task<SystemConfigDto> UpdateConfig(string key, UpdateConfigRequest req, CancellationToken ct) => _admin.UpdateConfigAsync(key, req, ct);

    [HttpGet("dashboard")]
    public Task<DashboardDto> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        _admin.GetDashboardAsync(from, to, ct);
}
