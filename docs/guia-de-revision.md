# BinsaRRHH — Guía para revisar el proyecto

Este documento está escrito para **quien evalúa**. La aplicación no se presenta en directo: se
revisa leyendo el repositorio, así que aquí queda dicho dónde mirar cada cosa y cómo comprobar
que funciona sin tener que preguntar nada.

Orden sugerido: **probar la aplicación desplegada** (10 min) → **recorrer el código** (30 min) →
**contrastar la documentación** (20 min).

---

## 1. Probarla sin instalar nada

La aplicación está **desplegada y accesible**. Su URL **no se publica en este repositorio** porque
el despliegue está sobre la infraestructura real de la empresa (Binsa); se facilita en el
**formulario de entrega**. Con estas credenciales:

| Rol | Usuario | Contraseña |
|-----|---------|-----------|
| Empleado | `empleado@hria.local` | `Demo1234!` |
| Administrador | `admin@hria.local` | facilitada en el formulario de entrega |

Con la cuenta de empleado se recorre la aplicación y se comprueba el control de acceso: las
secciones de Empleados, Registros y Auditoría devuelven `403`, no solo se ocultan.

> Alternativa sin depender del despliegue: arranca en local sin instalar SQL Server —ver
> [README § Instalación y ejecución](../README.md#c-instalación-y-ejecución)—. Con `Database__Provider=Sqlite` no hay
> dependencias externas.

---

## 2. Qué mirar en el código, y por qué

Cinco puntos donde se concentran las decisiones que merece la pena juzgar.

### 2.1 La IA no puede inventarse un dato

📄 [`AiToolRegistry.cs`](../backend/src/HRIA.Application/Ai/AiToolRegistry.cs)

El modelo **nunca ve la base de datos**. Recibe la lista de herramientas que su rol permite,
pide una, y el backend la ejecuta. No hay SQL generado por el modelo.

```csharp
public IReadOnlyList<AiTool> BuildTools(Role role, int currentEmployeeId)
{
    var tools = new List<AiTool>();
    if (role == Role.Admin) { /* herramientas globales */ }
    tools.Add(EmployeeHoursSummary(role, currentEmployeeId));  // el empleado queda forzado a su id
    return tools;
}
```

La autorización se decide **antes** de hablar con el modelo. Un empleado no obtiene datos de
otro porque esa herramienta no se le ofrece, no porque el modelo se porte bien.

Ver también [`ClaudeAssistant.cs`](../backend/src/HRIA.Infrastructure/Ai/ClaudeAssistant.cs)
para el bucle de uso de herramientas, y [`AiPrompt.cs`](../backend/src/HRIA.Application/Ai/AiPrompt.cs),
que inyecta la fecha actual —comentario incluido explicando por qué hizo falta—.

### 2.2 Protección horizontal

📄 [`TimeTrackingService.cs`](../backend/src/HRIA.Application/TimeTracking/TimeTrackingService.cs)

El `employeeId` de las operaciones propias **se deriva del token**, nunca de lo que envía el
cliente. Un empleado no puede leer ni escribir datos de otro aunque manipule la petición.

Comprobable en [`TimeTrackingServiceTests`](../backend/tests/HRIA.Tests/TimeTracking/) y en las
pruebas de endpoints, que llaman con el rol equivocado y esperan `403`.

### 2.3 Reglas de negocio en el dominio

📄 [`Workday.cs`](../backend/src/HRIA.Domain/Entities/Workday.cs)

Las ocho reglas del control horario (BR-01..BR-08) viven en el dominio, no en el controlador ni
en el botón de la interfaz. La regla de «no puede haber dos jornadas abiertas» está además
**en la base de datos**, con un índice único filtrado: no depende de que el código se acuerde.

### 2.4 Contraseñas

📄 [`Pbkdf2PasswordHasher.cs`](../backend/src/HRIA.Infrastructure/Security/Pbkdf2PasswordHasher.cs)
· [`AuthService.cs`](../backend/src/HRIA.Application/Auth/AuthService.cs)

PBKDF2-HMAC-SHA256, 100.000 iteraciones, sal por contraseña. **Irreversible**: no hay pantalla
ni endpoint que las muestre, tampoco al administrador.

El razonamiento de por qué eso no es una carencia sino un requisito está en
[`security.md` §3-bis](./security.md).

### 2.5 Zona horaria

📄 [`DashboardService.cs`](../backend/src/HRIA.Application/Dashboard/DashboardService.cs)

Todo se guarda en UTC, **salvo los tramos de horario**, que son hora local del centro de
trabajo: un horario de 08:00 no debe moverse con el cambio de estación. La excepción está
razonada en [`architecture.md` §8](./architecture.md).

En ese mismo fichero hay un comentario sobre por qué la comparación pasa por `TimeSpan`: restar
dos `TimeOnly` nunca da negativo, y entrar antes de hora se contabilizaba como un retraso de
casi 24 horas.

---

## 3. Comprobar que las pruebas pasan

```bash
cd backend && dotnet test
```

```bash
cd frontend && npm run test
```

**173 pruebas de backend y 9 de frontend.** El criterio es cobertura honesta: reglas de negocio
y autorización antes que porcentaje total. El mapa de qué cubre cada fichero está en
[`testing.md`](./testing.md).

---

## 4. Documentación, por si se busca algo concreto

| Pregunta | Documento |
|---|---|
| ¿Qué se pedía y qué se entregó? | [`requirements.md`](./requirements.md) |
| ¿Cómo se decidió la estructura? | [`architecture.md`](./architecture.md) y [`adr/`](./adr/) |
| ¿Cómo se protegió? | [`security.md`](./security.md) — tabla de riesgos con evidencias |
| ¿Qué se probó y qué no? | [`testing.md`](./testing.md) |
| ¿Cómo se despliega? | [`deployment-iis.md`](./deployment-iis.md) |
| ¿Qué endpoints hay? | [`api-design.md`](./api-design.md) |
| ¿Cómo son los datos? | [`data-model.md`](./data-model.md) |

---

## 5. Lo que conviene saber antes de juzgar

Tres cosas que se explican mejor dichas que descubiertas.

### El alcance creció, y está documentado

El MVP se cerró y se desplegó primero. **Horarios, ausencias, vacaciones y calendario laboral
se añadieron después**, a petición expresa. El alcance inicial las listaba como mejoras futuras;
la ampliación es consciente y posterior, no una desviación durante la construcción. En
[`requirements.md`](./requirements.md) §3.1 consta el alcance realmente entregado.

### El proveedor de IA es gratuito porque los datos son ficticios

Con plantilla real la capa gratuita no sería admisible: lo que viaja al proveedor son nombres y
horas de trabajadores identificados, y esas capas suelen reservarse el derecho a entrenar con
ello. Las dos vías válidas —suscripción de pago con encargo de tratamiento, o inferencia local—
están en [ADR-006](./adr/ADR-006-proveedor-de-ia.md), junto con la medición del hardware que
descartó la opción local en este servidor.

### Hay fallos encontrados verificando, no solo pruebas en verde

Durante el desarrollo aparecieron defectos que **las pruebas unitarias no podían encontrar**, porque
dependían del entorno o de datos reales, y se corrigieron: WebDAV interceptando `PUT` y `DELETE` en
IIS, el asistente resolviendo «esta semana» contra una fecha de su corpus de entrenamiento, el
autorrelleno del navegador escribiendo en el buscador de empleados, y celdas de una rejilla CSS
desplazándose al superponerles barras.

Varios de ellos dejaron su rastro en el código, con un comentario que explica el porqué: la fecha
que se inyecta al asistente ([`AiPrompt.cs`](../backend/src/HRIA.Application/Ai/AiPrompt.cs)), la
comparación horaria por `TimeSpan` ([`DashboardService.cs`](../backend/src/HRIA.Application/Dashboard/DashboardService.cs))
y el `web.config` del sitio que desactiva WebDAV. La lección de fondo: **la verificación se hizo
contra el despliegue real**, no confiando en que las pruebas en verde bastaran.

---

## 6. Limitaciones asumidas

Están en [`security.md` §4](./security.md), pero conviene el resumen:

- Sin *refresh tokens* ni revocación de JWT: el token vive hasta caducar (60 min).
- Sin bloqueo de cuenta ni doble factor; solo limitación de peticiones por IP.
- La credencial demo del empleado es **pública a propósito**, para que el proyecto se pueda
  evaluar sin gestionar altas. Los datos son ficticios y no hay ninguna persona real detrás.
- El restablecimiento de contraseña por administración existe en la API pero **no en la
  interfaz**: se retiró para dejar una única puerta visible.
