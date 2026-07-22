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

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(150) NOT NULL,
    [Module] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemSettings] (
    [Id] int NOT NULL IDENTITY,
    [Category] nvarchar(100) NOT NULL,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(4000) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [UserName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(1000) NOT NULL,
    [IsActive] bit NOT NULL,
    [FailedLoginAttempts] int NOT NULL,
    [LockoutEndUtc] datetime2 NULL,
    [PasswordChangedAtUtc] datetime2 NOT NULL,
    [PasswordExpiresAtUtc] datetime2 NOT NULL,
    [LastLoginAtUtc] datetime2 NULL,
    [PasswordResetTokenHash] nvarchar(200) NULL,
    [PasswordResetTokenExpiresAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeCode] nvarchar(50) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [PhotoUrl] nvarchar(500) NULL,
    [DateOfJoining] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [DepartmentId] int NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [EventType] nvarchar(100) NOT NULL,
    [EntityName] nvarchar(150) NOT NULL,
    [EntityId] nvarchar(100) NULL,
    [Details] nvarchar(4000) NULL,
    [IpAddress] nvarchar(64) NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] nvarchar(200) NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedByIp] nvarchar(64) NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    [RevokedByIp] nvarchar(64) NULL,
    [ReplacedByTokenHash] nvarchar(200) NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [EmployeeDocuments] (
    [Id] int NOT NULL IDENTITY,
    [EmployeeId] int NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [ContentType] nvarchar(150) NOT NULL,
    [SizeInBytes] bigint NOT NULL,
    [UploadedAtUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    [UpdatedDate] datetime2 NULL,
    CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_AuditLogs_CreatedDate] ON [AuditLogs] ([CreatedDate]);
GO

CREATE INDEX [IX_AuditLogs_EventType] ON [AuditLogs] ([EventType]);
GO

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_Departments_Code] ON [Departments] ([Code]);
GO

CREATE INDEX [IX_Departments_IsActive] ON [Departments] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
GO

CREATE INDEX [IX_EmployeeDocuments_EmployeeId] ON [EmployeeDocuments] ([EmployeeId]);
GO

CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
GO

CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
GO

CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
GO

CREATE INDEX [IX_Employees_Status] ON [Employees] ([Status]);
GO

CREATE UNIQUE INDEX [IX_Permissions_Code] ON [Permissions] ([Code]);
GO

CREATE INDEX [IX_Permissions_Module_Action] ON [Permissions] ([Module], [Action]);
GO

CREATE INDEX [IX_RefreshTokens_ExpiresAtUtc] ON [RefreshTokens] ([ExpiresAtUtc]);
GO

CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
GO

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO

CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
GO

CREATE UNIQUE INDEX [IX_SystemSettings_Category_Key] ON [SystemSettings] ([Category], [Key]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260722120933_InitialPhase2Schema', N'8.0.29');
GO

COMMIT;
GO

