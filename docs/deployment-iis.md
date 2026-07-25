# BinsaRRHH — Despliegue en IIS (Windows Server)

Guía completa para publicar BinsaRRHH en **IIS** con **SQL Server**.
Los pasos de base de datos están en [`deployment-sqlserver.md`](./deployment-sqlserver.md).

Desplegaremos **dos sitios** en IIS:

| Sitio | Contenido | Puerto sugerido |
|-------|-----------|-----------------|
| `HRIA-Api` | API ASP.NET Core (carpeta publicada) | 8080 (o `api.tudominio.com`) |
| `HRIA-Web` | SPA de Vue (archivos estáticos) | 80/443 (o `hria.tudominio.com`) |

---

## 1. Requisitos previos en el servidor

### 1.1 Rol de IIS
Instala IIS con el *Administrador del servidor* → **Agregar roles** → *Servidor web (IIS)*.

### 1.2 .NET 8 Hosting Bundle ⚠️ imprescindible
Descarga e instala el **ASP.NET Core 8 Hosting Bundle** (no el SDK ni solo el runtime):
<https://dotnet.microsoft.com/download/dotnet/8.0> → *Hosting Bundle*

Instala el **ASP.NET Core Module V2**, necesario para que IIS ejecute la API.
Después, reinicia IIS:
```powershell
net stop was /y
net start w3svc
```
Verifica que se instaló:
```powershell
dotnet --list-runtimes    # debe aparecer Microsoft.AspNetCore.App 8.0.x
```

> ⚠️ **`dotnet --list-runtimes` NO basta como comprobación.** Si alguien instaló el SDK o el
> runtime por su cuenta, verás `Microsoft.AspNetCore.App 8.0.x` aunque **falte el módulo de
> IIS**, y la API dará 500.19 / 502.5. La comprobación fiable es que exista el módulo:
> ```powershell
> Test-Path "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
> Select-String "$env:windir\System32\inetsrv\config\applicationHost.config" -Pattern 'name="AspNetCoreModuleV2"'
> ```

### 1.3 Módulo URL Rewrite ⚠️ necesario para la SPA
Descárgalo e instálalo: <https://www.iis.net/downloads/microsoft/url-rewrite>
Sin él, al recargar una ruta como `/empleados` el frontend daría **404**.

### 1.4 WebDAV ⚠️ rompe `PUT` y `DELETE` si está instalado

Si el servidor tiene instalada la característica **Publicación WebDAV**, su módulo captura los
verbos `PUT`, `DELETE`, `PROPFIND`, `MKCOL`, `COPY`, `MOVE`, `LOCK` y `UNLOCK` **antes** de que
lleguen a la aplicación, y responde **405 Method Not Allowed**.

El síntoma engaña bastante: `GET` y `POST` funcionan con normalidad, así que parece un problema
de rutas o de permisos de la API, cuando en realidad la petición nunca llega a ASP.NET Core.

Comprueba si lo tienes:

```powershell
Get-WindowsFeature -Name Web-DAV-Publishing | Select-Object Name, InstallState
```

**No lo desinstales**: otros sitios del servidor podrían usarlo. Desactívalo **solo para el sitio
de la API**, añadiendo esto dentro de `<system.webServer>` en su `web.config` (§4):

```xml
<modules>
  <remove name="WebDAVModule" />
</modules>
<handlers>
  <remove name="WebDAV" />
  <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
</handlers>
```

Verifícalo con una petición `DELETE` a un recurso inexistente: debe responder **404** (llega a la
aplicación), no **405**.

---

## 2. Publicar la API

En tu equipo de desarrollo:
```powershell
cd C:\Code\BS-RRHH\backend
dotnet publish src/HRIA.Api -c Release -o .\publish
```

Copia todo el contenido de `backend\publish\` al servidor, por ejemplo a:
```
C:\inetpub\hria-api\
```

### ⚠️ En las RE-publicaciones, excluye `web.config`

`dotnet publish` **genera su propio `web.config`** limpio. Si lo copias encima del que ya está
en el servidor, **te llevas por delante todas las variables de entorno** que guarda ese fichero:
la cadena de conexión, el secreto JWT, los orígenes de CORS y las claves del asistente. El sitio
arranca, pero `/health` devuelve `database: down` y el login deja de funcionar.

Para actualizar la API sin perder la configuración:

```powershell
Get-ChildItem C:\HRIA-Setup\publish-api -Exclude web.config |
  Copy-Item -Destination C:\inetpub\hria-api -Recurse -Force
```

Y ten siempre una copia a mano antes de tocar nada:

```powershell
Copy-Item C:\inetpub\hria-api\web.config C:\HRIA-Setup\web.config.api.backup -Force
```

---

## 3. Crear el sitio de la API en IIS

### 3.1 Grupo de aplicaciones
*Administrador de IIS* → **Grupos de aplicaciones** → **Agregar grupo de aplicaciones**:

| Campo | Valor |
|-------|-------|
| Nombre | `HRIA-Api` |
| Versión de .NET CLR | **Sin código administrado** ⚠️ |
| Modo de canalización | Integrada |

> "Sin código administrado" es correcto: .NET moderno se ejecuta **fuera** del CLR de IIS.

### 3.2 Sitio web
*Sitios* → **Agregar sitio web**:

| Campo | Valor |
|-------|-------|
| Nombre | `HRIA-Api` |
| Grupo de aplicaciones | `HRIA-Api` |
| Ruta de acceso física | `C:\inetpub\hria-api` |
| Puerto | `8080` (o enlace con nombre de host) |

### 3.3 Permisos de carpeta
El grupo de aplicaciones se ejecuta como `IIS APPPOOL\HRIA-Api`. Dale lectura:
```powershell
icacls "C:\inetpub\hria-api" /grant "IIS APPPOOL\HRIA-Api:(OI)(CI)RX" /T
```
Si activas los logs de stdout (§7), crea la carpeta y da permiso de escritura:
```powershell
New-Item -ItemType Directory "C:\inetpub\hria-api\logs" -Force
icacls "C:\inetpub\hria-api\logs" /grant "IIS APPPOOL\HRIA-Api:(OI)(CI)M" /T
```

---

## 4. Configurar las variables de entorno (`web.config`)

**La cadena de conexión va aquí, en el `web.config` de la carpeta publicada.**

⚠️ Ojo: el `web.config` que genera `dotnet publish` es **mínimo y no contiene ninguna
variable**. Tiene este aspecto, con `<aspNetCore ... />` **auto-cerrado**:

```xml
<!-- Generado por dotnet publish: NO tiene variables -->
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\HRIA.Api.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

Debes **abrir esa etiqueta** y añadir dentro el bloque `<environmentVariables>`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>

      <!-- ↓ etiqueta abierta (antes terminaba en "/>") -->
      <aspNetCore processPath="dotnet"
                  arguments=".\HRIA.Api.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="Database__Provider" value="SqlServer" />
          <!-- ↓ AQUÍ va la cadena de conexión -->
          <environmentVariable name="ConnectionStrings__DefaultConnection"
                               value="Server=NOMBRE_SERVIDOR,1433;Database=HRIA;User Id=hria_app;Password=TU_PASSWORD;TrustServerCertificate=True" />
          <environmentVariable name="Jwt__Secret" value="TU_SECRETO_LARGO_ALEATORIO" />
          <environmentVariable name="Jwt__ExpiresMinutes" value="60" />
          <environmentVariable name="Cors__AllowedOrigins__0" value="https://hria.tudominio.com" />
          <environmentVariable name="Demo__Enabled" value="false" />
          <!-- Asistente. Este despliegue usa Groq (capa gratuita) a través del cliente
               compatible con OpenAI. Clave vacía = modo demo.
               ⚠️ La capa gratuita solo vale con DATOS FICTICIOS: ver ADR-006. -->
          <environmentVariable name="Ai__Provider" value="OpenAI" />
          <environmentVariable name="OpenAI__BaseUrl" value="https://api.groq.com/openai/v1" />
          <environmentVariable name="OpenAI__Model" value="llama-3.3-70b-versatile" />
          <environmentVariable name="OpenAI__ApiKey" value="" />
        </environmentVariables>
      </aspNetCore>

    </system.webServer>
  </location>
</configuration>
```

> 📄 Tienes una plantilla lista para copiar en
> [`examples/api-web.config.example`](./examples/api-web.config.example).

### ⚠️ El puerto en la cadena de conexión: `,1433` no siempre vale

`Server=SERVIDOR,1433` **exige que el protocolo TCP/IP esté activado** en la instancia de
SQL Server. En muchas instalaciones (sobre todo **SQL Server Express**) viene **desactivado
de fábrica**, y entonces la API arranca pero `/health` devuelve `database: down`.

Comprueba el estado **sin modificar nada**:

```powershell
# Enabled = 1 -> TCP activo | Enabled = 0 -> TCP desactivado
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQLServer\SuperSocketNetLib\Tcp" |
  Select-Object Enabled
```

Según el resultado:

| Situación | Cadena de conexión correcta |
|-----------|-----------------------------|
| **La API y SQL Server están en el mismo servidor** | `Server=localhost;Database=HRIA;…` — **sin puerto**. Usa memoria compartida; funciona aunque TCP esté desactivado. ✅ *Opción recomendada.* |
| TCP activado y SQL en otra máquina | `Server=SERVIDOR,1433;Database=HRIA;…` |
| Instancia con nombre | `Server=SERVIDOR\INSTANCIA;Database=HRIA;…` (requiere el servicio *SQL Browser*) |

> ⚠️ **No actives TCP/IP a la ligera en un servidor compartido:** el cambio **exige reiniciar
> el servicio de SQL Server**, lo que tumba todas las aplicaciones que usen esa instancia. Si
> la API vive en el mismo servidor, `Server=localhost` sin puerto evita el problema por
> completo.

**Cuidado con los caracteres especiales del XML** en la contraseña: si contiene
`&`, `<`, `>` o `"`, escápalos como `&amp;`, `&lt;`, `&gt;`, `&quot;`.

### ¿Y si prefiero no tocar el `web.config`?

Alternativa: define las mismas variables como **variables de entorno de máquina**
(ver `deployment-sqlserver.md`, Opción B). Ventaja: `dotnet publish` no las sobrescribe.
Inconveniente: son visibles para todo el servidor.

Genera el secreto JWT (en tu equipo):
```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))
```

> ⚠️ Este `web.config` contiene secretos: **no lo subas al repositorio** y restringe su
> lectura al grupo de aplicaciones y a los administradores.

Tras editarlo, reinicia el sitio en IIS (el módulo detecta el cambio y recicla el proceso).

### Alternativa más segura: autenticación integrada de Windows

Evitas guardar la contraseña en el `web.config`. En la cadena de conexión usa:
```
Server=NOMBRE_SERVIDOR;Database=HRIA;Trusted_Connection=True;TrustServerCertificate=True
```
Y en SQL Server crea un login para la identidad del grupo de aplicaciones:
```sql
CREATE LOGIN [IIS APPPOOL\HRIA-Api] FROM WINDOWS;
USE [HRIA];
CREATE USER [IIS APPPOOL\HRIA-Api] FOR LOGIN [IIS APPPOOL\HRIA-Api];
ALTER ROLE [db_datareader] ADD MEMBER [IIS APPPOOL\HRIA-Api];
ALTER ROLE [db_datawriter] ADD MEMBER [IIS APPPOOL\HRIA-Api];
ALTER ROLE [db_ddladmin]  ADD MEMBER [IIS APPPOOL\HRIA-Api];  -- si la app aplica migraciones
```
> Si SQL Server está en **otro servidor**, la identidad del grupo de aplicaciones no llega
> por red: usa una cuenta de dominio como identidad del pool, o autenticación SQL.

### Comprobar la API
```powershell
curl http://localhost:8080/health
```
Debe responder `{"status":"healthy", ... "database":"up"}`.

---

## 5. Publicar el frontend

### 5.1 Compilar apuntando a la API
En tu equipo:
```powershell
cd C:\Code\BS-RRHH\frontend
$env:VITE_API_BASE_URL = "https://api.tudominio.com/api"
npm ci
npm run build
```
> Si `npm` da error de *execution policy*, usa `cmd /c "npm run build"`.

Copia el contenido de `frontend\dist\` al servidor, por ejemplo a `C:\inetpub\hria-web\`.

### 5.2 Crear el sitio en IIS
*Sitios* → **Agregar sitio web**:

| Campo | Valor |
|-------|-------|
| Nombre | `HRIA-Web` |
| Grupo de aplicaciones | `HRIA-Web` (Sin código administrado) |
| Ruta física | `C:\inetpub\hria-web` |
| Puerto / host | 80/443 o `hria.tudominio.com` |

### 5.3 `web.config` para el enrutado de la SPA
Crea `C:\inetpub\hria-web\web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
      <remove fileExtension=".woff2" />
      <mimeMap fileExtension=".woff2" mimeType="font/woff2" />
    </staticContent>
  </system.webServer>
</configuration>
```

> Requiere el módulo **URL Rewrite** (§1.3).

---

## 6. HTTPS y CORS

### 6.1 Elegir certificado

| Escenario | Qué usar |
|-----------|----------|
| Tienes un dominio público (`hria.tudominio.com`) | Certificado de una CA pública (Let's Encrypt, comodín corporativo…). Es lo ideal. |
| Solo hay IP o nombre interno, sin DNS público | **Certificado autofirmado**. Funciona, pero cada equipo cliente debe confiar en él (§6.3). |

> Si en el servidor ya hay un **comodín corporativo** (`*.tudominio.com`), puedes reutilizarlo,
> pero **solo sirve si el DNS resuelve** el nombre que vas a usar. Compruébalo antes:
> `Resolve-DnsName hria.tudominio.com`. Si no resuelve, necesitas crear el registro DNS primero.

### 6.2 Crear el certificado autofirmado y enlazarlo

```powershell
Import-Module WebAdministration

# 1) Certificado con todos los nombres por los que se accederá
$cert = New-SelfSignedCertificate `
  -DnsName "192.168.1.10","MISERVIDOR","localhost" `
  -FriendlyName "BinsaRRHH" `
  -CertStoreLocation "Cert:\LocalMachine\My" `
  -NotAfter (Get-Date).AddYears(3) -KeyExportPolicy Exportable

# 2) Que el propio servidor confíe en él
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
$store.Open("ReadWrite"); $store.Add($cert); $store.Close()

# 3) Exportarlo para instalarlo en los equipos cliente
Export-Certificate -Cert $cert -FilePath "C:\HRIA-Setup\hria-cert.cer" -Type CERT

# 4) Enlaces HTTPS en ambos sitios
New-WebBinding -Name "HRIA-Api" -Protocol https -Port 8443 -IPAddress "*"
New-WebBinding -Name "HRIA-Web" -Protocol https -Port 8490 -IPAddress "*"

# 5) Asignar el certificado a cada puerto
Get-Item "Cert:\LocalMachine\My\$($cert.Thumbprint)" | New-Item "IIS:\SslBindings\0.0.0.0!8443"
Get-Item "Cert:\LocalMachine\My\$($cert.Thumbprint)" | New-Item "IIS:\SslBindings\0.0.0.0!8490"
```

### 6.3 Confiar en el certificado desde los clientes

Con un certificado autofirmado, **no basta con aceptar el aviso del navegador en el frontend**:
las llamadas `fetch`/XHR a la API se bloquean en silencio si su certificado no es de confianza,
y verás errores de CORS engañosos en la consola.

Dos formas de resolverlo:

- **Recomendada:** copia `hria-cert.cer` al equipo cliente e instálalo en
  *Entidades de certificación raíz de confianza* (doble clic → *Instalar certificado* →
  *Equipo local* → *Colocar todos los certificados en el siguiente almacén*).
- **Rápida (solo para una demo):** abre `https://<api>:8443/health` en el navegador y acepta
  la excepción **antes** de entrar al frontend. Hay que repetirlo por cada origen y navegador.

### 6.4 CORS y recompilación del frontend

> ⚠️ Los puertos de los ejemplos son orientativos. **Confirma cuál corresponde a cada sitio antes
> de configurar nada**, sobre todo si es el departamento de red quien asigna los puertos
> publicados: es fácil acabar con el frontend y la API intercambiados respecto a lo que dice tu
> documentación, y el síntoma (la web carga, nada funciona) despista.

Al pasar a HTTPS hay que tocar **dos sitios**, y olvidar uno es el fallo más habitual:

1. **`web.config` de la API** — `Cors__AllowedOrigins__0` debe ser **exactamente** el origen del
   frontend (esquema + host + puerto), **sin barra final**:
   - `https://hria.tudominio.com` ✅
   - `https://hria.tudominio.com/` ❌ (barra final)
   - `http://hria.tudominio.com` ❌ si el sitio va por HTTPS

   Si mantienes también el acceso por HTTP, añade índices correlativos **sin saltarte ninguno**
   (`__0`, `__1`, `__2`…); ASP.NET Core deja de leer en el primer hueco.

2. **Recompilar el frontend** con la URL HTTPS de la API. La URL se **incrusta en el bundle** en
   tiempo de compilación: cambiar el `web.config` no basta.

   ```powershell
   cd C:\Code\BS-RRHH\frontend
   $env:VITE_API_BASE_URL = "https://<api>:8443/api"
   cmd /c "npm run build"
   Copy-Item .\dist\* C:\inetpub\hria-web\ -Recurse -Force
   ```

   Verifica que la URL quedó dentro del bundle:
   ```powershell
   Select-String -Path C:\inetpub\hria-web\assets\index-*.js -Pattern "https://<api>:8443/api" -Quiet
   ```

> ⚠️ **Contenido mixto:** si el frontend va por HTTPS y la API por HTTP, el navegador bloquea
> las llamadas. Ambos deben ir por HTTPS.

---

## 7. Verificación

```powershell
# API
curl https://api.tudominio.com/health

# Login
curl -Method POST https://api.tudominio.com/api/auth/login `
     -ContentType "application/json" `
     -Body '{"email":"admin@hria.local","password":"Demo1234!"}'
```

Checklist:
- [ ] `/health` responde `healthy` y `database: up`.
- [ ] `/swagger` **no** es accesible (solo en desarrollo).
- [ ] El frontend carga y el login funciona.
- [ ] Al recargar una ruta interna (p. ej. `/empleados`) **no** da 404.
- [ ] Un empleado no puede entrar en Empleados/Registros/Auditoría (403).

---

## 8. Problemas frecuentes en IIS

| Error | Causa | Solución |
|-------|-------|----------|
| **HTTP 500.19** | `web.config` mal formado o falta un módulo | Valida el XML; comprueba que el Hosting Bundle y URL Rewrite están instalados |
| **HTTP 500.30** (*In-Process Start Failure*) | La app no arranca: falta `Jwt__Secret`, cadena de conexión mala, BD inaccesible | Activa `stdoutLogEnabled="true"` y lee `logs\stdout_*.log`; revisa el Visor de eventos → Aplicación |
| **HTTP 502.5** | Fallo del proceso / runtime incorrecto | Verifica el Hosting Bundle y `processPath="dotnet"` |
| **HTTP 503** | El grupo de aplicaciones está detenido | Inícialo; revisa el Visor de eventos por bloqueos de identidad |
| **HTTP 405 solo en `PUT` y `DELETE`** (los `GET` y `POST` van bien) | El módulo **WebDAV** captura esos verbos antes que la aplicación | Quita `WebDAVModule` y el handler `WebDAV` en el `web.config` del sitio (§1.4). No desinstales la característica: otros sitios pueden usarla |
| **Los ficheros están bloqueados al desplegar** | Parar el sitio **no** detiene el proceso del grupo de aplicaciones | `Stop-WebAppPool` además de `Stop-WebSite`, y espera a que el estado sea `Stopped` antes de copiar |
| **404 al recargar rutas del frontend** | Falta URL Rewrite o la regla | Instala el módulo y añade el `web.config` de §5.3 |
| **CORS bloquea las llamadas** | Origen mal configurado | Ajusta `Cors__AllowedOrigins__0` al origen exacto |
| **`Login failed for user`** | Autenticación SQL desactivada, contraseña incorrecta o falta el login del pool | Activa el **modo mixto** en SQL Server y revisa credenciales/permisos |
| **`database: down` en /health** | **TCP/IP desactivado en la instancia** (lo más frecuente en SQL Express), firewall, instancia o cadena incorrecta | Si la API y SQL están en el mismo servidor, usa `Server=localhost` **sin `,1433`** (§4). Si están separados, activa TCP y abre el 1433 |
| **Llamadas del frontend bloqueadas con error de CORS por HTTPS** | Certificado autofirmado no confiable: el navegador corta la petición y lo reporta como CORS | Instala el `.cer` en *Entidades de certificación raíz de confianza* del cliente (§6.3) |
| **El frontend sigue llamando a la URL antigua tras cambiar el `web.config`** | `VITE_API_BASE_URL` se incrusta en el bundle al compilar | Recompila el frontend y vuelve a copiar `dist\` (§6.4) |
| **Cambios que no se aplican** | Proceso cacheado | Reinicia el sitio o recicla el grupo de aplicaciones |

### Activar logs para diagnosticar
En `web.config` pon `stdoutLogEnabled="true"` (y crea la carpeta `logs` con permisos, §3.3).
Reproduce el error, mira `C:\inetpub\hria-api\logs\stdout_*.log` y **vuelve a ponerlo en
`false`** (los logs crecen sin límite).

---

## 9. Actualizaciones posteriores

```powershell
# 1) En tu equipo
dotnet publish src/HRIA.Api -c Release -o .\publish

# 2) En el servidor: detén el sitio, copia los archivos, arráncalo
Stop-WebSite  -Name "HRIA-Api"
# (copiar el contenido de publish\ manteniendo tu web.config editado)
Start-WebSite -Name "HRIA-Api"
```

> ⚠️ `dotnet publish` **sobrescribe** el `web.config`. Guarda una copia del tuyo (con las
> variables de entorno) y restáurala tras cada despliegue, o mantén las variables como
> **variables de máquina** en su lugar (ver `deployment-sqlserver.md`, Opción B).
