# BinsaRRHH — Despliegue

Guía de despliegue nativo. El proyecto se ejecuta como dos piezas independientes:
una API ASP.NET Core y una SPA de Vue compilada a estáticos.

## 1. Arquitectura de despliegue

```
[ Navegador ] ── HTTPS ──> [ SPA estática (Netlify/Vercel/host estático) ]
                                   │  llamadas /api
                                   ▼
                           [ API ASP.NET Core (Render/Railway/Azure) ]
                                   │
                                   ▼
                           [ Base de datos: SQL Server o SQLite ]
```

## 2. Variables de entorno

La configuración sensible se inyecta por **variables de entorno** (nunca en el repositorio).
Notación de ASP.NET: el doble guion bajo `__` representa la jerarquía de secciones.

| Variable | Obligatoria | Descripción | Ejemplo |
|----------|:-----------:|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Sí | Entorno de ejecución | `Production` |
| `ASPNETCORE_URLS` | Sí | URL/puerto de escucha | `http://+:8080` |
| `Database__Provider` | Sí | `SqlServer` o `Sqlite` | `SqlServer` |
| `ConnectionStrings__DefaultConnection` | Sí | Cadena de conexión | `Server=...;Database=HRIA;User Id=...;Password=...;TrustServerCertificate=True` |
| `Jwt__Secret` | Sí (prod) | Secreto de firma JWT (>=32 bytes) | `openssl rand -base64 48` |
| `Jwt__Issuer` | No | Emisor | `HRIA` |
| `Jwt__Audience` | No | Audiencia | `HRIA.Client` |
| `Jwt__ExpiresMinutes` | No | Expiración del token | `60` |
| `Cors__AllowedOrigins__0` | Sí | Origen permitido (URL del frontend) | `https://hria.example.com` |
| `Ai__Provider` | No | Proveedor del asistente: `Claude`, `OpenAI` o `Demo` (por defecto **`Demo`**: un clon del repositorio arranca sin clave; el despliegue lo sobrescribe con `OpenAI` apuntado a Groq) | `OpenAI` |
| `Claude__ApiKey` | No | Clave de Anthropic (si falta → modo demo) | `sk-ant-...` |
| `Claude__Model` | No | Modelo | `claude-sonnet-5` |
| `OpenAI__ApiKey` | No | Clave, solo si `Ai__Provider=OpenAI` | `gsk_...` (Groq) |
| `OpenAI__BaseUrl` | No | Endpoint compatible con OpenAI | `https://api.groq.com/openai/v1` |
| `OpenAI__Model` | No | Modelo | `llama-3.3-70b-versatile` |
| `Demo__Enabled` | No | Sembrar datos demo al arrancar | `true` |

> Si `Jwt__Secret` está vacío en producción, la API **falla al arrancar** (a propósito).
> Si la clave del proveedor seleccionado está vacía, el asistente funciona en **modo demo**.
> Solo se registra el proveedor indicado en `Ai__Provider`: con dos proveedores vivos a la
> vez, cuál gana dependería del orden de registro en el contenedor.

`OpenAI__BaseUrl` convierte al cliente de OpenAI en un cliente **genérico**: cualquier servicio
que exponga `/chat/completions` (Groq, OpenRouter, Ollama, Azure OpenAI) funciona **sin tocar
código**. El despliegue de evaluación lo aprovecha para usar Groq.

> ⚠️ **La capa gratuita solo es admisible con datos ficticios.** Al proveedor viajan nombres de
> empleados, departamentos y horas: con plantilla real hay que pasar a una **suscripción de pago**
> con acuerdo de encargado del tratamiento, o a **inferencia local**. Es un requisito previo a
> producción, no una mejora futura — ver [ADR-006](./adr/ADR-006-proveedor-de-ia.md).

## 3. Backend (API ASP.NET Core)

### Publicar localmente
```bash
cd backend
dotnet publish src/HRIA.Api -c Release -o ./publish
```
El resultado en `./publish` es autocontenido (framework-dependent). Para ejecutarlo:
```bash
cd publish
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS=http://+:8080 \
Database__Provider=Sqlite \
ConnectionStrings__DefaultConnection="Data Source=/data/hria.db" \
Jwt__Secret="<secreto-largo>" \
Cors__AllowedOrigins__0="https://<tu-frontend>" \
dotnet HRIA.Api.dll
```

### Base de datos
- **SQL Server (producción):** define `Database__Provider=SqlServer` y la cadena de conexión.
  Las **migraciones** se aplican automáticamente al arrancar (`Database.Migrate`).
  👉 Guía detallada y **scripts SQL listos para ejecutar**:
  [`deployment-sqlserver.md`](./deployment-sqlserver.md) y carpeta [`db/`](../db/).
- **SQLite (demo/pequeño):** define `Database__Provider=Sqlite` y `Data Source=...`.
  El esquema se crea automáticamente (`EnsureCreated`).

### Plataformas recomendadas
- **Render** / **Railway**: entorno .NET nativo. Build: `dotnet publish src/HRIA.Api -c Release -o out`; Start: `dotnet out/HRIA.Api.dll`. Añade las variables de entorno del §2.
- **Azure App Service** (.NET 8).

## 4. Frontend (SPA Vue)

### Compilar
```bash
cd frontend
npm ci
VITE_API_BASE_URL="https://<tu-api>/api" npm run build
```
El resultado estático queda en `frontend/dist/`.

### Servir
- **Netlify / Vercel / Cloudflare Pages / GitHub Pages**: publica el contenido de `dist/`.
- Configura el *fallback* de SPA (todas las rutas → `index.html`) para que funcione el
  history mode de Vue Router.
- `VITE_API_BASE_URL` debe apuntar a la URL pública de la API.

## 5. Health check

`GET /health` devuelve el estado del servicio y de la base de datos:
```json
{ "status": "healthy", "service": "HRIA.Api", "database": "up", "timeUtc": "..." }
```
Devuelve `503` si la base de datos no responde. Úsalo como *health check* en la plataforma.

## 6. CI/CD (GitHub Actions)

`.github/workflows/ci.yml` ejecuta en cada *push* y *pull request*:
- **Backend:** restaurar, compilar y `dotnet test` (.NET 8).
- **Frontend:** `npm ci`, `npm run build` y `npm run test` (Node 22).

El despliegue puede añadirse como paso posterior conectando el repositorio a la plataforma
elegida (Render/Railway/Netlify), que reconstruye automáticamente en cada push a `main`.

## 7. Comprobaciones tras el despliegue

- [ ] `GET /health` responde `healthy`.
- [ ] `GET /swagger` **no** está accesible en producción (solo desarrollo).
- [ ] Login con el usuario demo funciona (`admin@hria.local` / `Demo1234!`).
- [ ] CORS permite el origen del frontend y rechaza otros.
- [ ] El asistente responde (`mode: demo` o `mode: live` según la clave del proveedor).
- [ ] No hay secretos en el repositorio ni en los logs.
