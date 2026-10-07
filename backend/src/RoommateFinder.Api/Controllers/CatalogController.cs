using Microsoft.AspNetCore.Mvc;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Application.Services;

namespace RoommateFinder.Api.Controllers;

/// <summary>Danh mục công khai — chỉ trả mục đang hoạt động.</summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly CatalogService _catalog;
    public CatalogController(CatalogService catalog) => _catalog = catalog;

    [HttpGet("areas")]
    public Task<List<AreaDto>> Areas(CancellationToken ct) => _catalog.GetAreasAsync(false, ct);

    [HttpGet("landmarks")]
    public Task<List<LandmarkDto>> Landmarks([FromQuery] int? areaId, CancellationToken ct) => _catalog.GetLandmarksAsync(areaId, false, ct);

    [HttpGet("amenities")]
    public Task<List<AmenityDto>> Amenities(CancellationToken ct) => _catalog.GetAmenitiesAsync(false, ct);

    [HttpGet("report-reasons")]
    public Task<List<ReportReasonDto>> ReportReasons([FromQuery] string? appliesTo, CancellationToken ct) =>
        _catalog.GetReportReasonsAsync(appliesTo, false, ct);
}

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });
}
