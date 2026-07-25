/* ==========================================================================
   HRIA — 01. Creación de la base de datos y del usuario de la aplicación
   --------------------------------------------------------------------------
   Ejecutar CONECTADO A LA BASE DE DATOS 'master' con un usuario administrador.
   Cambia los valores marcados con <<< CAMBIAR >>> antes de ejecutar.
   ========================================================================== */

/* --- 1. Crear la base de datos (si no existe) --- */
IF DB_ID(N'HRIA') IS NULL
BEGIN
    CREATE DATABASE [HRIA];
    PRINT 'Base de datos HRIA creada.';
END
ELSE
    PRINT 'La base de datos HRIA ya existe; no se recrea.';
GO

/* --- 2. Configuración recomendada --- */
ALTER DATABASE [HRIA] SET RECOVERY SIMPLE;      -- simplifica el mantenimiento del log
ALTER DATABASE [HRIA] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
GO

/* --- 3. Login de la aplicación (autenticación SQL) ---
   Usa una contraseña fuerte y guárdala como secreto (variable de entorno),
   NUNCA en el repositorio. */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'hria_app')
BEGIN
    CREATE LOGIN [hria_app]
        WITH PASSWORD = N'<<< CAMBIAR: contraseña fuerte >>>',
             CHECK_POLICY = ON;
    PRINT 'Login hria_app creado.';
END
GO

/* --- 4. Usuario de base de datos y permisos mínimos --- */
USE [HRIA];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'hria_app')
BEGIN
    CREATE USER [hria_app] FOR LOGIN [hria_app];
    PRINT 'Usuario hria_app creado en HRIA.';
END
GO

/* Permisos: lectura/escritura de datos.
   Se añade ddl_admin SOLO si quieres que la aplicación aplique migraciones
   automáticamente al arrancar. Si prefieres aplicar el esquema a mano
   (script 02), comenta la línea de db_ddladmin. */
ALTER ROLE [db_datareader] ADD MEMBER [hria_app];
ALTER ROLE [db_datawriter] ADD MEMBER [hria_app];
ALTER ROLE [db_ddladmin]  ADD MEMBER [hria_app];   -- necesario para migraciones automáticas
GO

PRINT 'Paso 1 completado. Ejecuta ahora 02-schema.sql sobre la base de datos HRIA.';
GO
