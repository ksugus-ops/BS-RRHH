# BinsaRRHH — Diseño de la API

Base URL: `/api`. Todas las respuestas son JSON. Autenticación con **JWT Bearer** salvo `/health` y `/auth/login`.
Errores con formato consistente (ver §6). Fechas en **UTC** (ISO 8601, sufijo `Z`).

## 1. Convenciones

- **Auth:** cabecera `Authorization: Bearer <token>`.
- **Autorización:** políticas `AdminOnly` y `Authenticated`. La protección horizontal (empleado ↔ sus datos) se valida en el servicio.
- **Paginación:** `?page=1&pageSize=20` → respuesta `{ items, page, pageSize, total }`.
- **Códigos:** `200` OK, `201` creado, `204` sin contenido, `400` validación, `401` no autenticado, `403` sin permiso, `404` no encontrado, `409` conflicto (duplicado / regla de negocio), `429` rate limit, `500` error interno (sin detalle en producción).

## 2. Salud y sistema

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/health` | — | Estado del servicio y (opcional) de la BD. |
| GET | `/swagger` | dev | UI de Swagger (solo desarrollo). |

## 3. Autenticación — `/api/auth`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| POST | `/auth/login` | — | — | Login con `{ email, password }` → `{ token, expiresAt, user }`. *Rate limited.* |
| GET | `/auth/me` | ✅ | cualquiera | Usuario autenticado actual `{ id, email, role, employee }`. |

> No hay endpoint de registro público: los usuarios se crean al dar de alta empleados (rol Admin) o por seeding. El logout es del lado cliente (descartar token).

### Contraseñas

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| POST | `/auth/change-password` | ✅ | Autenticado | Cambia la **propia**. Exige la actual. Límite de peticiones. `204` si va bien. |
| POST | `/auth/reset-password/{employeeId}` | ✅ | Admin | Restablece la de otro. Devuelve la nueva **una sola vez**. |

**Por qué está así diseñado**

- Las contraseñas se guardan como hash **PBKDF2 irreversible**: no existe ningún endpoint que
  las devuelva, ni para el administrador. Consultarlas es imposible por diseño, no por omisión.
- El cambio propio **exige la contraseña actual**. Sin ese requisito, quien robase un token
  podría apropiarse de la cuenta cambiándola.
- El restablecimiento **no pide la actual**: el administrador no debe conocerla. A cambio, la
  nueva viaja de vuelta una única vez para que pueda comunicarla, y no se almacena de forma
  recuperable ni puede volver a consultarse.
- Si no se indica una contraseña al restablecer, se **genera** con el generador criptográfico,
  excluyendo caracteres que se confunden al dictarla (`l`, `I`, `1`, `O`, `0`).
- Ambas operaciones quedan en **auditoría** registrando el hecho y quién lo hizo, **nunca la
  contraseña**.

> ⚠️ **Limitación conocida.** Los JWT no tienen revocación, así que tras un restablecimiento el
> token anterior del empleado sigue siendo válido hasta que caduca (60 min por defecto). Se
> asume en el alcance del MVP; la solución sería una marca de «contraseña cambiada en» que el
> token compare al validarse.

## 4. Empleados — `/api/employees`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/employees` | ✅ | Admin | Listado paginado. Query: `search`, `departmentId`, `isActive`, `page`, `pageSize`. |
| GET | `/employees/{id}` | ✅ | Admin o el propio | Detalle. Empleado solo puede ver el suyo (403 en otro caso). |
| POST | `/employees` | ✅ | Admin | Alta `{ firstName, lastName, email, departmentId, position, hireDate, role, initialPassword }`. Crea empleado + usuario. `201`. |
| PUT | `/employees/{id}` | ✅ | Admin | Modificación de datos. |
| POST | `/employees/{id}/deactivate` | ✅ | Admin | Baja lógica (desactiva empleado y su usuario). `204`. |
| GET | `/departments` | ✅ | cualquiera | Lista de departamentos activos (para filtros y formularios). |

**Validaciones (400):** campos obligatorios, formato de email, longitudes; **409** si el email ya existe.

## 5. Control horario — `/api/time`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/time/status` | ✅ | cualquiera | Estado actual del usuario `{ state: NotStarted\|Working\|OnBreak, workday? }`. |
| POST | `/time/check-in` | ✅ | cualquiera | Registrar entrada. `409` si ya hay jornada abierta (BR-01). |
| POST | `/time/break/start` | ✅ | cualquiera | Iniciar descanso. `409` si no hay jornada o ya hay descanso (BR-02/03). |
| POST | `/time/break/end` | ✅ | cualquiera | Finalizar descanso. `409` si no hay descanso abierto (BR-04). |
| POST | `/time/check-out` | ✅ | cualquiera | Registrar salida. `409` si descanso abierto o sin jornada (BR-05/06). Devuelve total trabajado. |
| GET | `/time/workdays` | ✅ | cualquiera | Jornadas propias. Query: `from`, `to`. |
| GET | `/time/workdays?employeeId={id}` | ✅ | Admin | Jornadas de un empleado (solo Admin puede indicar `employeeId` distinto al propio). |

Los fichajes actúan **siempre sobre el usuario autenticado**; el `employeeId` se toma del token, no del cliente (evita suplantación).

### Previsto frente a real

Cada jornada devuelve, además de lo fichado, lo que estaba **previsto** por el horario asignado:

| Campo | Significado |
|-------|-------------|
| `workedMinutes` | Minutos realmente trabajados (descansos descontados). |
| `expectedMinutes` | Minutos previstos por el horario. **`null` si el empleado no tiene horario vigente ese día.** |
| `deviationMinutes` | `workedMinutes − expectedMinutes`. Negativo = falta jornada. `null` si no hay previsión. |

`expectedMinutes` vale **0**, no `null`, cuando sí hay horario pero ese día no toca trabajar:
fin de semana, festivo del calendario o **ausencia aprobada**. Esa última condición evita que
estar de vacaciones aparezca como una desviación negativa.

> La distinción entre `null` y `0` es deliberada: devolver `0` para quien no tiene horario haría
> parecer que **todo** lo fichado es exceso de jornada.

## 6. Dashboard — `/api/dashboard`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/dashboard/summary` | ✅ | Admin | Ver campos abajo. |
| GET | `/dashboard/hours-by-day?from=&to=` | ✅ | Admin | Serie de horas trabajadas por día para el gráfico. |
| GET | `/dashboard/absences-by-type?year=` | ✅ | Admin | Días aprobados agrupados por tipo de ausencia. |
| GET | `/dashboard/vacation-summary?year=` | ✅ | Admin | Saldo de vacaciones agregado de la plantilla activa. |
| GET | `/dashboard/month-activity?year=&month=` | ✅ | Admin | Totales del mes: días trabajados, de vacaciones y de otras ausencias. |
| GET | `/dashboard/punctuality?year=&month=&toleranceMinutes=` | ✅ | Admin | Jornadas fichadas dentro y fuera del horario asignado. Tolerancia por defecto: 5 min. |

**Cómo se calcula la puntualidad**

Solo entran las jornadas **cerradas** de empleados con **horario asignado ese día**; el resto no
son comparables y quedan fuera del cómputo (no cuentan como incumplimiento). Se compara la
entrada con el inicio del primer tramo y la salida con el fin del último, y basta que una de las
dos se desvíe más de la tolerancia para contar como fuera de horario. Entrar antes o salir después
**no penaliza**.

> ⚠️ Los tramos del horario son **hora local del centro de trabajo** y los fichajes se guardan en
> **UTC**: la comparación convierte antes de restar. Sin esa conversión el resultado quedaría
> desplazado por el huso y por el horario de verano.
| GET | `/dashboard/upcoming-absences` | ✅ | Admin | Ausencias de la **semana actual y la siguiente**, con los días laborables que consume cada una en cada semana. |

`upcoming-absences` calcula las semanas ISO (de lunes a domingo), recorta cada ausencia a cada
semana y delega el cómputo en el mismo calculador que usa el resto de la aplicación, de modo que
los días respetan el calendario laboral y el horario del empleado.

`/dashboard/summary` devuelve:

```
activeEmployees, working, onBreak, incompleteWorkdays, hoursTodayMinutes, recentPunches[],
expectedTodayMinutes        minutos previstos hoy por los horarios asignados
employeesScheduledToday     empleados con jornada prevista hoy
onLeaveToday                empleados con ausencia aprobada hoy
pendingAbsenceRequests      solicitudes pendientes de resolver
```

## 7. Asistente de IA — `/api/ai`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| POST | `/ai/ask` | ✅ | cualquiera | `{ question }` → `{ answer, toolsUsed[], mode: live\|demo, status }`. *Rate limited.* |

**Herramientas disponibles (definidas en backend):**

| Herramienta | Rol requerido | Parámetros | Devuelve |
|-------------|---------------|-----------|----------|
| `get_current_working_employees` | Admin | — | Empleados con jornada abierta (nombre, depto, hora de entrada). |
| `get_open_time_entries` | Admin | — | Jornadas abiertas actuales. |
| `get_incomplete_workdays` | Admin | `from?`, `to?` | Jornadas marcadas incompletas. |
| `get_employee_hours_summary` | Admin (cualquiera) / Employee (solo el propio) | `employeeId?`, `from`, `to` | Resumen de horas de un empleado. |
| `get_department_hours_summary` | Admin | `departmentId`, `from`, `to` | Resumen agregado por departamento. |

**Reglas:** el backend selecciona las herramientas según el rol antes de llamar al modelo; valida y sanea argumentos; para Empleado fuerza `employeeId = usuario actual`; ejecuta consultas parametrizadas; limita resultados; audita la consulta. Sin API key → **modo demo** con respuestas controladas.

## 8. Auditoría — `/api/audit`

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/audit` | ✅ | Admin | Listado paginado de registros de auditoría. Query: `from`, `to`, `action`, `page`, `pageSize`. |
| GET | `/audit/ai` | ✅ | Admin | Listado de consultas de IA (AiQueryLog). |

## 8-bis. Horarios — `/api/schedules`

Un **horario** es una plantilla reutilizable con tramos por día de la semana
(`ScheduleSlot`), que se **asigna** a empleados durante un periodo.

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/schedules` | ✅ | Admin | Listado de horarios. Query: `includeInactive`. |
| GET | `/schedules/{id}` | ✅ | Admin | Detalle con sus tramos. |
| POST | `/schedules` | ✅ | Admin | Alta. `409` si el nombre ya existe; `400` si los tramos se solapan. |
| PUT | `/schedules/{id}` | ✅ | Admin | Modificación. Los tramos se **reemplazan en bloque**. |
| POST | `/schedules/{id}/deactivate` | ✅ | Admin | Baja lógica. `409` si tiene asignaciones vigentes. |
| GET | `/schedules/assignments` | ✅ | Admin \| propio | Asignaciones. Query: `employeeId`, `scheduleId`. El empleado solo obtiene las suyas. |
| POST | `/schedules/assignments` | ✅ | Admin | Asigna horario a empleado. `409` si el periodo se solapa con otro del mismo empleado. |
| PUT | `/schedules/assignments/{id}` | ✅ | Admin | Cambia las fechas de la asignación. |
| DELETE | `/schedules/assignments/{id}` | ✅ | Admin | Elimina la asignación. |
| GET | `/schedules/effective/{employeeId}` | ✅ | Admin \| propio | Horario vigente en una fecha (`date`, por defecto hoy). `204` si no tiene ninguno. |

**Reglas de negocio**

- Un horario necesita **al menos un tramo**, y cada tramo debe terminar después de empezar.
- Dos tramos del **mismo día** no pueden solaparse; contiguos sí se admiten (jornada partida).
- Un empleado **no puede tener dos asignaciones solapadas**: si no, no habría forma de saber
  qué horario se le aplica en una fecha dada. Una asignación sin fecha de fin bloquea
  cualquier otra posterior.
- Las horas de los tramos son **locales del centro de trabajo**, no UTC.

## 8-ter. Calendario laboral — `/api/work-calendars`

Un calendario por **año natural**: qué días de la semana no se trabaja y qué días concretos
son festivos (nacional, autonómico, local, **convenio** o empresa).

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/work-calendars` | ✅ | Admin | Calendarios disponibles. |
| GET | `/work-calendars/{year}` | ✅ | Admin | Detalle del año con sus festivos y días laborables. |
| GET | `/work-calendars/{year}/days` | ✅ | Autenticado | Los 365/366 días con su condición de laborable, fin de semana o festivo. Alimenta la vista anual de 12 meses. |
| POST | `/work-calendars` | ✅ | Admin | Alta. `409` si el año ya tiene calendario. |
| PUT | `/work-calendars/{id}` | ✅ | Admin | Modificación (nombre, actividad, días no laborables). |
| DELETE | `/work-calendars/{id}` | ✅ | Admin | Elimina el calendario y sus festivos. |
| POST | `/work-calendars/{id}/holidays` | ✅ | Admin | Añade festivo. `400` si la fecha no cae en el año; `409` si ya hay uno ese día. |
| DELETE | `/work-calendars/{id}/holidays/{holidayId}` | ✅ | Admin | Elimina el festivo. |

**Reglas de negocio**

- Los días de la semana no laborables son **configurables**: el valor por defecto es sábado y
  domingo, pero hay centros que trabajan el sábado. No se pueden marcar los siete.
- Un festivo debe pertenecer al **año del calendario**, y solo puede haber uno por fecha.
- `GET /{year}/days` **no falla si el año no tiene calendario**: devuelve el criterio por
  defecto para que la vista anual pueda pintarse igualmente.

## 8-quater. Ausencias y vacaciones — `/api/absences`

Las **vacaciones no son una entidad aparte**: son un tipo de ausencia marcado con
`ConsumesVacationBalance`, de modo que solicitud y aprobación siguen un único flujo.

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/absences/types` | ✅ | Autenticado | Catálogo de tipos activos. |
| GET | `/absences` | ✅ | Admin \| propio | Listado paginado. Query: `employeeId`, `absenceTypeId`, `status`, `from`, `to`, `page`, `pageSize`. |
| GET | `/absences/{id}` | ✅ | Admin \| propio | Detalle. |
| POST | `/absences` | ✅ | Autenticado | Crea solicitud. El empleado solo puede para sí mismo. |
| POST | `/absences/{id}/approve` | ✅ | Admin | Aprueba una pendiente. |
| POST | `/absences/{id}/reject` | ✅ | Admin | Rechaza una pendiente. |
| POST | `/absences/{id}/cancel` | ✅ | Admin \| propio | Retira una pendiente. |
| GET | `/absences/calendar/{year}` | ✅ | Admin | **Calendario anual de vacaciones** de toda la plantilla, agrupado por empleado. Alimenta la vista de 12 meses. |

| Método | Ruta | Auth | Rol | Descripción |
|--------|------|------|-----|-------------|
| GET | `/vacations/balance/{employeeId}` | ✅ | Admin \| propio | Saldo del año (`year`, por defecto el actual). |
| GET | `/vacations/balances` | ✅ | Admin | Saldo de toda la plantilla activa. |
| PUT | `/vacations/allowance` | ✅ | Admin | Fija los días concedidos a un empleado para un año. |

**Reglas de negocio**

- El `employeeId` de una solicitud se toma **del token** cuando quien la crea es un empleado;
  si lo envía en el cuerpo, **se ignora**. Solo el administrador puede solicitar en nombre de otro.
- No se admiten **solapamientos** con otras solicitudes pendientes o aprobadas del mismo
  empleado. Las rechazadas o retiradas no bloquean.
- El periodo debe contener **al menos un día laborable** para ese empleado; si no, la solicitud
  no tendría efecto.
- Los tipos con `RequiresApproval = false` (p. ej. una baja justificada) **nacen aprobados**.
- Las **vacaciones no pueden abarcar dos años naturales**: repartirían días entre dos saldos con
  reglas discutibles. Se pide dividir la solicitud.
- El **saldo se comprueba dos veces**: al solicitar y al aprobar. Entre ambos momentos pueden
  haberse aprobado otras solicitudes.
- El calendario anual incluye **aprobadas y pendientes**: el administrador necesita ver lo que
  está por decidir para detectar solapamientos entre compañeros antes de aprobar.

**Saldo de vacaciones**

```
disponibles = concedidos − aprobados − pendientes
```

Las solicitudes se imputan al año de su fecha de inicio; como no pueden cruzar el año, no hay
ambigüedad.

## 9. Formato de error uniforme

```json
{
  "type": "https://hria/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": { "email": ["El correo no es válido."] },
  "traceId": "00-abc..."
}
```

- En **producción**, `500` devuelve un mensaje genérico sin *stack trace*.
- Errores de regla de negocio del fichaje → `409` con `title` legible (p. ej. "Ya existe una jornada abierta.").

## 10. Seguridad transversal de la API

- CORS restrictivo configurable (orígenes permitidos por entorno).
- Rate limiting en `/auth/login` y `/ai/ask`.
- Todos los endpoints (salvo `/health`, `/auth/login`) exigen JWT válido.
- El `employeeId` de las operaciones propias se deriva del token.
- Logs sin credenciales ni PII sensible.

## 11. Resumen de endpoints

```
GET    /health
POST   /api/auth/login          (rate limited)
GET    /api/auth/me
POST   /api/auth/change-password              (rate limited)
POST   /api/auth/reset-password/{employeeId}  (Admin)
GET    /api/employees           (Admin)
GET    /api/employees/{id}      (Admin | propio)
POST   /api/employees           (Admin)
PUT    /api/employees/{id}      (Admin)
POST   /api/employees/{id}/deactivate  (Admin)
GET    /api/departments
GET    /api/time/status
POST   /api/time/check-in
POST   /api/time/break/start
POST   /api/time/break/end
POST   /api/time/check-out
GET    /api/time/workdays
GET    /api/dashboard/summary          (Admin)
GET    /api/dashboard/hours-by-day     (Admin)
GET    /api/dashboard/absences-by-type (Admin)
GET    /api/dashboard/vacation-summary (Admin)
GET    /api/dashboard/upcoming-absences (Admin)
GET    /api/dashboard/month-activity   (Admin)
GET    /api/dashboard/punctuality      (Admin)
POST   /api/ai/ask               (rate limited)
GET    /api/audit                (Admin)
GET    /api/audit/ai             (Admin)

GET    /api/schedules                          (Admin)
GET    /api/schedules/{id}                     (Admin)
POST   /api/schedules                          (Admin)
PUT    /api/schedules/{id}                     (Admin)
POST   /api/schedules/{id}/deactivate          (Admin)
GET    /api/schedules/assignments              (Admin | propio)
POST   /api/schedules/assignments              (Admin)
PUT    /api/schedules/assignments/{id}         (Admin)
DELETE /api/schedules/assignments/{id}         (Admin)
GET    /api/schedules/effective/{employeeId}   (Admin | propio)

GET    /api/work-calendars                     (Admin)
GET    /api/work-calendars/{year}              (Admin)
GET    /api/work-calendars/{year}/days         (Autenticado)
POST   /api/work-calendars                     (Admin)
PUT    /api/work-calendars/{id}                (Admin)
DELETE /api/work-calendars/{id}                (Admin)
POST   /api/work-calendars/{id}/holidays       (Admin)
DELETE /api/work-calendars/{id}/holidays/{hId} (Admin)

GET    /api/absences/types                     (Autenticado)
GET    /api/absences                           (Admin | propio)
GET    /api/absences/{id}                      (Admin | propio)
POST   /api/absences                           (Autenticado)
POST   /api/absences/{id}/approve              (Admin)
POST   /api/absences/{id}/reject               (Admin)
POST   /api/absences/{id}/cancel               (Admin | propio)
GET    /api/absences/calendar/{year}           (Admin)
GET    /api/vacations/balance/{employeeId}     (Admin | propio)
GET    /api/vacations/balances                 (Admin)
PUT    /api/vacations/allowance                (Admin)
```
