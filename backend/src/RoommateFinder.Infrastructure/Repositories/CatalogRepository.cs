using Microsoft.EntityFrameworkCore;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Application.Dtos;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;
using RoommateFinder.Infrastructure.Data;

namespace RoommateFinder.Infrastructure.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly AppDbContext _db;
    public CatalogRepository(AppDbContext db) => _db = db;

    public Task<List<AreaDto>> GetAreasAsync(bool includeInactive, CancellationToken ct = default) =>
        _db.Areas.AsNoTracking().Where(a => includeInactive || a.IsActive)
            .OrderBy(a => a.City).ThenBy(a => a.Name)
            .Select(a => new AreaDto
            {
                AreaId = a.AreaId, Name = a.Name, District = a.District, City = a.City,
                Latitude = a.Latitude, Longitude = a.Longitude, IsActive = a.IsActive,
            }).ToListAsync(ct);

    public Task<Area?> GetAreaAsync(int areaId, CancellationToken ct = default) =>
        _db.Areas.FirstOrDefaultAsync(a => a.AreaId == areaId, ct);

    public Task<bool> AreaNameExistsAsync(string name, string city, int? excludeId, CancellationToken ct = default) =>
        _db.Areas.AnyAsync(a => a.Name == name && a.City == city && a.AreaId != excludeId, ct);

    public void AddArea(Area area) => _db.Areas.Add(area);

    public Task<List<LandmarkDto>> GetLandmarksAsync(int? areaId, bool includeInactive, CancellationToken ct = default) =>
        _db.Landmarks.AsNoTracking()
            .Where(l => (includeInactive || l.IsActive) && (areaId == null || l.AreaId == areaId))
            .OrderBy(l => l.Name)
            .Select(l => new LandmarkDto
            {
                LandmarkId = l.LandmarkId, Name = l.Name, Type = l.Type, AreaId = l.AreaId, AreaName = l.Area.Name,
                Address = l.Address, Latitude = l.Latitude, Longitude = l.Longitude, IsActive = l.IsActive,
            }).ToListAsync(ct);

    public Task<Landmark?> GetLandmarkAsync(int landmarkId, CancellationToken ct = default) =>
        _db.Landmarks.FirstOrDefaultAsync(l => l.LandmarkId == landmarkId, ct);

    public Task<bool> LandmarkNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
        _db.Landmarks.AnyAsync(l => l.Name == name && l.LandmarkId != excludeId, ct);

    public void AddLandmark(Landmark landmark) => _db.Landmarks.Add(landmark);

    public Task<List<AmenityDto>> GetAmenitiesAsync(bool includeInactive, CancellationToken ct = default) =>
        _db.Amenities.AsNoTracking().Where(a => includeInactive || a.IsActive).OrderBy(a => a.AmenityId)
            .Select(a => new AmenityDto { AmenityId = a.AmenityId, Name = a.Name, IsActive = a.IsActive }).ToListAsync(ct);

    public Task<Amenity?> GetAmenityAsync(int amenityId, CancellationToken ct = default) =>
        _db.Amenities.FirstOrDefaultAsync(a => a.AmenityId == amenityId, ct);

    public Task<bool> AmenityNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
        _db.Amenities.AnyAsync(a => a.Name == name && a.AmenityId != excludeId, ct);

    public Task<List<int>> GetActiveAmenityIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = ids.ToList();
        return list.Count == 0
            ? Task.FromResult(new List<int>())
            : _db.Amenities.Where(a => a.IsActive && list.Contains(a.AmenityId)).Select(a => a.AmenityId).ToListAsync(ct);
    }

    public void AddAmenity(Amenity amenity) => _db.Amenities.Add(amenity);

    public Task<List<ReportReasonDto>> GetReportReasonsAsync(string? appliesTo, bool includeInactive, CancellationToken ct = default) =>
        _db.ReportReasons.AsNoTracking()
            .Where(r => (includeInactive || r.IsActive) && (appliesTo == null || r.AppliesTo == appliesTo || r.AppliesTo == ReasonAppliesTo.Both))
            .OrderBy(r => r.ReasonId)
            .Select(r => new ReportReasonDto { ReasonId = r.ReasonId, Name = r.Name, AppliesTo = r.AppliesTo, IsActive = r.IsActive })
            .ToListAsync(ct);

    public Task<ReportReason?> GetReportReasonAsync(int reasonId, CancellationToken ct = default) =>
        _db.ReportReasons.FirstOrDefaultAsync(r => r.ReasonId == reasonId, ct);

    public Task<bool> ReportReasonNameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
        _db.ReportReasons.AnyAsync(r => r.Name == name && r.ReasonId != excludeId, ct);

    public void AddReportReason(ReportReason reason) => _db.ReportReasons.Add(reason);
}
