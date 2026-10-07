/* =====================================================================
   RoommateFinderDB - Script khởi tạo CSDL, phiên bản 2 (đã sửa)
   Hệ quản trị: Microsoft SQL Server 2019+
   Ký hiệu: [SỬA] = thay đổi so với bản trong báo cáo, [MỚI] = thêm mới
   Tổng số bảng: 18 (16 bảng cũ + Landmarks + PasswordResetTokens)
   ===================================================================== */
CREATE DATABASE RoommateFinderDB;
GO
USE RoommateFinderDB;
GO
-- Filtered index yêu cầu hai tùy chọn này (mặc định đã bật trong SSMS / EF Core)
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ---------------------------------------------------------------------
   1. DANH MỤC
   --------------------------------------------------------------------- */
CREATE TABLE Areas (
    AreaId    INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(100) NOT NULL,
    District  NVARCHAR(100) NULL,
    City      NVARCHAR(100) NOT NULL,
    Latitude  DECIMAL(9,6) NULL,          -- tâm khu vực, dùng khi tin không có tọa độ riêng
    Longitude DECIMAL(9,6) NULL,
    IsActive  BIT NOT NULL DEFAULT 1      -- [MỚI] ẩn khu vực thay vì xóa
);

-- [MỚI] Địa điểm mốc (trường học, khu công nghiệp, tòa văn phòng) có sẵn tọa độ.
-- Giải quyết bài toán "lọc theo khoảng cách đến trường/công ty" khi không dùng Google Maps:
-- người dùng chọn mốc từ danh sách, hệ thống tính khoảng cách đường chim bay (Haversine).
CREATE TABLE Landmarks (
    LandmarkId INT IDENTITY(1,1) PRIMARY KEY,
    Name       NVARCHAR(150) NOT NULL,
    Type       NVARCHAR(15) NOT NULL CHECK (Type IN (N'school', N'company', N'other')),
    AreaId     INT NOT NULL FOREIGN KEY REFERENCES Areas(AreaId),
    Address    NVARCHAR(255) NULL,
    Latitude   DECIMAL(9,6) NOT NULL,
    Longitude  DECIMAL(9,6) NOT NULL,
    IsActive   BIT NOT NULL DEFAULT 1
);

CREATE TABLE Amenities (
    AmenityId INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(100) NOT NULL UNIQUE,
    IsActive  BIT NOT NULL DEFAULT 1      -- [MỚI]
);

CREATE TABLE ReportReasons (
    ReasonId  INT IDENTITY(1,1) PRIMARY KEY,
    Name      NVARCHAR(150) NOT NULL,
    AppliesTo NVARCHAR(10) NOT NULL DEFAULT N'both'          -- [MỚI] lý do dành cho tin, người dùng hay cả hai
        CHECK (AppliesTo IN (N'post', N'user', N'both')),
    IsActive  BIT NOT NULL DEFAULT 1                          -- [MỚI]
);

CREATE TABLE SystemConfigs (
    ConfigKey   NVARCHAR(50) PRIMARY KEY,
    ConfigValue NVARCHAR(255) NOT NULL,
    Description NVARCHAR(255) NULL
);

/* ---------------------------------------------------------------------
   2. NGƯỜI DÙNG
   --------------------------------------------------------------------- */
CREATE TABLE Users (
    UserId              BIGINT IDENTITY(1,1) PRIMARY KEY,
    FullName            NVARCHAR(100) NOT NULL,
    Email               NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash        NVARCHAR(255) NOT NULL,
    Phone               NVARCHAR(15) NULL,
    Gender              NVARCHAR(10) NULL CHECK (Gender IN (N'male', N'female', N'other')),
    DateOfBirth         DATE NULL,
    Role                NVARCHAR(20) NOT NULL DEFAULT N'user'
        CHECK (Role IN (N'user', N'moderator', N'admin')),
    Occupation          NVARCHAR(20) NULL CHECK (Occupation IN (N'student', N'worker')),
    SchoolOrCompany     NVARCHAR(150) NULL,
    LandmarkId          INT NULL FOREIGN KEY REFERENCES Landmarks(LandmarkId), -- [MỚI] trường/công ty chọn từ danh sách
    AvatarUrl           NVARCHAR(255) NULL,
    Bio                 NVARCHAR(500) NULL,
    -- [MỚI] Thông tin sinh hoạt (mục 3.2.3 yêu cầu nhưng bản cũ chưa có cột)
    SleepSchedule       NVARCHAR(10) NULL CHECK (SleepSchedule IN (N'early', N'late', N'flexible')),
    IsSmoker            BIT NULL,
    HasPet              BIT NULL,
    CleanlinessLevel    TINYINT NULL CHECK (CleanlinessLevel BETWEEN 1 AND 5),
    -- Đánh giá
    AvgRating           DECIMAL(3,2) NOT NULL DEFAULT 0,
    ReviewCount         INT NOT NULL DEFAULT 0,                 -- [MỚI] hiển thị "4.5 (12 đánh giá)"
    -- [SỬA] Tách rõ 2 loại khóa:
    --   LockedUntil    = khóa tạm do đăng nhập sai (tự mở sau 15 phút)
    --   Status 'suspended' + SuspendedUntil = khóa do vi phạm, có thời hạn
    --   Status 'banned' = khóa vĩnh viễn
    Status              NVARCHAR(15) NOT NULL DEFAULT N'active'
        CHECK (Status IN (N'active', N'suspended', N'banned')),
    SuspendedUntil      DATETIME2 NULL,                         -- [MỚI]
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockedUntil         DATETIME2 NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2 NULL,                         -- [MỚI]
    CONSTRAINT CK_User_Suspend CHECK (Status <> N'suspended' OR SuspendedUntil IS NOT NULL)
);

-- [MỚI] Quên mật khẩu: lưu mã đặt lại (đã băm), dùng một lần, có hạn
CREATE TABLE PasswordResetTokens (
    TokenId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId    BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    TokenHash NVARCHAR(255) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    UsedAt    DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

/* ---------------------------------------------------------------------
   3. TIN ĐĂNG
   --------------------------------------------------------------------- */
CREATE TABLE Posts (
    PostId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId           BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    -- has_room: có phòng, cần thêm người   | Price = giá thuê mỗi người/tháng
    -- seeking : đang tìm phòng để ở ghép   | Price = ngân sách tối đa mỗi người/tháng
    PostType         NVARCHAR(10) NOT NULL CHECK (PostType IN (N'has_room', N'seeking')),
    Title            NVARCHAR(150) NOT NULL,
    Description      NVARCHAR(2000) NULL,
    Price            DECIMAL(12,0) NOT NULL CHECK (Price > 0),      -- [SỬA] >= 0 thành > 0 cho khớp UC 3.4.3
    AreaId           INT NOT NULL FOREIGN KEY REFERENCES Areas(AreaId),
    Address          NVARCHAR(255) NULL,
    Latitude         DECIMAL(9,6) NULL,      -- NULL thì lấy tọa độ tâm khu vực
    Longitude        DECIMAL(9,6) NULL,
    CurrentOccupants INT NOT NULL DEFAULT 0 CHECK (CurrentOccupants >= 0),
    -- has_room: số chỗ còn trống cần tìm thêm người
    -- seeking : số người trong nhóm đang tìm chỗ (thường là 1)
    NeededOccupants  INT NOT NULL DEFAULT 1 CHECK (NeededOccupants >= 0),
    PreferredGender  NVARCHAR(10) NULL
        CHECK (PreferredGender IN (N'male', N'female', N'any')),
    Status           NVARCHAR(12) NOT NULL DEFAULT N'pending'
        CHECK (Status IN (N'pending', N'approved', N'rejected',
                          N'hidden', N'expired', N'closed')),
    RejectReason     NVARCHAR(500) NULL,                            -- [MỚI] lý do từ chối/ẩn
    ModeratedBy      BIGINT NULL FOREIGN KEY REFERENCES Users(UserId), -- [MỚI]
    ModeratedAt      DATETIME2 NULL,                                -- [MỚI]
    ExtendCount      INT NOT NULL DEFAULT 0,                        -- [MỚI] số lần đã gia hạn
    ViewCount        INT NOT NULL DEFAULT 0,
    IsDeleted        BIT NOT NULL DEFAULT 0,                        -- [MỚI] xóa mềm (tránh lỗi FK)
    CreatedAt        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt        DATETIME2 NULL,                                -- [MỚI]
    ExpiredAt        DATETIME2 NULL,
    CONSTRAINT CK_Post_Reject CHECK (Status <> N'rejected' OR RejectReason IS NOT NULL)
);

CREATE TABLE PostImages (
    ImageId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId    BIGINT NOT NULL FOREIGN KEY REFERENCES Posts(PostId) ON DELETE CASCADE,
    ImageUrl  NVARCHAR(255) NOT NULL,
    SortOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE PostAmenities (
    PostId    BIGINT NOT NULL FOREIGN KEY REFERENCES Posts(PostId) ON DELETE CASCADE,
    AmenityId INT NOT NULL FOREIGN KEY REFERENCES Amenities(AmenityId),
    PRIMARY KEY (PostId, AmenityId)
);

CREATE TABLE SavedPosts (
    SavedId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId  BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PostId  BIGINT NOT NULL FOREIGN KEY REFERENCES Posts(PostId),
    SavedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Saved_UserPost UNIQUE (UserId, PostId)
);

/* ---------------------------------------------------------------------
   4. KẾT NỐI VÀ TRÒ CHUYỆN
   --------------------------------------------------------------------- */
CREATE TABLE ConnectionRequests (
    RequestId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    PostId      BIGINT NOT NULL FOREIGN KEY REFERENCES Posts(PostId),
    SenderId    BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    ReceiverId  BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Message     NVARCHAR(500) NULL,
    -- [SỬA] thêm 'cancelled' (người gửi rút lại) và 'expired' (tin đóng/hết hạn khi còn pending)
    Status      NVARCHAR(10) NOT NULL DEFAULT N'pending'
        CHECK (Status IN (N'pending', N'accepted', N'rejected', N'cancelled', N'expired')),
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    RespondedAt DATETIME2 NULL,                                     -- [MỚI]
    CONSTRAINT CK_Request_NotSelf CHECK (SenderId <> ReceiverId)    -- [MỚI]
);
-- [SỬA] Bản cũ: UNIQUE (PostId, SenderId) chặn vĩnh viễn, bị từ chối 1 lần là không gửi lại được.
-- Bản mới: chỉ chặn khi đã có yêu cầu đang chờ hoặc đã được chấp nhận cho cùng tin.
CREATE UNIQUE INDEX UQ_Request_SenderPost_Active
    ON ConnectionRequests (PostId, SenderId)
    WHERE Status IN (N'pending', N'accepted');

CREATE TABLE Conversations (
    ConversationId BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId      BIGINT NOT NULL UNIQUE
        FOREIGN KEY REFERENCES ConnectionRequests(RequestId),
    User1Id        BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    User2Id        BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    LastMessageAt  DATETIME2 NULL,                                  -- [MỚI] sắp xếp danh sách hội thoại
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Conversation_NotSelf CHECK (User1Id <> User2Id)   -- [MỚI]
);

CREATE TABLE Messages (
    MessageId      BIGINT IDENTITY(1,1) PRIMARY KEY,
    ConversationId BIGINT NOT NULL
        FOREIGN KEY REFERENCES Conversations(ConversationId),
    SenderId       BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Content        NVARCHAR(1000) NOT NULL,
    SentAt         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsRead         BIT NOT NULL DEFAULT 0
);

/* ---------------------------------------------------------------------
   5. ĐÁNH GIÁ
   --------------------------------------------------------------------- */
-- [SỬA] Bản cũ: UNIQUE (ReviewerId, PostId) -> chủ tin nhận 2 người qua cùng 1 tin
-- chỉ đánh giá được 1 người. Bản mới gắn đánh giá với từng lượt kết nối (RequestId).
-- Bỏ cột PostId (suy ra qua RequestId) để tránh dữ liệu mâu thuẫn.
CREATE TABLE Reviews (
    ReviewId   BIGINT IDENTITY(1,1) PRIMARY KEY,
    RequestId  BIGINT NOT NULL FOREIGN KEY REFERENCES ConnectionRequests(RequestId), -- [MỚI]
    ReviewerId BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    RevieweeId BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Rating     TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment    NVARCHAR(500) NULL,
    IsHidden   BIT NOT NULL DEFAULT 0,                              -- [MỚI] Moderator ẩn đánh giá vi phạm
    CreatedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Review_ReviewerRequest UNIQUE (ReviewerId, RequestId),
    CONSTRAINT CK_Review_NotSelf CHECK (ReviewerId <> RevieweeId)    -- [MỚI]
);

/* ---------------------------------------------------------------------
   6. BÁO CÁO VÀ VI PHẠM
   --------------------------------------------------------------------- */
CREATE TABLE Reports (
    ReportId       BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReporterId     BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PostId         BIGINT NULL FOREIGN KEY REFERENCES Posts(PostId),
    ReportedUserId BIGINT NULL FOREIGN KEY REFERENCES Users(UserId),
    ReasonId       INT NOT NULL FOREIGN KEY REFERENCES ReportReasons(ReasonId),
    Description    NVARCHAR(500) NULL,
    Status         NVARCHAR(10) NOT NULL DEFAULT N'pending'
        CHECK (Status IN (N'pending', N'resolved', N'dismissed')),
    HandledBy      BIGINT NULL FOREIGN KEY REFERENCES Users(UserId),
    HandledAt      DATETIME2 NULL,
    HandledNote    NVARCHAR(500) NULL,                              -- [MỚI]
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Report_Target CHECK (
        (PostId IS NOT NULL AND ReportedUserId IS NULL) OR
        (PostId IS NULL AND ReportedUserId IS NOT NULL)
    ),
    CONSTRAINT CK_Report_NotSelf CHECK (ReportedUserId IS NULL OR ReportedUserId <> ReporterId)
);
-- [MỚI] Mỗi người chỉ có 1 báo cáo đang chờ cho cùng một tin -> chống 1 người spam đủ ngưỡng tự ẩn
CREATE UNIQUE INDEX UQ_Report_ReporterPost_Pending
    ON Reports (ReporterId, PostId)
    WHERE Status = N'pending' AND PostId IS NOT NULL;

CREATE TABLE ViolationHistory (
    ViolationId  BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId       BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    ReportId     BIGINT NULL FOREIGN KEY REFERENCES Reports(ReportId),
    PostId       BIGINT NULL FOREIGN KEY REFERENCES Posts(PostId),  -- [MỚI] tin bị ẩn (khi Action = hide_post)
    Level        NVARCHAR(15) NOT NULL CHECK (Level IN (N'light', N'medium', N'severe')),
    -- [SỬA] tách khóa có thời hạn và khóa vĩnh viễn
    Action       NVARCHAR(20) NOT NULL
        CHECK (Action IN (N'warning', N'hide_post', N'suspend_account', N'ban_account')),
    SuspendDays  INT NULL CHECK (SuspendDays > 0),                  -- [MỚI] số ngày khóa
    Note         NVARCHAR(500) NULL,
    HandledBy    BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Violation_Suspend CHECK (Action <> N'suspend_account' OR SuspendDays IS NOT NULL),
    CONSTRAINT CK_Violation_HidePost CHECK (Action <> N'hide_post' OR PostId IS NOT NULL)
);

/* ---------------------------------------------------------------------
   7. THÔNG BÁO
   --------------------------------------------------------------------- */
CREATE TABLE Notifications (
    NotificationId BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId         BIGINT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Type           NVARCHAR(30) NOT NULL,      -- post_approved, post_rejected, request_new, request_accepted, ...
    Content        NVARCHAR(500) NOT NULL,
    RelatedId      BIGINT NULL,                -- [MỚI] Id đối tượng liên quan (PostId, RequestId, ...)
    Link           NVARCHAR(255) NULL,         -- [MỚI] đường dẫn mở khi bấm thông báo
    IsRead         BIT NOT NULL DEFAULT 0,
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* ---------------------------------------------------------------------
   8. CHỈ MỤC [MỚI] phục vụ truy vấn thường dùng
   --------------------------------------------------------------------- */
CREATE INDEX IX_Posts_Search      ON Posts (Status, AreaId, PostType, Price)
    INCLUDE (PreferredGender, NeededOccupants, ExpiredAt, Latitude, Longitude)
    WHERE IsDeleted = 0;
CREATE INDEX IX_Posts_User        ON Posts (UserId, Status);
CREATE INDEX IX_Posts_ExpiredAt   ON Posts (ExpiredAt) WHERE Status = N'approved';
CREATE INDEX IX_Requests_Receiver ON ConnectionRequests (ReceiverId, Status);
CREATE INDEX IX_Requests_Sender   ON ConnectionRequests (SenderId, Status);
CREATE INDEX IX_Messages_Conv     ON Messages (ConversationId, SentAt);
CREATE INDEX IX_Notif_User        ON Notifications (UserId, IsRead, CreatedAt DESC);
CREATE INDEX IX_Reports_Status    ON Reports (Status, CreatedAt);
CREATE INDEX IX_Reports_Post      ON Reports (PostId, CreatedAt) WHERE PostId IS NOT NULL;
CREATE INDEX IX_Reviews_Reviewee  ON Reviews (RevieweeId) WHERE IsHidden = 0;
GO

/* ---------------------------------------------------------------------
   9. DỮ LIỆU KHỞI TẠO [MỚI]
   Các giá trị là ĐỀ XUẤT, chỉnh lại sau khi BA chốt.
   --------------------------------------------------------------------- */
INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description) VALUES
 (N'POST_EXPIRE_DAYS',         N'30', N'Số ngày hiển thị của một tin sau khi duyệt'),
 (N'POST_MAX_EXTEND',          N'3',  N'Số lần gia hạn tối đa của một tin'),
 (N'POST_MAX_PER_DAY',         N'3',  N'Số tin tối đa một người được đăng mỗi ngày (chống spam)'),
 (N'POST_MAX_IMAGES',          N'10', N'Số ảnh tối đa mỗi tin'),
 (N'IMAGE_MAX_SIZE_MB',        N'5',  N'Dung lượng tối đa mỗi ảnh (MB)'),
 (N'AUTO_HIDE_REPORT_COUNT',   N'5',  N'Số báo cáo để tự ẩn tin'),
 (N'AUTO_HIDE_WINDOW_HOURS',   N'24', N'Khoảng thời gian tính ngưỡng báo cáo (giờ)'),
 (N'LOGIN_MAX_FAILED',         N'5',  N'Số lần đăng nhập sai liên tiếp trước khi khóa tạm'),
 (N'LOGIN_LOCK_MINUTES',       N'15', N'Thời gian khóa tạm sau khi đăng nhập sai (phút)'),
 (N'RESET_TOKEN_MINUTES',      N'30', N'Thời hạn mã đặt lại mật khẩu (phút)'),
 (N'JWT_ACCESS_MINUTES',       N'120',N'Thời hạn JWT (phút)'),
 (N'REVIEW_MIN_DAYS',          N'7',  N'Số ngày tối thiểu sau khi chấp nhận kết nối mới được đánh giá'),
 (N'SEARCH_MAX_DISTANCE_KM',   N'20', N'Bán kính tối đa của bộ lọc khoảng cách (km)');

INSERT INTO ReportReasons (Name, AppliesTo) VALUES
 (N'Tin đăng sai sự thật / tin ảo', N'post'),
 (N'Lừa đảo, yêu cầu chuyển tiền trước', N'both'),
 (N'Nội dung phản cảm, không phù hợp', N'both'),
 (N'Tin đăng trùng lặp', N'post'),
 (N'Quấy rối, ngôn từ xúc phạm', N'user'),
 (N'Giả mạo danh tính', N'user'),
 (N'Lý do khác', N'both');

INSERT INTO Amenities (Name) VALUES
 (N'Điều hòa'), (N'Wifi'), (N'Máy giặt'), (N'Tủ lạnh'), (N'Chỗ để xe'),
 (N'Nhà vệ sinh riêng'), (N'Bếp nấu ăn'), (N'Giờ giấc tự do'), (N'Không chung chủ'), (N'Ban công');
GO
