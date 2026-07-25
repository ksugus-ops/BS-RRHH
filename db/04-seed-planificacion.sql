/* ==========================================================================
   HRIA — 04. Datos de demostración de planificación (opcional)
   --------------------------------------------------------------------------
   Calendario laboral 2026, horarios, asignaciones, saldos de vacaciones y
   solicitudes de ausencia repartidas por el año.

   Ejecutar CONECTADO A LA BASE DE DATOS 'HRIA', después de 03-seed-demo.sql.
   Es IDEMPOTENTE: se puede ejecutar varias veces sin duplicar nada.

   Sirve para que los calendarios y el contraste previsto/real tengan datos
   suficientes en la demostración. NO ejecutar en un entorno real.
   ========================================================================== */

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Employees)
BEGIN
    RAISERROR('No hay empleados. Ejecuta antes db/03-seed-demo.sql.', 16, 1);
    RETURN;
END

DECLARE @now datetime2 = SYSUTCDATETIME();
DECLARE @year int = 2026;

/* ------------------------------------------------------------------
   1. Calendario laboral 2026 (sábado y domingo no laborables)
   ------------------------------------------------------------------ */
IF NOT EXISTS (SELECT 1 FROM WorkCalendars WHERE [Year] = @year)
BEGIN
    INSERT INTO WorkCalendars ([Year], [Name], NonWorkingWeekDaysMask, IsActive, CreatedAt, UpdatedAt)
    VALUES (@year, CONCAT('Calendario laboral ', @year), 65, 1, @now, @now);  -- 65 = bit 0 (domingo) + bit 6 (sábado)
END

DECLARE @calId int = (SELECT Id FROM WorkCalendars WHERE [Year] = @year);

/* Festivos: nacionales de 2026 más dos días de convenio.
   Kind: 1 nacional, 2 autonómico, 3 local, 4 convenio, 5 empresa. */
;WITH Festivos([Date], [Name], Kind) AS (
    SELECT * FROM (VALUES
        ('2026-01-01', 'Año Nuevo',                      1),
        ('2026-01-06', 'Epifanía del Señor',             1),
        ('2026-04-03', 'Viernes Santo',                  1),
        ('2026-05-01', 'Fiesta del Trabajo',             1),
        ('2026-08-15', 'Asunción de la Virgen',          1),
        ('2026-10-12', 'Fiesta Nacional de España',      1),
        ('2026-11-02', 'Todos los Santos (trasladado)',  1),
        ('2026-12-07', 'Día de la Constitución (trasl.)',1),
        ('2026-12-08', 'Inmaculada Concepción',          1),
        ('2026-12-25', 'Navidad',                        1),
        ('2026-05-04', 'Puente de convenio',             4),
        ('2026-12-24', 'Nochebuena (convenio)',          4)
    ) AS t([Date], [Name], Kind)
)
INSERT INTO Holidays (WorkCalendarId, [Date], [Name], Kind, CreatedAt, UpdatedAt)
SELECT @calId, f.[Date], f.[Name], f.Kind, @now, @now
FROM Festivos f
WHERE NOT EXISTS (
    SELECT 1 FROM Holidays h WHERE h.WorkCalendarId = @calId AND h.[Date] = f.[Date]
);

/* ------------------------------------------------------------------
   2. Horarios
   ------------------------------------------------------------------ */
;WITH Horarios([Name], [Description]) AS (
    SELECT * FROM (VALUES
        ('Jornada completa L-V',  'Ocho horas de lunes a viernes, jornada partida'),
        ('Jornada intensiva',     'Siete horas de lunes a viernes, sin pausa de comida'),
        ('Media jornada mañanas', 'Cuatro horas de lunes a viernes'),
        ('Turno de tarde',        'Ocho horas de lunes a viernes, tarde')
    ) AS t([Name], [Description])
)
INSERT INTO Schedules ([Name], [Description], IsActive, CreatedAt, UpdatedAt)
SELECT h.[Name], h.[Description], 1, @now, @now
FROM Horarios h
WHERE NOT EXISTS (SELECT 1 FROM Schedules s WHERE s.[Name] = h.[Name]);

DECLARE @completa int = (SELECT Id FROM Schedules WHERE [Name] = 'Jornada completa L-V');
DECLARE @intensiva int = (SELECT Id FROM Schedules WHERE [Name] = 'Jornada intensiva');
DECLARE @media int = (SELECT Id FROM Schedules WHERE [Name] = 'Media jornada mañanas');
DECLARE @tarde int = (SELECT Id FROM Schedules WHERE [Name] = 'Turno de tarde');

/* Tramos. DayOfWeek: 1 lunes … 5 viernes. */
;WITH Tramos(ScheduleId, DayOfWeek, StartTime, EndTime) AS (
    SELECT @completa, d, '09:00:00', '14:00:00' FROM (VALUES (1),(2),(3),(4),(5)) AS x(d)
    UNION ALL SELECT @completa, d, '15:00:00', '18:00:00' FROM (VALUES (1),(2),(3),(4),(5)) AS x(d)
    UNION ALL SELECT @intensiva, d, '08:00:00', '15:00:00' FROM (VALUES (1),(2),(3),(4),(5)) AS x(d)
    UNION ALL SELECT @media,     d, '09:00:00', '13:00:00' FROM (VALUES (1),(2),(3),(4),(5)) AS x(d)
    UNION ALL SELECT @tarde,     d, '14:00:00', '22:00:00' FROM (VALUES (1),(2),(3),(4),(5)) AS x(d)
)
INSERT INTO ScheduleSlots (ScheduleId, DayOfWeek, StartTime, EndTime)
SELECT t.ScheduleId, t.DayOfWeek, t.StartTime, t.EndTime
FROM Tramos t
WHERE NOT EXISTS (
    SELECT 1 FROM ScheduleSlots s
    WHERE s.ScheduleId = t.ScheduleId AND s.DayOfWeek = t.DayOfWeek AND s.StartTime = t.StartTime
);

/* ------------------------------------------------------------------
   3. Asignación de horarios: se reparten entre los empleados activos
   ------------------------------------------------------------------ */
;WITH Numerados AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Employees WHERE IsActive = 1
)
INSERT INTO ScheduleAssignments (ScheduleId, EmployeeId, StartDate, EndDate, CreatedAt, UpdatedAt)
SELECT
    CASE n.rn % 4
        WHEN 1 THEN @completa
        WHEN 2 THEN @intensiva
        WHEN 3 THEN @media
        ELSE @tarde
    END,
    n.Id, '2026-01-01', NULL, @now, @now
FROM Numerados n
WHERE NOT EXISTS (SELECT 1 FROM ScheduleAssignments a WHERE a.EmployeeId = n.Id);

/* ------------------------------------------------------------------
   4. Saldo de vacaciones: 23 días para toda la plantilla activa
   ------------------------------------------------------------------ */
INSERT INTO VacationAllowances (EmployeeId, [Year], [Days], CreatedAt, UpdatedAt)
SELECT e.Id, @year, 23, @now, @now
FROM Employees e
WHERE e.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM VacationAllowances v WHERE v.EmployeeId = e.Id AND v.[Year] = @year);

/* ------------------------------------------------------------------
   5. Solicitudes de ausencia repartidas por el año
   --------------------------------------------------------------------------
   WorkingDays va calculado a mano de acuerdo con el calendario de arriba
   (laborables de lunes a viernes descontando los festivos que caigan dentro).
   Status: 1 pendiente, 2 aprobada, 3 rechazada, 4 retirada.
   AbsenceTypeId: 1 vacaciones, 2 enfermedad, 3 asuntos propios, 4 permiso.
   ------------------------------------------------------------------ */
DECLARE @admin int = (SELECT TOP 1 Id FROM Users WHERE Role = 1 ORDER BY Id);

;WITH Solicitudes(rn, TypeId, StartDate, EndDate, WorkingDays, Status, Reason) AS (
    SELECT * FROM (VALUES
        (1, 1, '2026-08-03', '2026-08-14', 10.0, 2, 'Vacaciones de verano'),
        (2, 1, '2026-07-06', '2026-07-17', 10.0, 2, 'Vacaciones de verano'),
        (3, 1, '2026-08-17', '2026-08-28',  9.0, 2, 'Segunda quincena de agosto'),
        (4, 1, '2026-04-06', '2026-04-10',  5.0, 2, 'Semana Santa'),
        (5, 1, '2026-09-07', '2026-09-18', 10.0, 1, 'Vacaciones de septiembre'),
        (6, 1, '2026-12-28', '2026-12-31',  3.0, 1, 'Fin de año'),
        (7, 1, '2026-06-01', '2026-06-12', 10.0, 2, 'Vacaciones de junio'),
        (8, 1, '2026-11-02', '2026-11-06',  4.0, 1, 'Puente de noviembre'),
        (9, 1, '2026-05-04', '2026-05-08',  4.0, 2, 'Puente de mayo'),
        (10,1, '2026-10-05', '2026-10-16', 10.0, 3, 'Coincide con cierre trimestral')
    ) AS t(rn, TypeId, StartDate, EndDate, WorkingDays, Status, Reason)
),
Numerados AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Employees WHERE IsActive = 1
)
INSERT INTO AbsenceRequests
    (EmployeeId, AbsenceTypeId, StartDate, EndDate, WorkingDays, Status, Reason,
     RequestedAt, DecidedAt, DecidedByUserId, DecisionComment, CreatedAt, UpdatedAt)
SELECT
    n.Id, s.TypeId, s.StartDate, s.EndDate, s.WorkingDays, s.Status, s.Reason,
    DATEADD(day, -45, @now),
    CASE WHEN s.Status IN (2, 3) THEN DATEADD(day, -40, @now) END,
    CASE WHEN s.Status IN (2, 3) THEN @admin END,
    CASE WHEN s.Status = 3 THEN 'No es posible en esas fechas.' END,
    @now, @now
FROM Solicitudes s
JOIN Numerados n ON n.rn = s.rn
WHERE NOT EXISTS (
    SELECT 1 FROM AbsenceRequests a
    WHERE a.EmployeeId = n.Id AND a.StartDate = s.StartDate
);

/* Algunas ausencias cortas que no consumen vacaciones. */
;WITH Cortas(rn, TypeId, StartDate, EndDate, WorkingDays, Status, Reason) AS (
    SELECT * FROM (VALUES
        (2, 2, '2026-02-09', '2026-02-11', 3.0, 2, 'Gripe'),
        (4, 3, '2026-03-16', '2026-03-16', 1.0, 2, 'Asunto propio'),
        (6, 2, '2026-05-18', '2026-05-20', 3.0, 2, 'Baja médica'),
        (8, 4, '2026-09-24', '2026-09-25', 2.0, 1, 'Permiso por mudanza')
    ) AS t(rn, TypeId, StartDate, EndDate, WorkingDays, Status, Reason)
),
Numerados AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Employees WHERE IsActive = 1
)
INSERT INTO AbsenceRequests
    (EmployeeId, AbsenceTypeId, StartDate, EndDate, WorkingDays, Status, Reason,
     RequestedAt, DecidedAt, DecidedByUserId, DecisionComment, CreatedAt, UpdatedAt)
SELECT
    n.Id, c.TypeId, c.StartDate, c.EndDate, c.WorkingDays, c.Status, c.Reason,
    DATEADD(day, -30, @now),
    CASE WHEN c.Status = 2 THEN DATEADD(day, -29, @now) END,
    CASE WHEN c.Status = 2 THEN @admin END,
    NULL, @now, @now
FROM Cortas c
JOIN Numerados n ON n.rn = c.rn
WHERE NOT EXISTS (
    SELECT 1 FROM AbsenceRequests a
    WHERE a.EmployeeId = n.Id AND a.StartDate = c.StartDate
);

/* ------------------------------------------------------------------
   5-bis. Ausencias ancladas a la SEMANA EN CURSO
   --------------------------------------------------------------------------
   Las de arriba tienen fechas fijas de 2026 y pueden quedar lejos del día en
   que se ejecute la demostración, dejando vacía la tabla de "esta semana y la
   próxima" del panel. Estas se calculan desde la fecha actual para que esa
   tabla siempre tenga contenido.
   ------------------------------------------------------------------ */
DECLARE @hoy date = CAST(SYSUTCDATETIME() AS date);
/* Lunes de esta semana (DATEFIRST puede variar entre sesiones; se calcula sin depender de él). */
DECLARE @lunes date = DATEADD(day, -((DATEDIFF(day, '1900-01-01', @hoy) + 0) % 7), @hoy);

;WITH Semana(rn, TypeId, Desde, Hasta, Dias, Status, Reason) AS (
    SELECT * FROM (VALUES
        (3, 1, 2,  4, 3.0, 2, 'Días sueltos de vacaciones'),
        (5, 2, 0,  1, 2.0, 2, 'Baja médica'),
        (7, 1, 7, 11, 5.0, 1, 'Vacaciones la próxima semana'),
        (9, 3, 8,  8, 1.0, 1, 'Asunto propio')
    ) AS t(rn, TypeId, Desde, Hasta, Dias, Status, Reason)
),
Numerados AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Employees WHERE IsActive = 1
)
INSERT INTO AbsenceRequests
    (EmployeeId, AbsenceTypeId, StartDate, EndDate, WorkingDays, Status, Reason,
     RequestedAt, DecidedAt, DecidedByUserId, DecisionComment, CreatedAt, UpdatedAt)
SELECT
    n.Id, s.TypeId,
    DATEADD(day, s.Desde, @lunes), DATEADD(day, s.Hasta, @lunes),
    s.Dias, s.Status, s.Reason,
    DATEADD(day, -7, @now),
    CASE WHEN s.Status = 2 THEN DATEADD(day, -6, @now) END,
    CASE WHEN s.Status = 2 THEN @admin END,
    NULL, @now, @now
FROM Semana s
JOIN Numerados n ON n.rn = s.rn
WHERE NOT EXISTS (
    /* Idempotente pese a que las fechas se muevan: basta con que el empleado no
       tenga ya una ausencia solapada con la ventana de dos semanas. */
    SELECT 1 FROM AbsenceRequests a
    WHERE a.EmployeeId = n.Id
      AND a.StartDate <= DATEADD(day, 13, @lunes)
      AND DATEADD(day, s.Desde, @lunes) <= a.EndDate
);

/* ------------------------------------------------------------------
   5-ter. Alinear los fichajes con el horario asignado
   --------------------------------------------------------------------------
   Las jornadas de 03-seed-demo.sql se sembraron con horas fijas (10:00 a
   18:30) antes de que existieran los horarios. Al asignarlos, alguien con
   turno de tarde aparecía entrando seis horas antes, y el indicador de
   puntualidad daba 0 % correctamente sobre un dato absurdo.

   Aquí se recolocan sobre el horario de cada empleado con un desvío
   determinista: la mayoría dentro del margen y una parte con retraso o salida
   anticipada, para que el indicador tenga una mezcla realista.

   ⚠️ Solo para la base de demostración: sobrescribe las horas de TODAS las
   jornadas cerradas.
   ------------------------------------------------------------------ */
;WITH Prevision AS (
    SELECT
        w.Id,
        w.[Date],
        MIN(sl.StartTime) AS Entrada,
        MAX(sl.EndTime)   AS Salida
    FROM Workdays w
    JOIN ScheduleAssignments a
      ON a.EmployeeId = w.EmployeeId
     AND a.StartDate <= w.[Date]
     AND (a.EndDate IS NULL OR w.[Date] <= a.EndDate)
    JOIN ScheduleSlots sl
      ON sl.ScheduleId = a.ScheduleId
      /* Día de la semana con la convención de .NET (0 = domingo) y sin
         depender de DATEFIRST, que varía entre sesiones. */
     AND sl.DayOfWeek = ((DATEDIFF(day, '1900-01-01', w.[Date]) + 1) % 7)
    WHERE w.CheckOut IS NOT NULL
    GROUP BY w.Id, w.[Date]
)
UPDATE w
SET
    CheckIn = CAST(
        DATEADD(minute,
            CASE w.Id % 5 WHEN 0 THEN 1 WHEN 1 THEN 2 WHEN 2 THEN -3 WHEN 3 THEN 22 ELSE 3 END,
            CAST(CAST(p.[Date] AS datetime) + CAST(p.Entrada AS datetime) AS datetime2))
        AT TIME ZONE 'Romance Standard Time' AT TIME ZONE 'UTC' AS datetime2),
    CheckOut = CAST(
        DATEADD(minute,
            CASE w.Id % 7 WHEN 0 THEN 2 WHEN 1 THEN -25 WHEN 2 THEN 4 WHEN 3 THEN 1 ELSE 0 END,
            CAST(CAST(p.[Date] AS datetime) + CAST(p.Salida AS datetime) AS datetime2))
        AT TIME ZONE 'Romance Standard Time' AT TIME ZONE 'UTC' AS datetime2),
    UpdatedAt = @now
FROM Workdays w
JOIN Prevision p ON p.Id = w.Id;

/* ------------------------------------------------------------------
   6. Avatares de los usuarios con acceso
   --------------------------------------------------------------------------
   Ficheros servidos por el propio frontend (frontend/public/avatars/).
   Si la ruta no existe, la interfaz cae a las iniciales automáticamente.
   ------------------------------------------------------------------ */
UPDATE u
SET AvatarUrl = '/avatars/' + LOWER(LEFT(u.Email, CHARINDEX('@', u.Email) - 1)) + '.svg',
    UpdatedAt = @now
FROM Users u
WHERE u.AvatarUrl IS NULL;

/* Las subconsultas no se admiten dentro de PRINT: se vuelcan a variables. */
DECLARE @nFestivos int, @nHorarios int, @nAsig int, @nSaldos int, @nSolic int;
SELECT @nFestivos = COUNT(*) FROM Holidays WHERE WorkCalendarId = @calId;
SELECT @nHorarios = COUNT(*) FROM Schedules;
SELECT @nAsig     = COUNT(*) FROM ScheduleAssignments;
SELECT @nSaldos   = COUNT(*) FROM VacationAllowances;
SELECT @nSolic    = COUNT(*) FROM AbsenceRequests;

PRINT 'Datos de planificación insertados.';
PRINT CONCAT('  Festivos          : ', @nFestivos);
PRINT CONCAT('  Horarios          : ', @nHorarios);
PRINT CONCAT('  Asignaciones      : ', @nAsig);
PRINT CONCAT('  Saldos vacaciones : ', @nSaldos);
PRINT CONCAT('  Solicitudes       : ', @nSolic);
GO
