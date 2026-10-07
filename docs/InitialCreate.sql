IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Amenities] (
    [AmenityId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Amenities] PRIMARY KEY ([AmenityId])
);
GO

CREATE TABLE [Areas] (
    [AreaId] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [District] nvarchar(100) NULL,
    [City] nvarchar(100) NOT NULL,
    [Latitude] decimal(9,6) NULL,
    [Longitude] decimal(9,6) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Areas] PRIMARY KEY ([AreaId])
);
GO

CREATE TABLE [ReportReasons] (
    [ReasonId] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [AppliesTo] nvarchar(10) NOT NULL DEFAULT N'both',
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_ReportReasons] PRIMARY KEY ([ReasonId]),
    CONSTRAINT [CK_ReportReason_AppliesTo] CHECK ([AppliesTo] IN (N'post', N'user', N'both'))
);
GO

CREATE TABLE [SystemConfigs] (
    [ConfigKey] nvarchar(50) NOT NULL,
    [ConfigValue] nvarchar(255) NOT NULL,
    [Description] nvarchar(255) NULL,
    CONSTRAINT [PK_SystemConfigs] PRIMARY KEY ([ConfigKey])
);
GO

CREATE TABLE [Landmarks] (
    [LandmarkId] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Type] nvarchar(15) NOT NULL,
    [AreaId] int NOT NULL,
    [Address] nvarchar(255) NULL,
    [Latitude] decimal(9,6) NOT NULL,
    [Longitude] decimal(9,6) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Landmarks] PRIMARY KEY ([LandmarkId]),
    CONSTRAINT [CK_Landmark_Type] CHECK ([Type] IN (N'school', N'company', N'other')),
    CONSTRAINT [FK_Landmarks_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([AreaId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Users] (
    [UserId] bigint NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Phone] nvarchar(15) NULL,
    [Gender] nvarchar(10) NULL,
    [DateOfBirth] date NULL,
    [Role] nvarchar(20) NOT NULL DEFAULT N'user',
    [Occupation] nvarchar(20) NULL,
    [SchoolOrCompany] nvarchar(150) NULL,
    [LandmarkId] int NULL,
    [AvatarUrl] nvarchar(255) NULL,
    [Bio] nvarchar(500) NULL,
    [SleepSchedule] nvarchar(10) NULL,
    [IsSmoker] bit NULL,
    [HasPet] bit NULL,
    [CleanlinessLevel] tinyint NULL,
    [AvgRating] decimal(3,2) NOT NULL DEFAULT 0.0,
    [ReviewCount] int NOT NULL,
    [Status] nvarchar(15) NOT NULL DEFAULT N'active',
    [SuspendedUntil] datetime2 NULL,
    [FailedLoginAttempts] int NOT NULL,
    [LockedUntil] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_User_Cleanliness] CHECK ([CleanlinessLevel] BETWEEN 1 AND 5),
    CONSTRAINT [CK_User_Gender] CHECK ([Gender] IN (N'male', N'female', N'other')),
    CONSTRAINT [CK_User_Occupation] CHECK ([Occupation] IN (N'student', N'worker')),
    CONSTRAINT [CK_User_Role] CHECK ([Role] IN (N'user', N'moderator', N'admin')),
    CONSTRAINT [CK_User_SleepSchedule] CHECK ([SleepSchedule] IN (N'early', N'late', N'flexible')),
    CONSTRAINT [CK_User_Status] CHECK ([Status] IN (N'active', N'suspended', N'banned')),
    CONSTRAINT [CK_User_Suspend] CHECK ([Status] <> N'suspended' OR [SuspendedUntil] IS NOT NULL),
    CONSTRAINT [FK_Users_Landmarks_LandmarkId] FOREIGN KEY ([LandmarkId]) REFERENCES [Landmarks] ([LandmarkId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Notifications] (
    [NotificationId] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [Content] nvarchar(500) NOT NULL,
    [RelatedId] bigint NULL,
    [Link] nvarchar(255) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PasswordResetTokens] (
    [TokenId] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [TokenHash] nvarchar(255) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [UsedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([TokenId]),
    CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Posts] (
    [PostId] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [PostType] nvarchar(10) NOT NULL,
    [Title] nvarchar(150) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [Price] decimal(12,0) NOT NULL,
    [AreaId] int NOT NULL,
    [Address] nvarchar(255) NULL,
    [Latitude] decimal(9,6) NULL,
    [Longitude] decimal(9,6) NULL,
    [CurrentOccupants] int NOT NULL,
    [NeededOccupants] int NOT NULL,
    [PreferredGender] nvarchar(10) NULL,
    [Status] nvarchar(12) NOT NULL DEFAULT N'pending',
    [RejectReason] nvarchar(500) NULL,
    [ModeratedBy] bigint NULL,
    [ModeratedAt] datetime2 NULL,
    [ExtendCount] int NOT NULL,
    [ViewCount] int NOT NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] datetime2 NULL,
    [ExpiredAt] datetime2 NULL,
    CONSTRAINT [PK_Posts] PRIMARY KEY ([PostId]),
    CONSTRAINT [CK_Post_CurrentOccupants] CHECK ([CurrentOccupants] >= 0),
    CONSTRAINT [CK_Post_NeededOccupants] CHECK ([NeededOccupants] >= 0),
    CONSTRAINT [CK_Post_PreferredGender] CHECK ([PreferredGender] IN (N'male', N'female', N'any')),
    CONSTRAINT [CK_Post_Price] CHECK ([Price] > 0),
    CONSTRAINT [CK_Post_Reject] CHECK ([Status] <> N'rejected' OR [RejectReason] IS NOT NULL),
    CONSTRAINT [CK_Post_Status] CHECK ([Status] IN (N'pending', N'approved', N'rejected', N'hidden', N'expired', N'closed')),
    CONSTRAINT [CK_Post_Type] CHECK ([PostType] IN (N'has_room', N'seeking')),
    CONSTRAINT [FK_Posts_Areas_AreaId] FOREIGN KEY ([AreaId]) REFERENCES [Areas] ([AreaId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Posts_Users_ModeratedBy] FOREIGN KEY ([ModeratedBy]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Posts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ConnectionRequests] (
    [RequestId] bigint NOT NULL IDENTITY,
    [PostId] bigint NOT NULL,
    [SenderId] bigint NOT NULL,
    [ReceiverId] bigint NOT NULL,
    [Message] nvarchar(500) NULL,
    [Status] nvarchar(10) NOT NULL DEFAULT N'pending',
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_ConnectionRequests] PRIMARY KEY ([RequestId]),
    CONSTRAINT [CK_Request_NotSelf] CHECK ([SenderId] <> [ReceiverId]),
    CONSTRAINT [CK_Request_Status] CHECK ([Status] IN (N'pending', N'accepted', N'rejected', N'cancelled', N'expired')),
    CONSTRAINT [FK_ConnectionRequests_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConnectionRequests_Users_ReceiverId] FOREIGN KEY ([ReceiverId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConnectionRequests_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PostAmenities] (
    [PostId] bigint NOT NULL,
    [AmenityId] int NOT NULL,
    CONSTRAINT [PK_PostAmenities] PRIMARY KEY ([PostId], [AmenityId]),
    CONSTRAINT [FK_PostAmenities_Amenities_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [Amenities] ([AmenityId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PostAmenities_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE CASCADE
);
GO

CREATE TABLE [PostImages] (
    [ImageId] bigint NOT NULL IDENTITY,
    [PostId] bigint NOT NULL,
    [ImageUrl] nvarchar(255) NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_PostImages] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK_PostImages_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Reports] (
    [ReportId] bigint NOT NULL IDENTITY,
    [ReporterId] bigint NOT NULL,
    [PostId] bigint NULL,
    [ReportedUserId] bigint NULL,
    [ReasonId] int NOT NULL,
    [Description] nvarchar(500) NULL,
    [Status] nvarchar(10) NOT NULL DEFAULT N'pending',
    [HandledBy] bigint NULL,
    [HandledAt] datetime2 NULL,
    [HandledNote] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Reports] PRIMARY KEY ([ReportId]),
    CONSTRAINT [CK_Report_NotSelf] CHECK ([ReportedUserId] IS NULL OR [ReportedUserId] <> [ReporterId]),
    CONSTRAINT [CK_Report_Status] CHECK ([Status] IN (N'pending', N'resolved', N'dismissed')),
    CONSTRAINT [CK_Report_Target] CHECK (([PostId] IS NOT NULL AND [ReportedUserId] IS NULL) OR ([PostId] IS NULL AND [ReportedUserId] IS NOT NULL)),
    CONSTRAINT [FK_Reports_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_ReportReasons_ReasonId] FOREIGN KEY ([ReasonId]) REFERENCES [ReportReasons] ([ReasonId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_Users_HandledBy] FOREIGN KEY ([HandledBy]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_Users_ReportedUserId] FOREIGN KEY ([ReportedUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reports_Users_ReporterId] FOREIGN KEY ([ReporterId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [SavedPosts] (
    [SavedId] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [PostId] bigint NOT NULL,
    [SavedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SavedPosts] PRIMARY KEY ([SavedId]),
    CONSTRAINT [FK_SavedPosts_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_SavedPosts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Conversations] (
    [ConversationId] bigint NOT NULL IDENTITY,
    [RequestId] bigint NOT NULL,
    [User1Id] bigint NOT NULL,
    [User2Id] bigint NOT NULL,
    [LastMessageAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Conversations] PRIMARY KEY ([ConversationId]),
    CONSTRAINT [CK_Conversation_NotSelf] CHECK ([User1Id] <> [User2Id]),
    CONSTRAINT [FK_Conversations_ConnectionRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ConnectionRequests] ([RequestId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Conversations_Users_User1Id] FOREIGN KEY ([User1Id]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Conversations_Users_User2Id] FOREIGN KEY ([User2Id]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Reviews] (
    [ReviewId] bigint NOT NULL IDENTITY,
    [RequestId] bigint NOT NULL,
    [ReviewerId] bigint NOT NULL,
    [RevieweeId] bigint NOT NULL,
    [Rating] tinyint NOT NULL,
    [Comment] nvarchar(500) NULL,
    [IsHidden] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Reviews] PRIMARY KEY ([ReviewId]),
    CONSTRAINT [CK_Review_NotSelf] CHECK ([ReviewerId] <> [RevieweeId]),
    CONSTRAINT [CK_Review_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
    CONSTRAINT [FK_Reviews_ConnectionRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ConnectionRequests] ([RequestId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Users_RevieweeId] FOREIGN KEY ([RevieweeId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Reviews_Users_ReviewerId] FOREIGN KEY ([ReviewerId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ViolationHistory] (
    [ViolationId] bigint NOT NULL IDENTITY,
    [UserId] bigint NOT NULL,
    [ReportId] bigint NULL,
    [PostId] bigint NULL,
    [Level] nvarchar(15) NOT NULL,
    [Action] nvarchar(20) NOT NULL,
    [SuspendDays] int NULL,
    [Note] nvarchar(500) NULL,
    [HandledBy] bigint NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ViolationHistory] PRIMARY KEY ([ViolationId]),
    CONSTRAINT [CK_Violation_Action] CHECK ([Action] IN (N'warning', N'hide_post', N'suspend_account', N'ban_account')),
    CONSTRAINT [CK_Violation_HidePost] CHECK ([Action] <> N'hide_post' OR [PostId] IS NOT NULL),
    CONSTRAINT [CK_Violation_Level] CHECK ([Level] IN (N'light', N'medium', N'severe')),
    CONSTRAINT [CK_Violation_Suspend] CHECK ([Action] <> N'suspend_account' OR [SuspendDays] IS NOT NULL),
    CONSTRAINT [CK_Violation_SuspendDays] CHECK ([SuspendDays] > 0),
    CONSTRAINT [FK_ViolationHistory_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ViolationHistory_Reports_ReportId] FOREIGN KEY ([ReportId]) REFERENCES [Reports] ([ReportId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ViolationHistory_Users_HandledBy] FOREIGN KEY ([HandledBy]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ViolationHistory_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Messages] (
    [MessageId] bigint NOT NULL IDENTITY,
    [ConversationId] bigint NOT NULL,
    [SenderId] bigint NOT NULL,
    [Content] nvarchar(1000) NOT NULL,
    [SentAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [IsRead] bit NOT NULL,
    CONSTRAINT [PK_Messages] PRIMARY KEY ([MessageId]),
    CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([ConversationId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Messages_Users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AmenityId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Amenities]'))
    SET IDENTITY_INSERT [Amenities] ON;
INSERT INTO [Amenities] ([AmenityId], [IsActive], [Name])
VALUES (1, CAST(1 AS bit), N'Điều hòa'),
(2, CAST(1 AS bit), N'Wifi'),
(3, CAST(1 AS bit), N'Máy giặt'),
(4, CAST(1 AS bit), N'Tủ lạnh'),
(5, CAST(1 AS bit), N'Chỗ để xe'),
(6, CAST(1 AS bit), N'Nhà vệ sinh riêng'),
(7, CAST(1 AS bit), N'Bếp nấu ăn'),
(8, CAST(1 AS bit), N'Giờ giấc tự do'),
(9, CAST(1 AS bit), N'Không chung chủ'),
(10, CAST(1 AS bit), N'Ban công');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AmenityId', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[Amenities]'))
    SET IDENTITY_INSERT [Amenities] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ReasonId', N'AppliesTo', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[ReportReasons]'))
    SET IDENTITY_INSERT [ReportReasons] ON;
INSERT INTO [ReportReasons] ([ReasonId], [AppliesTo], [IsActive], [Name])
VALUES (1, N'post', CAST(1 AS bit), N'Tin đăng sai sự thật / tin ảo'),
(2, N'both', CAST(1 AS bit), N'Lừa đảo, yêu cầu chuyển tiền trước'),
(3, N'both', CAST(1 AS bit), N'Nội dung phản cảm, không phù hợp'),
(4, N'post', CAST(1 AS bit), N'Tin đăng trùng lặp'),
(5, N'user', CAST(1 AS bit), N'Quấy rối, ngôn từ xúc phạm'),
(6, N'user', CAST(1 AS bit), N'Giả mạo danh tính'),
(7, N'both', CAST(1 AS bit), N'Lý do khác');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ReasonId', N'AppliesTo', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[ReportReasons]'))
    SET IDENTITY_INSERT [ReportReasons] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConfigKey', N'ConfigValue', N'Description') AND [object_id] = OBJECT_ID(N'[SystemConfigs]'))
    SET IDENTITY_INSERT [SystemConfigs] ON;
INSERT INTO [SystemConfigs] ([ConfigKey], [ConfigValue], [Description])
VALUES (N'AUTO_HIDE_REPORT_COUNT', N'5', N'Số báo cáo để tự ẩn tin'),
(N'AUTO_HIDE_WINDOW_HOURS', N'24', N'Khoảng thời gian tính ngưỡng báo cáo (giờ)'),
(N'IMAGE_MAX_SIZE_MB', N'5', N'Dung lượng tối đa mỗi ảnh (MB)'),
(N'JWT_ACCESS_MINUTES', N'120', N'Thời hạn JWT (phút)'),
(N'LOGIN_LOCK_MINUTES', N'15', N'Thời gian khóa tạm sau khi đăng nhập sai (phút)'),
(N'LOGIN_MAX_FAILED', N'5', N'Số lần đăng nhập sai liên tiếp trước khi khóa tạm'),
(N'POST_EXPIRE_DAYS', N'30', N'Số ngày hiển thị của một tin sau khi duyệt'),
(N'POST_MAX_EXTEND', N'3', N'Số lần gia hạn tối đa của một tin'),
(N'POST_MAX_IMAGES', N'10', N'Số ảnh tối đa mỗi tin'),
(N'POST_MAX_PER_DAY', N'3', N'Số tin tối đa một người được đăng mỗi ngày (chống spam)'),
(N'RESET_TOKEN_MINUTES', N'30', N'Thời hạn mã đặt lại mật khẩu (phút)'),
(N'REVIEW_MIN_DAYS', N'7', N'Số ngày tối thiểu sau khi chấp nhận kết nối mới được đánh giá'),
(N'SEARCH_MAX_DISTANCE_KM', N'20', N'Bán kính tối đa của bộ lọc khoảng cách (km)');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ConfigKey', N'ConfigValue', N'Description') AND [object_id] = OBJECT_ID(N'[SystemConfigs]'))
    SET IDENTITY_INSERT [SystemConfigs] OFF;
GO

CREATE UNIQUE INDEX [UQ_Amenities_Name] ON [Amenities] ([Name]);
GO

CREATE INDEX [IX_Requests_Receiver] ON [ConnectionRequests] ([ReceiverId], [Status]);
GO

CREATE INDEX [IX_Requests_Sender] ON [ConnectionRequests] ([SenderId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_Request_SenderPost_Active] ON [ConnectionRequests] ([PostId], [SenderId]) WHERE [Status] IN (N'pending', N'accepted');
GO

CREATE INDEX [IX_Conversations_User1Id] ON [Conversations] ([User1Id]);
GO

CREATE INDEX [IX_Conversations_User2Id] ON [Conversations] ([User2Id]);
GO

CREATE UNIQUE INDEX [UQ_Conversations_Request] ON [Conversations] ([RequestId]);
GO

CREATE INDEX [IX_Landmarks_AreaId] ON [Landmarks] ([AreaId]);
GO

CREATE INDEX [IX_Messages_Conv] ON [Messages] ([ConversationId], [SentAt]);
GO

CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
GO

CREATE INDEX [IX_Notif_User] ON [Notifications] ([UserId], [IsRead], [CreatedAt] DESC);
GO

CREATE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
GO

CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
GO

CREATE INDEX [IX_PostAmenities_AmenityId] ON [PostAmenities] ([AmenityId]);
GO

CREATE INDEX [IX_PostImages_PostId] ON [PostImages] ([PostId]);
GO

CREATE INDEX [IX_Posts_AreaId] ON [Posts] ([AreaId]);
GO

CREATE INDEX [IX_Posts_ExpiredAt] ON [Posts] ([ExpiredAt]) WHERE [Status] = N'approved';
GO

CREATE INDEX [IX_Posts_ModeratedBy] ON [Posts] ([ModeratedBy]);
GO

CREATE INDEX [IX_Posts_Search] ON [Posts] ([Status], [AreaId], [PostType], [Price]) INCLUDE ([PreferredGender], [NeededOccupants], [ExpiredAt], [Latitude], [Longitude]) WHERE [IsDeleted] = 0;
GO

CREATE INDEX [IX_Posts_User] ON [Posts] ([UserId], [Status]);
GO

CREATE INDEX [IX_Reports_HandledBy] ON [Reports] ([HandledBy]);
GO

CREATE INDEX [IX_Reports_Post] ON [Reports] ([PostId], [CreatedAt]) WHERE [PostId] IS NOT NULL;
GO

CREATE INDEX [IX_Reports_ReasonId] ON [Reports] ([ReasonId]);
GO

CREATE INDEX [IX_Reports_ReportedUserId] ON [Reports] ([ReportedUserId]);
GO

CREATE INDEX [IX_Reports_Status] ON [Reports] ([Status], [CreatedAt]);
GO

CREATE UNIQUE INDEX [UQ_Report_ReporterPost_Pending] ON [Reports] ([ReporterId], [PostId]) WHERE [Status] = N'pending' AND [PostId] IS NOT NULL;
GO

CREATE INDEX [IX_Reviews_RequestId] ON [Reviews] ([RequestId]);
GO

CREATE INDEX [IX_Reviews_Reviewee] ON [Reviews] ([RevieweeId]) WHERE [IsHidden] = 0;
GO

CREATE UNIQUE INDEX [UQ_Review_ReviewerRequest] ON [Reviews] ([ReviewerId], [RequestId]);
GO

CREATE INDEX [IX_SavedPosts_PostId] ON [SavedPosts] ([PostId]);
GO

CREATE UNIQUE INDEX [UQ_Saved_UserPost] ON [SavedPosts] ([UserId], [PostId]);
GO

CREATE INDEX [IX_Users_LandmarkId] ON [Users] ([LandmarkId]);
GO

CREATE INDEX [IX_Users_Role_Status] ON [Users] ([Role], [Status]);
GO

CREATE UNIQUE INDEX [UQ_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_ViolationHistory_HandledBy] ON [ViolationHistory] ([HandledBy]);
GO

CREATE INDEX [IX_ViolationHistory_PostId] ON [ViolationHistory] ([PostId]);
GO

CREATE INDEX [IX_ViolationHistory_ReportId] ON [ViolationHistory] ([ReportId]);
GO

CREATE INDEX [IX_ViolationHistory_UserId] ON [ViolationHistory] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261006090017_InitialCreate', N'8.0.31');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [SecurityStamp] nvarchar(32) NOT NULL DEFAULT (REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''));
GO

ALTER TABLE [Posts] ADD [RowVersion] rowversion NOT NULL;
GO

ALTER TABLE [Posts] ADD [SubmittedAt] datetime2 NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261007122055_AddRowVersionSecurityStampSubmittedAt', N'8.0.31');
GO

COMMIT;
GO

