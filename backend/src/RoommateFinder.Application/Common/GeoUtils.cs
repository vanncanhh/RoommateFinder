namespace RoommateFinder.Application.Common;

/// <summary>Tính khoảng cách đường chim bay (BR-01) — không dùng bản đồ số.</summary>
public static class GeoUtils
{
    private const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>Khung vĩ độ/kinh độ bao quanh một điểm — dùng để lọc thô trong SQL trước khi tính Haversine.</summary>
    public static GeoBox BoundingBox(double lat, double lon, double radiusKm)
    {
        var dLat = radiusKm / 111.32;
        var cos = Math.Cos(ToRad(lat));
        var dLon = radiusKm / (111.32 * Math.Max(cos, 0.01));
        return new GeoBox((decimal)(lat - dLat), (decimal)(lat + dLat), (decimal)(lon - dLon), (decimal)(lon + dLon));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}

public record GeoBox(decimal MinLat, decimal MaxLat, decimal MinLon, decimal MaxLon);
