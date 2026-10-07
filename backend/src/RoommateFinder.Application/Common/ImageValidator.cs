namespace RoommateFinder.Application.Common;

/// <summary>Kiểm tra ảnh bằng nội dung file (magic bytes), không tin vào đuôi file hay Content-Type (NFR-06).</summary>
public static class ImageValidator
{
    /// <summary>Trả về đuôi file chuẩn (".jpg", ".png", ".webp") hoặc null nếu không phải ảnh hợp lệ.</summary>
    public static async Task<string?> DetectExtensionAsync(Stream stream, CancellationToken ct = default)
    {
        var header = new byte[12];
        var read = 0;
        while (read < header.Length)
        {
            var n = await stream.ReadAsync(header.AsMemory(read, header.Length - read), ct);
            if (n == 0) break;
            read += n;
        }
        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
        if (read < 4) return null;

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return ".jpg";
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return ".png";
        if (read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P') return ".webp";
        return null;
    }
}
