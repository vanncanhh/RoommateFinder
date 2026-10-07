using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Common;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Application.Services;

/// <summary>Danh mục: đọc công khai (chỉ mục đang hoạt động) và quản trị (Admin, không xóa cứng — ẩn bằng IsActive).</summary>
public class CatalogService
{
    private readonly ICatalogRepository _catalog;
    private readonly IUnitOfWork _uow;

    public CatalogService(ICatalogRepository catalog, IUnitOfWork uow)
    {
        _catalog = catalog;
        _uow = uow;
    }

    public Task<List<AreaDto>> GetAreasAsync(bool includeInactive, CancellationToken ct = default) => _catalog.GetAreasAsync(includeInactive, ct);
    public Task<List<LandmarkDto>> GetLandmarksAsync(int? areaId, bool includeInactive, CancellationToken ct = default) => _catalog.GetLandmarksAsync(areaId, includeInactive, ct);
    public Task<List<AmenityDto>> GetAmenitiesAsync(bool includeInactive, CancellationToken ct = default) => _catalog.GetAmenitiesAsync(includeInactive, ct);

    public Task<List<ReportReasonDto>> GetReportReasonsAsync(string? appliesTo, bool includeInactive, CancellationToken ct = default)
    {
        if (appliesTo != null && appliesTo != ReasonAppliesTo.Post && appliesTo != ReasonAppliesTo.User)
            throw BusinessException.Validation("appliesTo chỉ nhận post hoặc user.");
        return _catalog.GetReportReasonsAsync(appliesTo, includeInactive, ct);
    }

    // ------------------------------------------------------------------ Khu vực

    public async Task<AreaDto> SaveAreaAsync(int? id, AreaUpsertRequest req, CancellationToken ct = default)
    {
        var name = Required(req.Name, 100, "Tên khu vực");
        var city = Required(req.City, 100, "Thành phố");
        var district = Optional(req.District, 100, "Quận/huyện");
        if (req.Latitude.HasValue != req.Longitude.HasValue) throw BusinessException.Validation("Cần nhập đủ cả vĩ độ và kinh độ.");
        ValidateCoordinates(req.Latitude, req.Longitude);
        if (await _catalog.AreaNameExistsAsync(name, city, id, ct)) throw BusinessException.Conflict("Khu vực đã tồn tại trong thành phố này.");

        Area area;
        if (id is { } areaId)
            area = await _catalog.GetAreaAsync(areaId, ct) ?? throw BusinessException.NotFound("Không tìm thấy khu vực.");
        else
            _catalog.AddArea(area = new Area());
        area.Name = name;
        area.City = city;
        area.District = district;
        area.Latitude = req.Latitude;
        area.Longitude = req.Longitude;
        area.IsActive = req.IsActive;
        await _uow.SaveChangesAsync(ct);
        return new AreaDto
        {
            AreaId = area.AreaId, Name = area.Name, District = area.District, City = area.City,
            Latitude = area.Latitude, Longitude = area.Longitude, IsActive = area.IsActive,
        };
    }

    // ------------------------------------------------------------------ Địa điểm mốc

    public async Task<LandmarkDto> SaveLandmarkAsync(int? id, LandmarkUpsertRequest req, CancellationToken ct = default)
    {
        var name = Required(req.Name, 150, "Tên địa điểm");
        if (!LandmarkType.All.Contains(req.Type)) throw BusinessException.Validation("Loại địa điểm không hợp lệ.");
        var address = Optional(req.Address, 255, "Địa chỉ");
        ValidateCoordinates(req.Latitude, req.Longitude);
        var area = await _catalog.GetAreaAsync(req.AreaId, ct) ?? throw BusinessException.Validation("Khu vực không tồn tại.");
        if (await _catalog.LandmarkNameExistsAsync(name, id, ct)) throw BusinessException.Conflict("Địa điểm đã tồn tại.");

        Landmark landmark;
        if (id is { } landmarkId)
            landmark = await _catalog.GetLandmarkAsync(landmarkId, ct) ?? throw BusinessException.NotFound("Không tìm thấy địa điểm.");
        else
            _catalog.AddLandmark(landmark = new Landmark());
        landmark.Name = name;
        landmark.Type = req.Type;
        landmark.AreaId = area.AreaId;
        landmark.Address = address;
        landmark.Latitude = req.Latitude;
        landmark.Longitude = req.Longitude;
        landmark.IsActive = req.IsActive;
        await _uow.SaveChangesAsync(ct);
        return new LandmarkDto
        {
            LandmarkId = landmark.LandmarkId, Name = landmark.Name, Type = landmark.Type, AreaId = area.AreaId, AreaName = area.Name,
            Address = landmark.Address, Latitude = landmark.Latitude, Longitude = landmark.Longitude, IsActive = landmark.IsActive,
        };
    }

    // ------------------------------------------------------------------ Tiện ích

    public async Task<AmenityDto> SaveAmenityAsync(int? id, AmenityUpsertRequest req, CancellationToken ct = default)
    {
        var name = Required(req.Name, 100, "Tên tiện ích");
        if (await _catalog.AmenityNameExistsAsync(name, id, ct)) throw BusinessException.Conflict("Tiện ích đã tồn tại.");
        Amenity amenity;
        if (id is { } amenityId)
            amenity = await _catalog.GetAmenityAsync(amenityId, ct) ?? throw BusinessException.NotFound("Không tìm thấy tiện ích.");
        else
            _catalog.AddAmenity(amenity = new Amenity());
        amenity.Name = name;
        amenity.IsActive = req.IsActive;
        try
        {
            await _uow.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            throw BusinessException.Conflict("Tiện ích đã tồn tại.");
        }
        return new AmenityDto { AmenityId = amenity.AmenityId, Name = amenity.Name, IsActive = amenity.IsActive };
    }

    // ------------------------------------------------------------------ Lý do báo cáo

    public async Task<ReportReasonDto> SaveReportReasonAsync(int? id, ReportReasonUpsertRequest req, CancellationToken ct = default)
    {
        var name = Required(req.Name, 150, "Tên lý do");
        if (!ReasonAppliesTo.All.Contains(req.AppliesTo)) throw BusinessException.Validation("Phạm vi áp dụng không hợp lệ.");
        if (await _catalog.ReportReasonNameExistsAsync(name, id, ct)) throw BusinessException.Conflict("Lý do báo cáo đã tồn tại.");
        ReportReason reason;
        if (id is { } reasonId)
            reason = await _catalog.GetReportReasonAsync(reasonId, ct) ?? throw BusinessException.NotFound("Không tìm thấy lý do báo cáo.");
        else
            _catalog.AddReportReason(reason = new ReportReason());
        reason.Name = name;
        reason.AppliesTo = req.AppliesTo;
        reason.IsActive = req.IsActive;
        await _uow.SaveChangesAsync(ct);
        return new ReportReasonDto { ReasonId = reason.ReasonId, Name = reason.Name, AppliesTo = reason.AppliesTo, IsActive = reason.IsActive };
    }

    // ------------------------------------------------------------------ Kiểm tra dữ liệu

    private static string Required(string? value, int max, string field)
    {
        var v = (value ?? "").Trim();
        if (v.Length == 0) throw BusinessException.Validation($"{field} không được để trống.");
        if (v.Length > max) throw BusinessException.Validation($"{field} tối đa {max} ký tự.");
        return v;
    }

    private static string? Optional(string? value, int max, string field)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Length > max) throw BusinessException.Validation($"{field} tối đa {max} ký tự.");
        return v;
    }

    private static void ValidateCoordinates(decimal? lat, decimal? lon)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180)
            throw BusinessException.Validation("Tọa độ không hợp lệ (vĩ độ −90..90, kinh độ −180..180).");
    }
}
