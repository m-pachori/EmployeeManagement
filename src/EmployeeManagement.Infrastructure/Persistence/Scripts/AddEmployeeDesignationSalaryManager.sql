BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    ALTER TABLE [Employees] ADD [Designation] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    ALTER TABLE [Employees] ADD [ManagerId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    ALTER TABLE [Employees] ADD [Salary] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId_Status] ON [Employees] ([DepartmentId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    CREATE INDEX [IX_Employees_ManagerId] ON [Employees] ([ManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722170859_AddEmployeeDesignationSalaryManager'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722170859_AddEmployeeDesignationSalaryManager', N'8.0.29');
END;
GO

COMMIT;
GO

