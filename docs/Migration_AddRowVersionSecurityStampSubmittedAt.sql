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

