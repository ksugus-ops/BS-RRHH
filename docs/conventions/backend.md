# BinsaRRHH — Convenciones de backend

> **Cuándo aplicar:** Convenciones del backend de BinsaRRHH (ASP.NET Core 8, arquitectura por capas Domain/Application/Infrastructure/Api/Tests, EF Core, DTOs, validadores, servicios, migraciones). Úsala al crear o modificar código del backend .NET.

Aplica estas reglas al trabajar en el backend .NET del ERP de RRHH BinsaRRHH.

> **Principios de diseño.** Estas convenciones materializan **Clean Architecture** (dependencias
> hacia el dominio), **inversión de dependencias** (la aplicación depende de interfaces, no de
> implementaciones) y **responsabilidad única** (un servicio por caso de uso, controladores finos).
> El razonamiento está en [`../metodologia.md`](../metodologia.md).

## Estructura de la solución
- `HRIA.Domain` — entidades, enums (`Role`, `WorkdayStatus`), lógica de negocio pura. **Sin** dependencias de EF Core/ASP.NET.
- `HRIA.Application` — interfaces (`I...Repository`, `I...Service`, `IAiAssistant`), DTOs, validadores (FluentValidation), servicios de caso de uso, definición de herramientas de IA.
- `HRIA.Infrastructure` — `AppDbContext`, repositorios EF Core, JWT, hashing, cliente OpenAI + modo demo, seeding.
- `HRIA.Api` — controladores finos, middleware, DI, CORS, Swagger, auth, `/health`.
- `HRIA.Tests` — xUnit.

Dependencias hacia dentro: `Api→Application,Infrastructure`; `Infrastructure→Application,Domain`; `Application→Domain`.

## Reglas
- **Fechas siempre en UTC** (`DateTime.UtcNow`, columnas `datetime2`). Nunca `DateTime.Now`.
- **Controladores finos:** validan entrada (DTO + validador), delegan en un servicio de aplicación y devuelven DTOs. Sin lógica de negocio ni EF Core en el controlador.
- **DTOs de entrada/salida** separados de las entidades de dominio. No exponer entidades directamente.
- **Consultas parametrizadas** vía EF Core/LINQ. Nunca SQL concatenado.
- **Autorización:** políticas `AdminOnly` / autenticado. La protección horizontal (empleado solo sus datos) se valida en el servicio comparando con el `employeeId` del token, nunca con un id enviado por el cliente.
- **Hash de contraseñas** con algoritmo fuerte (PBKDF2/ASP.NET Identity `PasswordHasher` o BCrypt). Nunca texto plano ni MD5/SHA simple.
- **Errores:** excepciones de negocio → middleware global → respuesta uniforme (ver `docs/api-design.md` §9). En producción, `500` sin stack trace.
- **Auditoría:** registrar acciones sensibles (login, CRUD empleados, consultas IA) sin PII sensible.
- **Nada de secretos en código/appsettings versionado.** Usar `appsettings.Example.json` / variables de entorno.

## Migraciones EF Core
- Crear con `dotnet ef migrations add <Nombre> -p HRIA.Infrastructure -s HRIA.Api`.
- El seeding de datos demo es idempotente y solo para entorno demo/desarrollo.

## Testing (xUnit)
- Prioriza reglas de negocio del control horario (BR-01..BR-08), autorización y validación.
- Un test por regla, nombres descriptivos (`Metodo_Escenario_ResultadoEsperado`).

## Verificación de cierre
`dotnet build` y `dotnet test` en verde antes de dar una fase por terminada. No afirmar que algo funciona sin ejecutarlo.
