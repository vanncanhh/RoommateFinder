using RoommateFinder.Application.Common;
using Xunit;

namespace RoommateFinder.UnitTests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdef12", true)]
    [InlineData("User@1234", true)]
    [InlineData("abcdef12", false)]   // thiếu chữ hoa
    [InlineData("ABCDEF12", false)]   // thiếu chữ thường
    [InlineData("Abcdefgh", false)]   // thiếu chữ số
    [InlineData("Ab1", false)]        // quá ngắn
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStrong(string? password, bool expected) => Assert.Equal(expected, PasswordPolicy.IsStrong(password));
}

public class GeoUtilsTests
{
    [Fact]
    public void Haversine_HanoiToHoChiMinh_About1140Km()
    {
        // Hồ Hoàn Kiếm → Nhà thờ Đức Bà: khoảng 1.140 km đường chim bay
        var km = GeoUtils.HaversineKm(21.0285, 105.8542, 10.7798, 106.6990);
        Assert.InRange(km, 1130, 1150);
    }

    [Fact]
    public void Haversine_SamePoint_IsZero() => Assert.Equal(0, GeoUtils.HaversineKm(21.03, 105.78, 21.03, 105.78), 6);

    [Fact]
    public void Haversine_OneKmNorth()
    {
        // 1 km theo kinh tuyến ≈ 0,008993°
        var km = GeoUtils.HaversineKm(21.0, 105.8, 21.0 + 1 / 111.195, 105.8);
        Assert.InRange(km, 0.99, 1.01);
    }

    [Fact]
    public void BoundingBox_ContainsPointsWithinRadius()
    {
        var box = GeoUtils.BoundingBox(21.0050, 105.8430, 3);
        // Điểm cách ~2,9 km về phía đông vẫn nằm trong khung
        const double east = 105.8430 + 2.9 / (111.32 * 0.9334);
        Assert.True((decimal)east <= box.MaxLon);
        Assert.True(box.MinLat < 21.0050m && box.MaxLat > 21.0050m);
    }
}

public class ImageValidatorTests
{
    [Fact]
    public async Task DetectsPng() =>
        Assert.Equal(".png", await ImageValidator.DetectExtensionAsync(new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 })));

    [Fact]
    public async Task DetectsJpeg() =>
        Assert.Equal(".jpg", await ImageValidator.DetectExtensionAsync(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 })));

    [Fact]
    public async Task DetectsWebp() =>
        Assert.Equal(".webp", await ImageValidator.DetectExtensionAsync(new MemoryStream("RIFF\0\0\0\0WEBPVP8 "u8.ToArray())));

    [Fact]
    public async Task RejectsExecutableRenamedAsImage() =>
        Assert.Null(await ImageValidator.DetectExtensionAsync(new MemoryStream("MZ\u0090\0 not an image"u8.ToArray())));

    [Fact]
    public async Task RewindsStreamAfterCheck()
    {
        var stream = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4, 5, 6, 7, 8 });
        await ImageValidator.DetectExtensionAsync(stream);
        Assert.Equal(0, stream.Position);
    }
}
