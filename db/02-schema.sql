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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(80) NOT NULL,
        [LastName] nvarchar(80) NOT NULL,
        [Email] nvarchar(160) NOT NULL,
        [DepartmentId] int NOT NULL,
        [Position] nvarchar(100) NOT NULL,
        [HireDate] date NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Email] nvarchar(160) NOT NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [Role] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [Workdays] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Date] date NOT NULL,
        [CheckIn] datetime2 NOT NULL,
        [CheckOut] datetime2 NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Workdays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Workdays_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [AiQueryLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Question] nvarchar(1000) NOT NULL,
        [ToolsUsed] nvarchar(256) NULL,
        [ResponseStatus] nvarchar(40) NOT NULL,
        [DurationMs] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AiQueryLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiQueryLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Action] nvarchar(80) NOT NULL,
        [Entity] nvarchar(80) NOT NULL,
        [EntityId] nvarchar(64) NULL,
        [Details] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE TABLE [Breaks] (
        [Id] int NOT NULL IDENTITY,
        [WorkdayId] int NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NULL,
        CONSTRAINT [PK_Breaks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Breaks_Workdays_WorkdayId] FOREIGN KEY ([WorkdayId]) REFERENCES [Workdays] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiQueryLogs_UserId] ON [AiQueryLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Breaks_WorkdayId] ON [Breaks] ([WorkdayId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Departments_Name] ON [Departments] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_EmployeeId] ON [Users] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Workdays_EmployeeId] ON [Workdays] ([EmployeeId]) WHERE [CheckOut] IS NULL AND [Status] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721040432_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721040432_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [AbsenceTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(40) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ConsumesVacationBalance] bit NOT NULL,
        [RequiresApproval] bit NOT NULL,
        [ColorHex] nvarchar(7) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AbsenceTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [Holidays] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Name] nvarchar(120) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [Schedules] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Schedules] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [VacationAllowances] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Year] int NOT NULL,
        [Days] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_VacationAllowances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VacationAllowances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [AbsenceRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [AbsenceTypeId] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [WorkingDays] decimal(5,2) NOT NULL,
        [Status] int NOT NULL,
        [Reason] nvarchar(500) NULL,
        [RequestedAt] datetime2 NOT NULL,
        [DecidedAt] datetime2 NULL,
        [DecidedByUserId] int NULL,
        [DecisionComment] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AbsenceRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AbsenceRequests_AbsenceTypes_AbsenceTypeId] FOREIGN KEY ([AbsenceTypeId]) REFERENCES [AbsenceTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AbsenceRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AbsenceRequests_Users_DecidedByUserId] FOREIGN KEY ([DecidedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [ScheduleAssignments] (
        [Id] int NOT NULL IDENTITY,
        [ScheduleId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ScheduleAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduleAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ScheduleAssignments_Schedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [Schedules] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE TABLE [ScheduleSlots] (
        [Id] int NOT NULL IDENTITY,
        [ScheduleId] int NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        CONSTRAINT [PK_ScheduleSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScheduleSlots_Schedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [Schedules] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_AbsenceRequests_AbsenceTypeId] ON [AbsenceRequests] ([AbsenceTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_AbsenceRequests_DecidedByUserId] ON [AbsenceRequests] ([DecidedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_AbsenceRequests_EmployeeId_StartDate] ON [AbsenceRequests] ([EmployeeId], [StartDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_AbsenceRequests_Status] ON [AbsenceRequests] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AbsenceTypes_Code] ON [AbsenceTypes] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Holidays_Date] ON [Holidays] ([Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_ScheduleAssignments_EmployeeId_StartDate] ON [ScheduleAssignments] ([EmployeeId], [StartDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_ScheduleAssignments_ScheduleId] ON [ScheduleAssignments] ([ScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Schedules_Name] ON [Schedules] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE INDEX [IX_ScheduleSlots_ScheduleId_DayOfWeek] ON [ScheduleSlots] ([ScheduleId], [DayOfWeek]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VacationAllowances_EmployeeId_Year] ON [VacationAllowances] ([EmployeeId], [Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153746_SchedulesAbsencesVacations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722153746_SchedulesAbsencesVacations', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153941_SeedAbsenceTypes'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ColorHex', N'ConsumesVacationBalance', N'CreatedAt', N'IsActive', N'Name', N'RequiresApproval', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AbsenceTypes]'))
        SET IDENTITY_INSERT [AbsenceTypes] ON;
    EXEC(N'INSERT INTO [AbsenceTypes] ([Id], [Code], [ColorHex], [ConsumesVacationBalance], [CreatedAt], [IsActive], [Name], [RequiresApproval], [UpdatedAt])
    VALUES (1, N''VACACIONES'', N''#16b98a'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Vacaciones'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z''),
    (2, N''ENFERMEDAD'', N''#f43f5e'', CAST(0 AS bit), ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Baja por enfermedad'', CAST(0 AS bit), ''2026-01-01T00:00:00.0000000Z''),
    (3, N''ASUNTOS_PROPIOS'', N''#f59e0b'', CAST(0 AS bit), ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Asuntos propios'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z''),
    (4, N''PERMISO'', N''#3b82f6'', CAST(0 AS bit), ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Permiso retribuido'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z''),
    (5, N''SIN_SUELDO'', N''#94a3b8'', CAST(0 AS bit), ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), N''Permiso sin sueldo'', CAST(1 AS bit), ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'ColorHex', N'ConsumesVacationBalance', N'CreatedAt', N'IsActive', N'Name', N'RequiresApproval', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AbsenceTypes]'))
        SET IDENTITY_INSERT [AbsenceTypes] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722153941_SeedAbsenceTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722153941_SeedAbsenceTypes', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    DROP INDEX [IX_Holidays_Date] ON [Holidays];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    ALTER TABLE [Holidays] ADD [Kind] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    ALTER TABLE [Holidays] ADD [WorkCalendarId] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    CREATE TABLE [WorkCalendars] (
        [Id] int NOT NULL IDENTITY,
        [Year] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [NonWorkingWeekDaysMask] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_WorkCalendars] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Holidays_WorkCalendarId_Date] ON [Holidays] ([WorkCalendarId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    CREATE UNIQUE INDEX [IX_WorkCalendars_Year] ON [WorkCalendars] ([Year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    ALTER TABLE [Holidays] ADD CONSTRAINT [FK_Holidays_WorkCalendars_WorkCalendarId] FOREIGN KEY ([WorkCalendarId]) REFERENCES [WorkCalendars] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722155353_WorkCalendar'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722155353_WorkCalendar', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200001_UserAvatar'
)
BEGIN
    ALTER TABLE [Users] ADD [AvatarUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722200001_UserAvatar'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722200001_UserAvatar', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    EXEC(N'UPDATE [AbsenceTypes] SET [ColorHex] = N''#1baf7a''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    EXEC(N'UPDATE [AbsenceTypes] SET [ColorHex] = N''#e34948''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    EXEC(N'UPDATE [AbsenceTypes] SET [ColorHex] = N''#eda100''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    EXEC(N'UPDATE [AbsenceTypes] SET [ColorHex] = N''#2a78d6''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    EXEC(N'UPDATE [AbsenceTypes] SET [ColorHex] = N''#4a3aa7''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723042615_AbsenceTypeColors'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723042615_AbsenceTypeColors', N'8.0.10');
END;
GO

COMMIT;
GO

