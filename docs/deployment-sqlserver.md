# BinsaRRHH — Despliegue en un servidor con SQL Server

Guía paso a paso para desplegar BinsaRRHH en un servidor con **SQL Server**.
Los scripts SQL están en la carpeta [`db/`](../db/).

| Script | Qué hace | Dónde se ejecuta |
|--------|----------|------------------|
| `db/01-create-database.sql` | Crea la BD `HRIA`, el login `hria_app` y sus permisos | Conectado a `master`, como administrador |
| `db/02-schema.sql` | Crea las 7 tablas, índices y claves foráneas (**idempotente**) | Conectado a `HRIA` |
| `db/03-seed-demo.sql` | Datos de demostración (opcional, **idempotente**) | Conectado a `HRIA` |

---

## 0. Requisitos del servidor

- **SQL Server** 2019 o superior (Express vale) y acceso con un usuario administrador.
- **ASP.NET Core 8 Runtime** en el servidor de aplicación.
  - Windows/IIS: instalar el **[.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)**.
  - Linux: instalar `aspnetcore-runtime-8.0`.
- Para compilar: **.NET SDK 8** y **Node.js 22+** (puedes compilar en tu equipo y subir el resultado).
- Herramienta SQL: **SSMS**, **Azure Data Studio** o `sqlcmd`.

---

## 1. Crear la base de datos y el usuario

Abre `db/01-create-database.sql`, **cambia la contraseña** marcada como `<<< CAMBIAR >>>`
y ejecútalo conectado a `master`:

```bash
sqlcmd -S <servidor> -U sa -P '<password-sa>' -i db/01-create-database.sql
```

Crea:
- Base de datos `HRIA` (con `READ_COMMITTED_SNAPSHOT ON`).
- Login/usuario `hria_app` con `db_datareader`, `db_datawriter` y `db_ddladmin`.

> ⚠️ **Si la instancia es compartida con otras aplicaciones**, inventaría antes qué hay y
> confirma que ninguna base existente se llama `HRIA`. El script solo crea objetos nuevos y los
> permisos de `hria_app` se conceden **únicamente dentro de la base `HRIA`**, así que no puede
> leer ni escribir en las demás. Aun así, ejecútalo con un usuario administrador y revisa la
> lista antes y después:
> ```powershell
> sqlcmd -S localhost -Q "SELECT name, create_date FROM sys.databases ORDER BY name"
> ```
> Ojo también con las **instancias múltiples**: un mismo servidor puede tener varias
> (`localhost` y `localhost\OTRA`), cada una con sus bases. Confirma en cuál estás trabajando:
> ```powershell
> Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"
> ```

> Si **no** quieres que la aplicación aplique migraciones sola, comenta la línea de
> `db_ddladmin` en el script y aplica el esquema a mano (paso 2).

---

## 2. Crear las tablas

```bash
sqlcmd -S <servidor> -d HRIA -U hria_app -P '<password-hria_app>' -i db/02-schema.sql
```

El script es **idempotente**: registra la migración en `__EFMigrationsHistory` y no
vuelve a crear objetos existentes. Crea:

| Tabla | Contenido |
|-------|-----------|
| `Departments` | Departamentos (nombre único) |
| `Employees` | Empleados (email único, FK a departamento) |
| `Users` | Usuarios de acceso (email único, 1:1 con empleado, hash de contraseña, rol) |
| `Workdays` | Jornadas (FK a empleado, **índice único filtrado**: una sola jornada abierta por empleado) |
| `Breaks` | Descansos (FK a jornada, borrado en cascada) |
| `AuditLogs` | Auditoría de acciones sensibles |
| `AiQueryLogs` | Auditoría de consultas al asistente de IA |

> **Alternativa:** si dejas `db_ddladmin`, puedes saltarte este paso: la API aplica las
> migraciones automáticamente al arrancar (`Database.Migrate()`).

---

## 3. (Opcional) Datos de demostración

```bash
sqlcmd -S <servidor> -d HRIA -U hria_app -P '<password-hria_app>' -i db/03-seed-demo.sql
```

Inserta 4 departamentos, 10 empleados, los 2 usuarios demo y jornadas de ejemplo
(completas, incompletas, trabajando ahora y en descanso).

| Rol | Email | Contraseña |
|-----|-------|-----------|
| Administrador | `admin@hria.local` | `Demo1234!` |
| Empleado | `empleado@hria.local` | `Demo1234!` |

> Si usas este script, arranca la API con `Demo__Enabled=false` para que **no** vuelva a
> sembrar. Si prefieres que siembre la aplicación, **no ejecutes** este script y usa
> `Demo__Enabled=true`.
>
> ⚠️ En un entorno real, elimina estos usuarios demo o cambia sus contraseñas.

---

## 4. Publicar la API

En tu equipo (o en el servidor con el SDK):

```bash
cd backend
dotnet publish src/HRIA.Api -c Release -o ./publish
```

Copia el contenido de `backend/publish/` al servidor.

### Variables de entorno (obligatorias)

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Database__Provider=SqlServer
# Si la API y SQL Server están en el MISMO servidor, usa "Server=localhost" SIN puerto (ver aviso abajo)
ConnectionStrings__DefaultConnection=Server=<servidor>,1433;Database=HRIA;User Id=hria_app;Password=<password>;TrustServerCertificate=True;Encrypt=True
Jwt__Secret=<secreto largo y aleatorio, >=32 bytes>
Cors__AllowedOrigins__0=https://<url-publica-del-frontend>
Demo__Enabled=false
# Opcional: sin clave, el asistente funciona en modo demo
OpenAI__ApiKey=
```

Genera el secreto JWT con: `openssl rand -base64 48`

> La API **no arranca** en producción si `Jwt__Secret` está vacío (es intencionado).

> ⚠️ **Sobre el `,1433` de la cadena de conexión.** Ese puerto solo funciona si el protocolo
> **TCP/IP está activado** en la instancia, y en **SQL Server Express viene desactivado de
> fábrica**. El síntoma es `database: down` en `/health` con la API arrancada.
>
> - **API y SQL en el mismo servidor** → usa `Server=localhost;…` **sin puerto**: va por memoria
>   compartida y funciona con TCP desactivado. Es la opción recomendada.
> - **API y SQL en máquinas distintas** → hay que activar TCP/IP, lo que **obliga a reiniciar el
>   servicio de SQL Server**. En un servidor compartido, planifícalo como ventana de mantenimiento.
>
> Comprobación sin modificar nada (ajusta `MSSQL16.MSSQLSERVER` a tu versión de instancia):
> ```powershell
> Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp" | Select-Object Enabled
> ```

### ¿Dónde se definen estas variables?

> ⚠️ **La aplicación NO lee ningún archivo `.env`.** El fichero `.env.example` del
> repositorio es solo una **lista de referencia** de qué variables existen. Hay que
> definirlas en el sistema o en la configuración del servicio, según cómo despliegues.

ASP.NET Core lee la configuración en este orden (**lo último gana**):
`appsettings.json` → `appsettings.{Entorno}.json` → *User Secrets* (solo Desarrollo) →
**variables de entorno** → argumentos de línea de comandos.

El doble guion bajo `__` representa la jerarquía: `Jwt__Secret` equivale a la sección
`{ "Jwt": { "Secret": "..." } }`.

#### Opción A — IIS en Windows Server (lo más habitual)

`dotnet publish` genera un `web.config` en la carpeta publicada. Añade dentro de
`<aspNetCore>` un bloque `<environmentVariables>`:

```xml
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\HRIA.Api.dll" hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        <environmentVariable name="Database__Provider" value="SqlServer" />
        <environmentVariable name="ConnectionStrings__DefaultConnection"
                             value="Server=SERVIDOR,1433;Database=HRIA;User Id=hria_app;Password=TU_PASSWORD;TrustServerCertificate=True" />
        <environmentVariable name="Jwt__Secret" value="TU_SECRETO_LARGO" />
        <environmentVariable name="Cors__AllowedOrigins__0" value="https://tu-frontend" />
        <environmentVariable name="Demo__Enabled" value="false" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

Tras editarlo, reinicia el sitio (o `iisreset`).
**Protege el `web.config`**: contiene secretos, no lo subas al repositorio.

#### Opción B — Variables de entorno de Windows (a nivel de máquina)

En PowerShell **como administrador** (persistente, sobrevive a reinicios):
```powershell
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT","Production","Machine")
[Environment]::SetEnvironmentVariable("Database__Provider","SqlServer","Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection","Server=...;Database=HRIA;User Id=hria_app;Password=...;TrustServerCertificate=True","Machine")
[Environment]::SetEnvironmentVariable("Jwt__Secret","TU_SECRETO_LARGO","Machine")
[Environment]::SetEnvironmentVariable("Cors__AllowedOrigins__0","https://tu-frontend","Machine")
[Environment]::SetEnvironmentVariable("Demo__Enabled","false","Machine")
```
Reinicia IIS o el servicio para que las tome (`iisreset`).

También puedes hacerlo por interfaz: *Panel de control → Sistema → Configuración avanzada
→ Variables de entorno → Variables del sistema*.

#### Opción C — Solo para la sesión actual (pruebas rápidas)

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Database__Provider = "SqlServer"
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=HRIA;User Id=hria_app;Password=...;TrustServerCertificate=True"
$env:Jwt__Secret = "TU_SECRETO_LARGO"
dotnet .\HRIA.Api.dll
```
Se pierden al cerrar la terminal. Útil para validar el despliegue antes de fijarlas.

#### Opción D — Linux (systemd)

En el `.service` con líneas `Environment=` (ver §4) o, mejor, en un fichero aparte con
permisos restringidos:

```ini
# /etc/systemd/system/hria-api.service
EnvironmentFile=/etc/hria/hria.env
```
```bash
sudo install -m 600 -o root -g root /dev/null /etc/hria/hria.env
sudo nano /etc/hria/hria.env      # una variable por línea: Clave=Valor (sin comillas)
sudo systemctl daemon-reload && sudo systemctl restart hria-api
```

#### Opción E — Desarrollo local (tu equipo)

No necesitas nada: `appsettings.Development.json` ya usa **SQLite** y el secreto JWT se
genera solo. Si quieres probar contra SQL Server en local, usa *User Secrets* (no se
versionan; ojo: aquí la sintaxis usa **dos puntos**, no `__`):

```bash
cd backend/src/HRIA.Api
dotnet user-secrets init
dotnet user-secrets set "Database:Provider" "SqlServer"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=HRIA;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Secret" "un-secreto-largo-para-desarrollo"
```

#### Resumen rápido

| Escenario | Dónde definirlas |
|-----------|------------------|
| IIS / Windows Server | `web.config` → `<environmentVariables>` (Opción A) |
| Windows, servicio o consola | Variables de máquina con `SetEnvironmentVariable` (Opción B) |
| Linux | `EnvironmentFile` del servicio systemd (Opción D) |
| Prueba puntual | `$env:VAR = "..."` en la terminal (Opción C) |
| Desarrollo local | Nada (SQLite) o *User Secrets* (Opción E) |

### Arrancar

**Linux (systemd)** — `/etc/systemd/system/hria-api.service`:
```ini
[Unit]
Description=HRIA API
After=network.target

[Service]
WorkingDirectory=/opt/hria-api
ExecStart=/usr/bin/dotnet /opt/hria-api/HRIA.Api.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:8080
Environment=Database__Provider=SqlServer
Environment=ConnectionStrings__DefaultConnection=Server=...;Database=HRIA;User Id=hria_app;Password=...;TrustServerCertificate=True
Environment=Jwt__Secret=...
Environment=Cors__AllowedOrigins__0=https://...
Environment=Demo__Enabled=false

[Install]
WantedBy=multi-user.target
```
```bash
sudo systemctl daemon-reload && sudo systemctl enable --now hria-api
```

**Windows / IIS:** 👉 guía dedicada paso a paso en
**[`deployment-iis.md`](./deployment-iis.md)** (Hosting Bundle, URL Rewrite, grupos de
aplicaciones, `web.config`, permisos, HTTPS y solución de problemas).

Resumen: instala el **.NET 8 Hosting Bundle**, crea un sitio apuntando a la carpeta
publicada con un *Application Pool* en modo **"Sin código administrado"** y define las
variables de entorno en el `web.config` (`<environmentVariables>`).

---

## 5. Publicar el frontend

```bash
cd frontend
npm ci
# Apunta a la URL pública de la API
VITE_API_BASE_URL="https://<url-publica-api>/api" npm run build
```

Sube el contenido de `frontend/dist/` a tu servidor web (IIS, Nginx, Apache o un host
estático). **Importante:** configura el *fallback* de SPA — cualquier ruta desconocida
debe servir `index.html` (history mode de Vue Router).

Ejemplo IIS (`web.config` en la carpeta del frontend):
```xml
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

---

## 6. Verificación posterior

- [ ] `GET https://<api>/health` devuelve `{"status":"healthy","database":"up"}`.
- [ ] `https://<api>/swagger` **no** es accesible (solo en desarrollo).
- [ ] Login correcto con `admin@hria.local` / `Demo1234!`.
- [ ] El frontend carga y las llamadas a `/api` funcionan (CORS correcto).
- [ ] Un empleado **no** puede acceder a Empleados/Registros/Auditoría (403).
- [ ] El asistente responde (modo demo si no hay `OpenAI__ApiKey`).

```bash
# Comprobación rápida
curl -s https://<api>/health
curl -s -X POST https://<api>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@hria.local","password":"Demo1234!"}'
```

---

## 7. Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| `502` al llamar a `/api` desde el frontend | La API no está arrancada o la URL es incorrecta | Comprueba el servicio y `VITE_API_BASE_URL` |
| `/health` devuelve `503` con `database: down` | Cadena de conexión, firewall o credenciales | Verifica la cadena, el puerto 1433 y el login |
| La API no arranca en producción | `Jwt__Secret` vacío | Define el secreto por variable de entorno |
| `Login failed for user 'hria_app'` | Autenticación SQL deshabilitada o contraseña incorrecta | Activa el modo mixto en SQL Server y revisa la contraseña |
| Error de certificado TLS al conectar | Certificado no confiable | Añade `TrustServerCertificate=True` o instala un certificado válido |
| Rutas del frontend dan 404 al recargar | Falta el fallback de SPA | Configura la regla de reescritura a `index.html` |
| CORS bloquea las peticiones | Origen no permitido | Ajusta `Cors__AllowedOrigins__0` a la URL exacta del frontend |

---

## 8. Actualizar el esquema en el futuro

Si añades migraciones de EF Core, regenera el script:

```bash
cd backend
dotnet ef migrations script --idempotent \
  -p src/HRIA.Infrastructure -s src/HRIA.Api \
  -o ../db/02-schema.sql
```

El script resultante sigue siendo idempotente: aplica solo lo que falte.
