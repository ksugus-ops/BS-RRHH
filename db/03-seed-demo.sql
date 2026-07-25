/* ==========================================================================
   HRIA — 03. Datos de demostración (OPCIONAL)
   --------------------------------------------------------------------------
   Ejecutar CONECTADO A LA BASE DE DATOS 'HRIA', después de 02-schema.sql.

   IMPORTANTE:
   - Este script es IDEMPOTENTE: no hace nada si ya existen usuarios.
   - Solo es necesario si arrancas la API con Demo__Enabled=false. Si la dejas
     en true, la propia aplicación siembra estos datos al arrancar.
   - Contraseña de ambos usuarios demo: Demo1234!
     El hash es PBKDF2-HMAC-SHA256, 100.000 iteraciones, formato
     {iteraciones}.{salt-base64}.{hash-base64}  (validado por pruebas unitarias).
   - NO uses estas credenciales en un entorno real.
   ========================================================================== */

SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM [Users])
BEGIN
    PRINT 'Ya existen usuarios: no se siembran datos de demostración.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    DECLARE @now      datetime2     = SYSUTCDATETIME();
    DECLARE @today    date          = CAST(SYSUTCDATETIME() AS date);
    DECLARE @demoHash nvarchar(256) =
        N'100000.WqdvmnxDjI8uKe85rD4O9w==.+jYPMHu04rG7iqrwej0oqxJn6NdNRthR9dhiwl75F6g=';

    /* ---------- Departamentos ---------- */
    INSERT INTO [Departments] ([Name], [IsActive]) VALUES
        (N'Desarrollo',       1),
        (N'Recursos Humanos', 1),
        (N'Ventas',           1),
        (N'Operaciones',      1);

    DECLARE @devId  int = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Desarrollo');
    DECLARE @rrhhId int = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Recursos Humanos');
    DECLARE @venId  int = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Ventas');
    DECLARE @opsId  int = (SELECT [Id] FROM [Departments] WHERE [Name] = N'Operaciones');

    /* ---------- Empleados (10) ---------- */
    INSERT INTO [Employees]
        ([FirstName], [LastName], [Email], [DepartmentId], [Position], [HireDate], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES
        (N'Ana',    N'Admin',    N'admin@hria.local',         @rrhhId, N'Responsable de RR. HH.', DATEADD(YEAR,-5,@today), 1, @now, @now),
        (N'Eva',    N'Empleada', N'empleado@hria.local',      @devId,  N'Desarrolladora',         DATEADD(YEAR,-2,@today), 1, @now, @now),
        (N'Carlos', N'Gomez',    N'carlos.gomez@hria.local',  @devId,  N'Desarrollador Senior',   DATEADD(YEAR,-4,@today), 1, @now, @now),
        (N'Marta',  N'Ruiz',     N'marta.ruiz@hria.local',    @devId,  N'QA Engineer',            DATEADD(YEAR,-3,@today), 1, @now, @now),
        (N'Luis',   N'Perez',    N'luis.perez@hria.local',    @venId,  N'Comercial',              DATEADD(YEAR,-6,@today), 1, @now, @now),
        (N'Sara',   N'Lopez',    N'sara.lopez@hria.local',    @venId,  N'Comercial',              DATEADD(YEAR,-1,@today), 1, @now, @now),
        (N'Javier', N'Moreno',   N'javier.moreno@hria.local', @opsId,  N'Técnico de Operaciones', DATEADD(YEAR,-2,@today), 1, @now, @now),
        (N'Lucia',  N'Diaz',     N'lucia.diaz@hria.local',    @opsId,  N'Coordinadora',           DATEADD(YEAR,-7,@today), 1, @now, @now),
        (N'Pablo',  N'Sanz',     N'pablo.sanz@hria.local',    @rrhhId, N'Técnico de RR. HH.',     DATEADD(YEAR,-3,@today), 1, @now, @now),
        (N'Nuria',  N'Vidal',    N'nuria.vidal@hria.local',   @devId,  N'Diseñadora UX',          DATEADD(YEAR,-2,@today), 1, @now, @now);

    DECLARE @adminEmp  int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'admin@hria.local');
    DECLARE @demoEmp   int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'empleado@hria.local');
    DECLARE @carlosEmp int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'carlos.gomez@hria.local');
    DECLARE @martaEmp  int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'marta.ruiz@hria.local');
    DECLARE @luisEmp   int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'luis.perez@hria.local');
    DECLARE @saraEmp   int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'sara.lopez@hria.local');
    DECLARE @javierEmp int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'javier.moreno@hria.local');
    DECLARE @luciaEmp  int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'lucia.diaz@hria.local');
    DECLARE @pabloEmp  int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'pablo.sanz@hria.local');
    DECLARE @nuriaEmp  int = (SELECT [Id] FROM [Employees] WHERE [Email] = N'nuria.vidal@hria.local');

    /* ---------- Usuarios de acceso (Role: 1 = Admin, 2 = Employee) ---------- */
    INSERT INTO [Users] ([EmployeeId], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES
        (@adminEmp, N'admin@hria.local',    @demoHash, 1, 1, @now, @now),
        (@demoEmp,  N'empleado@hria.local', @demoHash, 2, 1, @now, @now);

    /* ---------- Jornadas completas de los 3 días anteriores ----------
       08:00–16:30 con 30 min de descanso => 8 h trabajadas.
       Status: 1 = Open, 2 = Completed, 3 = Incomplete                    */
    DECLARE @emps TABLE (EmployeeId int);
    INSERT INTO @emps VALUES (@demoEmp), (@carlosEmp), (@martaEmp), (@luisEmp), (@luciaEmp);

    DECLARE @d int = 1;
    DECLARE @day date;
    WHILE @d <= 3
    BEGIN
        SET @day = DATEADD(DAY, -@d, @today);

        INSERT INTO [Workdays] ([EmployeeId], [Date], [CheckIn], [CheckOut], [Status], [CreatedAt], [UpdatedAt])
        SELECT EmployeeId, @day,
               DATEADD(HOUR, 8, CAST(@day AS datetime2)),
               DATEADD(MINUTE, 990, CAST(@day AS datetime2)),   -- 16:30
               2, @now, @now
        FROM @emps;

        INSERT INTO [Breaks] ([WorkdayId], [StartTime], [EndTime])
        SELECT w.[Id],
               DATEADD(HOUR, 12, CAST(@day AS datetime2)),
               DATEADD(MINUTE, 750, CAST(@day AS datetime2))     -- 12:30
        FROM [Workdays] w
        WHERE w.[Date] = @day AND w.[Status] = 2;

        SET @d = @d + 1;
    END

    /* ---------- Empleados TRABAJANDO ahora (jornada abierta) ---------- */
    INSERT INTO [Workdays] ([EmployeeId], [Date], [CheckIn], [CheckOut], [Status], [CreatedAt], [UpdatedAt])
    VALUES
        (@carlosEmp, @today, DATEADD(HOUR, -2, @now), NULL, 1, @now, @now),
        (@saraEmp,   @today, DATEADD(HOUR, -2, @now), NULL, 1, @now, @now);

    /* ---------- Empleado EN DESCANSO ahora (jornada abierta + descanso abierto) ---------- */
    INSERT INTO [Workdays] ([EmployeeId], [Date], [CheckIn], [CheckOut], [Status], [CreatedAt], [UpdatedAt])
    VALUES (@javierEmp, @today, DATEADD(HOUR, -3, @now), NULL, 1, @now, @now);

    DECLARE @javierWorkday int = SCOPE_IDENTITY();
    INSERT INTO [Breaks] ([WorkdayId], [StartTime], [EndTime])
    VALUES (@javierWorkday, DATEADD(MINUTE, -15, @now), NULL);

    /* ---------- Jornadas INCOMPLETAS (entrada sin salida, día pasado) ---------- */
    DECLARE @twoAgo date = DATEADD(DAY, -2, @today);
    INSERT INTO [Workdays] ([EmployeeId], [Date], [CheckIn], [CheckOut], [Status], [CreatedAt], [UpdatedAt])
    VALUES
        (@pabloEmp, @twoAgo, DATEADD(HOUR, 9, CAST(@twoAgo AS datetime2)), NULL, 3, @now, @now),
        (@nuriaEmp, @twoAgo, DATEADD(HOUR, 9, CAST(@twoAgo AS datetime2)), NULL, 3, @now, @now);

    COMMIT TRANSACTION;

    PRINT 'Datos de demostración insertados.';
    PRINT 'Usuarios: admin@hria.local / empleado@hria.local  ·  Contraseña: Demo1234!';
END
GO
