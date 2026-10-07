using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoommateFinder.Application.Abstractions;
using RoommateFinder.Domain.Constants;
using RoommateFinder.Domain.Entities;

namespace RoommateFinder.Infrastructure.Data;

/// <summary>
/// Dữ liệu demo cho môi trường Development (giai đoạn 2.9 / 6.2). Chỉ chạy khi CSDL chưa có người dùng.
/// Tọa độ khu vực và trường học là giá trị gần đúng, đủ cho demo lọc khoảng cách — không dùng cho mục đích khác.
/// </summary>
public class DemoDataSeeder
{
    public const string AdminPassword = "Admin@123";
    public const string ModeratorPassword = "Mod@12345";
    public const string UserPassword = "User@1234";

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(AppDbContext db, IPasswordHasher hasher, IDateTimeProvider clock, ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _hasher = hasher;
        _clock = clock;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(ct)) return;
        var now = _clock.UtcNow;

        // ---------------------------------------------------------------- Khu vực (Hà Nội)
        var areas = new[]
        {
            Area("Cầu Giấy", 21.0362m, 105.7906m), Area("Đống Đa", 21.0181m, 105.8295m),
            Area("Hai Bà Trưng", 21.0060m, 105.8570m), Area("Thanh Xuân", 20.9937m, 105.8126m),
            Area("Hoàng Mai", 20.9745m, 105.8633m), Area("Ba Đình", 21.0340m, 105.8140m),
            Area("Nam Từ Liêm", 21.0120m, 105.7650m), Area("Bắc Từ Liêm", 21.0700m, 105.7600m),
            Area("Hà Đông", 20.9714m, 105.7788m), Area("Tây Hồ", 21.0700m, 105.8180m),
        };
        _db.Areas.AddRange(areas);
        await _db.SaveChangesAsync(ct);
        Area A(string name) => areas.First(a => a.Name == name);

        // ---------------------------------------------------------------- Địa điểm mốc
        var landmarks = new[]
        {
            Landmark("Đại học Quốc gia Hà Nội (Xuân Thủy)", LandmarkType.School, A("Cầu Giấy"), 21.0380m, 105.7826m),
            Landmark("Đại học Sư phạm Hà Nội", LandmarkType.School, A("Cầu Giấy"), 21.0370m, 105.7830m),
            Landmark("Đại học Thương mại", LandmarkType.School, A("Cầu Giấy"), 21.0370m, 105.7750m),
            Landmark("Đại học Bách khoa Hà Nội", LandmarkType.School, A("Hai Bà Trưng"), 21.0050m, 105.8430m),
            Landmark("Đại học Kinh tế Quốc dân", LandmarkType.School, A("Hai Bà Trưng"), 20.9990m, 105.8450m),
            Landmark("Đại học Xây dựng Hà Nội", LandmarkType.School, A("Hai Bà Trưng"), 21.0035m, 105.8450m),
            Landmark("Đại học Thủy lợi", LandmarkType.School, A("Đống Đa"), 21.0080m, 105.8240m),
            Landmark("Đại học Ngoại thương", LandmarkType.School, A("Đống Đa"), 21.0230m, 105.8050m),
            Landmark("Học viện Công nghệ Bưu chính Viễn thông", LandmarkType.School, A("Hà Đông"), 20.9810m, 105.7870m),
            Landmark("Đại học Công nghiệp Hà Nội", LandmarkType.School, A("Bắc Từ Liêm"), 21.0540m, 105.7350m),
            Landmark("Tòa Keangnam Landmark 72", LandmarkType.Company, A("Nam Từ Liêm"), 21.0170m, 105.7840m),
            Landmark("Khu văn phòng Duy Tân", LandmarkType.Company, A("Cầu Giấy"), 21.0300m, 105.7830m),
        };
        _db.Landmarks.AddRange(landmarks);
        await _db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- Tài khoản
        var adminHash = _hasher.Hash(AdminPassword);
        var modHash = _hasher.Hash(ModeratorPassword);
        var userHash = _hasher.Hash(UserPassword);
        var admin = NewUser("Quản trị viên", "admin@roommate.test", adminHash, Roles.Admin, now.AddDays(-60));
        var mod1 = NewUser("Kiểm duyệt viên Lan", "mod1@roommate.test", modHash, Roles.Moderator, now.AddDays(-55));
        var mod2 = NewUser("Kiểm duyệt viên Hùng", "mod2@roommate.test", modHash, Roles.Moderator, now.AddDays(-50));
        var u = new[]
        {
            NewUser("Nguyễn Minh Anh", "user1@roommate.test", userHash, Roles.User, now.AddDays(-45), Gender.Female, Occupation.Student, landmarks[0], "0912345601", SleepSchedule.Early, false, false, 4),
            NewUser("Trần Quốc Bảo", "user2@roommate.test", userHash, Roles.User, now.AddDays(-40), Gender.Male, Occupation.Student, landmarks[3], "0912345602", SleepSchedule.Late, false, false, 3),
            NewUser("Lê Thu Hà", "user3@roommate.test", userHash, Roles.User, now.AddDays(-38), Gender.Female, Occupation.Worker, landmarks[10], "0912345603", SleepSchedule.Flexible, false, true, 5),
            NewUser("Phạm Đức Long", "user4@roommate.test", userHash, Roles.User, now.AddDays(-35), Gender.Male, Occupation.Worker, landmarks[11], "0912345604", SleepSchedule.Late, true, false, 3),
            NewUser("Hoàng Ngọc Mai", "user5@roommate.test", userHash, Roles.User, now.AddDays(-30), Gender.Female, Occupation.Student, landmarks[4], "0912345605", SleepSchedule.Early, false, false, 5),
            NewUser("Vũ Thành Nam", "user6@roommate.test", userHash, Roles.User, now.AddDays(-25), Gender.Male, Occupation.Student, landmarks[8], "0912345606", SleepSchedule.Flexible, false, false, 4),
        };
        _db.Users.AddRange(admin, mod1, mod2);
        _db.Users.AddRange(u);
        await _db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- Tin đăng
        var rnd = new Random(2026);
        var posts = new List<Post>();
        Post AddPost(User owner, string type, string title, string desc, decimal price, Area area, string? address,
            int current, int needed, string? gender, int createdDaysAgo, string status, int[] amenities, string? reject = null)
        {
            var created = now.AddDays(-createdDaysAgo).AddHours(-rnd.Next(1, 20));
            var moderated = status == PostStatus.Pending ? (DateTime?)null : created.AddHours(rnd.Next(1, 12));
            var post = new Post
            {
                UserId = owner.UserId, PostType = type, Title = title, Description = desc, Price = price,
                AreaId = area.AreaId, Address = address,
                // Lệch nhẹ quanh tâm khu vực để demo lọc khoảng cách
                Latitude = Math.Round(area.Latitude!.Value + (decimal)(rnd.NextDouble() - 0.5) * 0.02m, 6),
                Longitude = Math.Round(area.Longitude!.Value + (decimal)(rnd.NextDouble() - 0.5) * 0.02m, 6),
                CurrentOccupants = current, NeededOccupants = needed, PreferredGender = gender,
                Status = status, RejectReason = reject,
                ModeratedBy = moderated.HasValue ? mod1.UserId : null, ModeratedAt = moderated,
                ExpiredAt = status is PostStatus.Approved or PostStatus.Closed or PostStatus.Hidden or PostStatus.Expired
                    ? (status == PostStatus.Expired ? now.AddDays(-1) : moderated!.Value.AddDays(30)) : null,
                ViewCount = status == PostStatus.Approved ? rnd.Next(5, 120) : 0,
                CreatedAt = created,
            };
            foreach (var a in amenities) post.PostAmenities.Add(new PostAmenity { AmenityId = a });
            posts.Add(post);
            return post;
        }

        var p1 = AddPost(u[0], PostType.HasRoom, "Phòng 25m² gần ĐHQG Xuân Thủy, cần 1 bạn nữ ở ghép",
            "Phòng thoáng, có cửa sổ, khu yên tĩnh. Mình là sinh viên năm 3, sinh hoạt điều độ, ngủ sớm. Tiền điện nước chia đều.",
            1_800_000, A("Cầu Giấy"), "Ngõ 165 Xuân Thủy", 1, 1, Gender.Female, 12, PostStatus.Approved, new[] { 1, 2, 5, 8 });
        var p2 = AddPost(u[1], PostType.HasRoom, "Căn hộ mini 2 phòng ngủ gần Bách khoa, tìm 2 bạn nam",
            "Căn hộ đủ đồ, có máy giặt, bếp riêng. Tìm bạn nam sạch sẽ, không hút thuốc trong nhà.",
            2_200_000, A("Hai Bà Trưng"), "Ngõ 40 Tạ Quang Bửu", 1, 2, Gender.Male, 10, PostStatus.Approved, new[] { 1, 2, 3, 4, 7 });
        var p3 = AddPost(u[2], PostType.HasRoom, "Ở ghép chung cư gần Keangnam, phòng riêng có ban công",
            "Chung cư có bảo vệ 24/7, thang máy. Mình đi làm văn phòng, có nuôi 1 bé mèo ngoan.",
            3_500_000, A("Nam Từ Liêm"), "Khu đô thị Mỹ Đình 1", 1, 1, Gender.Any, 8, PostStatus.Approved, new[] { 1, 2, 3, 4, 5, 6, 10 });
        AddPost(u[3], PostType.Seeking, "Nam đi làm tìm phòng ở ghép khu Cầu Giấy, ngân sách 2,5 triệu",
            "Mình làm IT ở Duy Tân, đi sớm về muộn, cần phòng có chỗ để xe máy, giờ giấc tự do.",
            2_500_000, A("Cầu Giấy"), null, 0, 1, Gender.Any, 6, PostStatus.Approved, new[] { 2, 5, 8 });
        AddPost(u[4], PostType.Seeking, "Nữ sinh viên KTQD tìm phòng ghép gần trường dưới 2 triệu",
            "Mình năm 2, ngủ sớm, gọn gàng, không hút thuốc. Ưu tiên ở cùng bạn nữ.",
            2_000_000, A("Hai Bà Trưng"), null, 0, 1, Gender.Female, 5, PostStatus.Approved, new[] { 1, 2 });
        AddPost(u[5], PostType.HasRoom, "Phòng gần Học viện Bưu chính, cần thêm 1 bạn nam",
            "Phòng 20m², khép kín, gần chợ và bến xe buýt. Chủ nhà dễ tính.",
            1_500_000, A("Hà Đông"), "Ngõ 122 Trần Phú", 1, 1, Gender.Male, 4, PostStatus.Approved, new[] { 2, 5, 6 });
        AddPost(u[0], PostType.HasRoom, "Phòng khép kín Đống Đa gần ĐH Thủy lợi, tìm 1 bạn",
            "Phòng có điều hòa, nóng lạnh, gần nhiều quán ăn sinh viên.",
            1_700_000, A("Đống Đa"), "Ngõ 175 Tây Sơn", 1, 1, Gender.Any, 3, PostStatus.Approved, new[] { 1, 2, 6 });
        AddPost(u[2], PostType.HasRoom, "Nhà nguyên căn Tây Hồ, còn 2 phòng cho người đi làm",
            "Nhà 4 tầng, mỗi người một phòng riêng, dùng chung bếp và phòng khách. Gần hồ, yên tĩnh.",
            4_000_000, A("Tây Hồ"), "Ngõ 52 Tô Ngọc Vân", 2, 2, Gender.Any, 2, PostStatus.Approved, new[] { 1, 2, 3, 4, 7, 9, 10 });
        AddPost(u[3], PostType.HasRoom, "Phòng giá rẻ Thanh Xuân, cần 1 bạn nam ở ghép ngay",
            "Phòng 18m², gần Royal City. Có thể dọn vào ngay đầu tháng.",
            1_300_000, A("Thanh Xuân"), "Ngõ 1 Nguyễn Huy Tưởng", 1, 1, Gender.Male, 1, PostStatus.Pending, new[] { 2, 5 });
        AddPost(u[4], PostType.HasRoom, "Tìm bạn ở ghép phòng Hoàng Mai gần Times City",
            "Phòng rộng, có ban công, khu an ninh tốt.",
            1_900_000, A("Hoàng Mai"), "Ngõ 460 Minh Khai", 1, 1, Gender.Female, 1, PostStatus.Pending, new[] { 1, 2, 10 });
        AddPost(u[5], PostType.Seeking, "Cần phòng gấp giá 500 nghìn",
            "Cần phòng gấp, liên hệ chuyển cọc trước qua tài khoản để giữ chỗ.",
            500_000, A("Bắc Từ Liêm"), null, 0, 1, null, 7, PostStatus.Rejected, Array.Empty<int>(),
            "Nội dung yêu cầu chuyển tiền cọc trước, không rõ ràng. Vui lòng mô tả cụ thể nhu cầu.");
        var p12 = AddPost(u[1], PostType.HasRoom, "Phòng Ba Đình gần Lăng Bác, đã đủ người",
            "Phòng đẹp gần trung tâm, đã tìm được bạn ở ghép.",
            2_800_000, A("Ba Đình"), "Phố Đội Cấn", 1, 0, Gender.Any, 20, PostStatus.Closed, new[] { 1, 2, 3 });

        _db.Posts.AddRange(posts);
        await _db.SaveChangesAsync(ct);

        // ---------------------------------------------------------------- Kết nối, trò chuyện, đánh giá
        // Một lượt kết nối đã chấp nhận từ 15 ngày trước (đã đủ 7 ngày để đánh giá — BR-05).
        var accepted = new ConnectionRequest
        {
            PostId = p12.PostId, SenderId = u[5].UserId, ReceiverId = u[1].UserId, Message = "Chào bạn, mình muốn xem phòng cuối tuần này.",
            Status = RequestStatus.Accepted, CreatedAt = now.AddDays(-16), RespondedAt = now.AddDays(-15),
        };
        var pending1 = new ConnectionRequest
        {
            PostId = p1.PostId, SenderId = u[4].UserId, ReceiverId = u[0].UserId, Message = "Mình là sinh viên KTQD, ngủ sớm, rất gọn gàng. Mình xem phòng được không?",
            Status = RequestStatus.Pending, CreatedAt = now.AddDays(-1),
        };
        var pending2 = new ConnectionRequest
        {
            PostId = p2.PostId, SenderId = u[3].UserId, ReceiverId = u[1].UserId, Message = "Mình đi làm, cần chỗ ở từ tháng sau.",
            Status = RequestStatus.Pending, CreatedAt = now.AddHours(-5),
        };
        var recentAccepted = new ConnectionRequest
        {
            PostId = p3.PostId, SenderId = u[0].UserId, ReceiverId = u[2].UserId, Message = "Mình rất thích mèo, phòng còn không bạn?",
            Status = RequestStatus.Accepted, CreatedAt = now.AddDays(-3), RespondedAt = now.AddDays(-2),
        };
        _db.ConnectionRequests.AddRange(accepted, pending1, pending2, recentAccepted);
        await _db.SaveChangesAsync(ct);
        // recentAccepted đã lấy 1 chỗ của p3 → hết chỗ → đóng tin (giữ dữ liệu nhất quán với quy tắc trừ chỗ)
        p3.NeededOccupants = 0;
        p3.Status = PostStatus.Closed;

        var conv1 = new Conversation { RequestId = accepted.RequestId, User1Id = u[5].UserId, User2Id = u[1].UserId, CreatedAt = now.AddDays(-15) };
        var conv2 = new Conversation { RequestId = recentAccepted.RequestId, User1Id = u[0].UserId, User2Id = u[2].UserId, CreatedAt = now.AddDays(-2) };
        _db.Conversations.AddRange(conv1, conv2);
        await _db.SaveChangesAsync(ct);

        var messages = new[]
        {
            Msg(conv1, u[5], "Chào anh, em có thể qua xem phòng chiều thứ 7 không ạ?", now.AddDays(-15).AddHours(1)),
            Msg(conv1, u[1], "Được em, khoảng 3 giờ chiều nhé. Anh gửi địa chỉ chi tiết sau.", now.AddDays(-15).AddHours(2)),
            Msg(conv1, u[5], "Dạ em cảm ơn anh!", now.AddDays(-15).AddHours(3)),
            Msg(conv2, u[0], "Chào chị, em muốn hỏi tiền điện nước tính thế nào ạ?", now.AddDays(-2).AddHours(1)),
            Msg(conv2, u[2], "Điện 3.500đ/số, nước 100k/người em nhé.", now.AddDays(-2).AddHours(2)),
        };
        _db.Messages.AddRange(messages);
        conv1.LastMessageAt = messages[2].SentAt;
        conv2.LastMessageAt = messages[4].SentAt;
        foreach (var m in messages.Take(4)) m.IsRead = true;

        _db.Reviews.Add(new Review
        {
            RequestId = accepted.RequestId, ReviewerId = u[5].UserId, RevieweeId = u[1].UserId, Rating = 5,
            Comment = "Anh Bảo thân thiện, phòng đúng như mô tả.", CreatedAt = now.AddDays(-6),
        });
        u[1].AvgRating = 5;
        u[1].ReviewCount = 1;

        // ---------------------------------------------------------------- Báo cáo đang chờ
        _db.Reports.Add(new Report
        {
            ReporterId = u[3].UserId, PostId = posts[5].PostId, ReasonId = 1,
            Description = "Ảnh có vẻ không phải phòng thật.", Status = ReportStatus.Pending, CreatedAt = now.AddHours(-10),
        });

        _db.Notifications.AddRange(
            new Notification { UserId = u[0].UserId, Type = NotificationType.RequestNew, Content = $"{u[4].FullName} muốn kết nối về tin \"{p1.Title}\".", RelatedId = pending1.RequestId, Link = "/connections?tab=incoming", CreatedAt = now.AddDays(-1) },
            new Notification { UserId = u[1].UserId, Type = NotificationType.ReviewNew, Content = "Bạn vừa nhận được đánh giá 5 sao.", Link = $"/users/{u[1].UserId}", CreatedAt = now.AddDays(-6), IsRead = true });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã tạo dữ liệu demo: {Areas} khu vực, {Landmarks} địa điểm, {Users} tài khoản, {Posts} tin.",
            areas.Length, landmarks.Length, u.Length + 3, posts.Count);
    }

    private static Area Area(string name, decimal lat, decimal lon) =>
        new() { Name = name, District = name, City = "Hà Nội", Latitude = lat, Longitude = lon, IsActive = true };

    private static Landmark Landmark(string name, string type, Area area, decimal lat, decimal lon) =>
        new() { Name = name, Type = type, AreaId = area.AreaId, Latitude = lat, Longitude = lon, IsActive = true };

    private static User NewUser(string name, string email, string hash, string role, DateTime created,
        string? gender = null, string? occupation = null, Landmark? landmark = null, string? phone = null,
        string? sleep = null, bool? smoker = null, bool? pet = null, byte? clean = null) => new()
    {
        FullName = name, Email = email, PasswordHash = hash, Role = role, Status = UserStatus.Active, CreatedAt = created,
        Gender = gender, Occupation = occupation, LandmarkId = landmark?.LandmarkId, SchoolOrCompany = landmark?.Name,
        Phone = phone, SleepSchedule = sleep, IsSmoker = smoker, HasPet = pet, CleanlinessLevel = clean,
        Bio = role == Roles.User ? "Mình thân thiện, sống gọn gàng và tôn trọng không gian chung." : null,
    };

    private static Message Msg(Conversation c, User sender, string content, DateTime at) =>
        new() { ConversationId = c.ConversationId, SenderId = sender.UserId, Content = content, SentAt = at };
}
