# BinsaRRHH — Requisitos del proyecto

> **BinsaRRHH** — ERP de Recursos Humanos simplificado con asistente de IA de solo lectura.
> *(Nombre en clave interno del proyecto: **HRIA**, Human Resources Intelligence Assistant.)*
>

## 1. Descripción general

BinsaRRHH es un ERP de RR. HH. simplificado que permite:

1. Gestionar empleados (altas, bajas lógicas, modificaciones y consultas).
2. Controlar la jornada laboral mediante fichajes (entrada, descansos y salida).
3. Calcular horas trabajadas descontando descansos.
4. Definir **horarios** y asignarlos a empleados, con cálculo de desviación frente a lo previsto.
5. Gestionar **ausencias y vacaciones**, con calendario anual de toda la plantilla.
6. Mantener el **calendario laboral** de la empresa (festivos de convenio y fines de semana).
7. Visualizar indicadores y gráficos en un dashboard.
8. Consultar los datos autorizados mediante un **asistente de IA de solo lectura** que usa herramientas controladas.

El sistema aplica autenticación con JWT, autorización por roles validada **siempre en el backend**, auditoría básica y datos de demostración.

## 2. Objetivos

| # | Objetivo |
|---|----------|
| O1 | Entregar un MVP funcional, profesional, seguro y desplegable. |
| O2 | Demostrar buenas prácticas de arquitectura (separación de responsabilidades). |
| O3 | Demostrar integración de IA controlada y segura (herramientas autorizadas, sin acceso directo a BD). |
| O4 | Cumplir los criterios de seguridad (OWASP Top 10 aplicado al alcance). |
| O5 | Entregar documentación completa para el TFM (README, arquitectura, seguridad, testing, guiones). |

## 3. Alcance del MVP

### 3.1 Dentro del alcance (obligatorio)

- Autenticación con correo/contraseña y JWT.
- Gestión de empleados (CRUD + baja lógica + búsqueda/filtros/paginación).
- Control horario (entrada, inicio/fin de descanso, salida) con reglas de negocio.
- Cálculo de horas trabajadas y detección de jornadas incompletas.
- Horarios asignables a empleados y desviación frente a lo previsto.
- Ausencias y vacaciones con flujo de solicitud y resolución.
- Calendario laboral de empresa y calendario anual de vacaciones.
- Gestión de contraseñas (cambio propio y restablecimiento por administración).
- Dashboard con indicadores y gráficos.
- Roles y permisos (Administrador / Empleado).
- Asistente de IA de solo lectura con herramientas controladas y modo demo.
- Auditoría básica (acciones sensibles y consultas de IA).
- Datos ficticios de demostración (seeding).
- Documentación completa, tests de reglas críticas, CI/CD.

> **Ampliación respecto al alcance inicial.** Horarios, ausencias, vacaciones y calendario
> laboral no formaban parte del MVP original: figuraban como mejoras futuras. Se incorporaron
> a petición expresa una vez cerrado el núcleo, y se documentan aquí como alcance real
> entregado.

### 3.2 Fuera del alcance (mejoras futuras)

Nóminas · Firma digital · Gestión documental avanzada · Integración con sistemas externos · RAG documental · Aplicación móvil · Sistemas multiagente · Notificaciones por correo · Recuperación de contraseña por el propio usuario · Cualquier funcionalidad no solicitada.

> Estas opciones se documentan como **mejoras futuras**, no se implementan.

## 4. Usuarios y roles

| Rol | Descripción |
|-----|-------------|
| **Administrador** | Gestiona empleados, consulta todos los registros horarios, accede al dashboard general, usa el asistente de IA (datos agregados) y consulta auditoría. |
| **Empleado** | Consulta sus propios datos, ficha (entrada/descanso/salida), consulta sus jornadas y usa el asistente de IA limitado a sus propios datos. |

### 4.1 Matriz de permisos

| Capacidad | Administrador | Empleado |
|-----------|:---:|:---:|
| Iniciar sesión / usuario actual | ✅ | ✅ |
| Listar / buscar / filtrar empleados | ✅ | ❌ |
| Alta / edición / baja lógica de empleado | ✅ | ❌ |
| Ver detalle de cualquier empleado | ✅ | Solo el propio |
| Fichar (entrada, descanso, salida) | ✅ (propio) | ✅ (propio) |
| Ver estado de fichaje actual | ✅ (propio) | ✅ (propio) |
| Consultar jornadas propias | ✅ | ✅ |
| Consultar jornadas de cualquier empleado | ✅ | ❌ |
| Dashboard general (agregados de la organización) | ✅ | ❌ |
| Definir horarios y asignarlos a empleados | ✅ | ❌ |
| Consultar el horario propio | ✅ | ✅ |
| Solicitar una ausencia o vacaciones | ✅ (propio) | ✅ (propio) |
| Aprobar o denegar solicitudes de ausencia | ✅ | ❌ |
| Ver las ausencias de toda la plantilla / calendario anual | ✅ | ❌ |
| Editar el calendario laboral de la empresa | ✅ | ❌ |
| Consultar el calendario laboral | ✅ | ✅ |
| Cambiar la contraseña propia | ✅ | ✅ |
| Restablecer la contraseña de otro | ✅ *(solo API)* | ❌ |
| Asistente IA — datos agregados / de otros | ✅ | ❌ |
| Asistente IA — datos propios | ✅ | ✅ |
| Consultar auditoría | ✅ | ❌ |

> **Regla de oro:** el frontend oculta o deshabilita opciones por UX, pero **toda autorización se valida en el backend**. Se protege explícitamente frente a acceso horizontal (un empleado accediendo a datos de otro).

## 5. Requisitos funcionales

### RF-AUTH — Autenticación
- RF-AUTH-01: Inicio de sesión con correo y contraseña.
- RF-AUTH-02: Contraseñas almacenadas con hash seguro (no reversible, con sal).
- RF-AUTH-03: Emisión de *access token* JWT firmado.
- RF-AUTH-04: Expiración del token configurable.
- RF-AUTH-05: Protección de rutas y validación de roles en backend.
- RF-AUTH-06: Endpoint para obtener el usuario autenticado actual.
- RF-AUTH-07: Un usuario inactivo no puede autenticarse.
- RF-AUTH-08: Cualquier usuario puede cambiar su propia contraseña aportando la actual.
- RF-AUTH-09: El administrador puede restablecer la de otro sin conocerla; la nueva se devuelve **una única vez** y no queda recuperable.
- RF-AUTH-10: Longitud mínima de contraseña de 8 caracteres; la nueva debe diferir de la actual.
- RF-AUTH-11: Las contraseñas **nunca** se muestran, ni al administrador, ni se registran en auditoría.

### RF-EMP — Empleados
- RF-EMP-01: Listado paginado.
- RF-EMP-02: Búsqueda por nombre o correo.
- RF-EMP-03: Filtro por departamento y estado (activo/inactivo).
- RF-EMP-04: Alta de empleado (con departamento, puesto, fecha de incorporación, estado, rol de acceso).
- RF-EMP-05: Modificación de empleado.
- RF-EMP-06: Consulta detallada de un empleado.
- RF-EMP-07: Baja lógica (no borrado físico).
- RF-EMP-08: Correo único (rechazo de duplicados).
- RF-EMP-09: Validación de datos de entrada.

### RF-TIME — Control horario
- RF-TIME-01: Registrar entrada.
- RF-TIME-02: Iniciar descanso.
- RF-TIME-03: Finalizar descanso.
- RF-TIME-04: Registrar salida.
- RF-TIME-05: Impedir secuencias inválidas (ver reglas de negocio §6).
- RF-TIME-06: Impedir dos jornadas abiertas simultáneas.
- RF-TIME-07: Calcular la duración total trabajada descontando descansos.
- RF-TIME-08: Mostrar el estado actual del empleado.
- RF-TIME-09: Consultar jornadas por empleado y rango de fechas.
- RF-TIME-10: Detectar y marcar jornadas incompletas.

### RF-SCH — Horarios
- RF-SCH-01: Definir plantillas de horario con tramos por día de la semana.
- RF-SCH-02: Asignar un horario a un empleado con vigencia desde una fecha.
- RF-SCH-03: Calcular las horas previstas de una jornada según el horario vigente.
- RF-SCH-04: Mostrar la desviación entre lo trabajado y lo previsto.
- RF-SCH-05: Un empleado sin horario asignado no genera desviación (se muestra vacía, no cero).

### RF-ABS — Ausencias y vacaciones
- RF-ABS-01: Solicitar una ausencia indicando tipo, rango de fechas y motivo.
- RF-ABS-02: Tipos soportados: vacaciones, baja por enfermedad, permiso y asuntos propios.
- RF-ABS-03: Estados de la solicitud: pendiente, aprobada y denegada.
- RF-ABS-04: Solo el administrador aprueba o deniega, y queda registrado quién resolvió.
- RF-ABS-05: Un empleado solo ve y solicita las suyas.
- RF-ABS-06: Saldo anual de vacaciones disponible por empleado.
- RF-ABS-07: Rechazar solapamientos con otra ausencia ya aprobada.
- RF-ABS-08: Calendario anual con las ausencias de toda la plantilla (solo administrador).

### RF-CAL — Calendario laboral
- RF-CAL-01: Marcar días no laborables del año (festivos de convenio).
- RF-CAL-02: Marcar los fines de semana de forma masiva.
- RF-CAL-03: Rejilla de 12 meses para edición y consulta.
- RF-CAL-04: Los días no laborables se excluyen de los cómputos que dependen de días hábiles.

### RF-DASH — Dashboard
- RF-DASH-01: Nº de empleados activos.
- RF-DASH-02: Empleados trabajando actualmente.
- RF-DASH-03: Empleados en descanso.
- RF-DASH-04: Jornadas incompletas.
- RF-DASH-05: Actividad reciente de fichajes, paginada.
- RF-DASH-06: Gráfico de estado de la plantilla.
- RF-DASH-07: Gráfico de horas trabajadas por día.
- RF-DASH-08: Gráfico de ausencias, vacaciones y personal trabajando en el mes en curso.
- RF-DASH-09: Gráfico de puntualidad: porcentaje de fichajes dentro y fuera del horario asignado.
- RF-DASH-10: Tabla de previsión de ausencias a dos semanas vista.
- RF-DASH-11: El empleado ve un resumen de sus fichajes del mes, solo en días laborables.
- RF-DASH-12: Los colores de los gráficos deben ser distinguibles con daltonismo.

### RF-AI — Asistente de IA
- RF-AI-01: Responder preguntas en lenguaje natural sobre datos autorizados.
- RF-AI-02: El modelo **no** accede directamente a la base de datos.
- RF-AI-03: Herramientas controladas en backend (`get_current_working_employees`, `get_open_time_entries`, `get_incomplete_workdays`, `get_employee_hours_summary`, `get_department_hours_summary`).
- RF-AI-04: El backend determina qué herramientas puede usar el usuario según su rol y solo envía esas al modelo.
- RF-AI-05: Validación de argumentos de las herramientas.
- RF-AI-06: Ejecución de consultas parametrizadas y resultados limitados.
- RF-AI-07: Registro de la consulta en auditoría (pregunta, herramientas, duración, estado).
- RF-AI-08: Solo lectura (no crea, modifica ni elimina).
- RF-AI-09: **Modo demo** con respuestas controladas si no hay API key configurada.
- RF-AI-10: Un empleado solo puede consultar sus propios datos.
- RF-AI-11: Proveedor intercambiable por configuración, sin cambios de código.
- RF-AI-12: El asistente se abre desde una ventana flotante disponible en todas las pantallas, y la conversación sobrevive a la navegación.
- RF-AI-13: El modelo recibe la fecha actual, para resolver rangos relativos como «esta semana».
- RF-AI-14: El resultado de las herramientas incluye el rango realmente consultado, para que la respuesta no pueda atribuir las cifras a otro periodo.

### RF-AUD — Auditoría
- RF-AUD-01: Registrar acciones sensibles (login, CRUD de empleados, consultas de IA).
- RF-AUD-02: Consulta de auditoría restringida a Administrador.

### RF-DEMO — Datos de demostración
- RF-DEMO-01: Usuario administrador y usuario empleado de demo.
- RF-DEMO-02: Varios departamentos.
- RF-DEMO-03: Entre 8 y 12 empleados ficticios.
- RF-DEMO-04: Jornadas completas, incompletas, empleados trabajando y en descanso.

## 6. Reglas de negocio del control horario

| # | Regla |
|---|-------|
| BR-01 | No se puede registrar entrada si ya existe una jornada abierta. |
| BR-02 | No se puede iniciar un descanso sin una jornada abierta. |
| BR-03 | No puede haber dos descansos abiertos a la vez. |
| BR-04 | No se puede finalizar un descanso que no existe / no está abierto. |
| BR-05 | No se puede registrar la salida con un descanso abierto. |
| BR-06 | No se puede registrar la salida sin una jornada abierta. |
| BR-07 | Total trabajado = (salida − entrada) − suma de descansos. |
| BR-08 | Una jornada con entrada pero sin salida se marca como **incompleta** cuando corresponde (p. ej. cambio de día). |
| BR-09 | Todas las marcas temporales se almacenan en **UTC**; se convierten a la zona del usuario al mostrarlas. |
| BR-10 | Los tramos de horario son hora **local del centro de trabajo**, no UTC: un horario de 08:00 no cambia con el horario de verano. |
| BR-11 | Desviación = trabajado − previsto según el horario vigente ese día. Sin horario asignado, no hay desviación. |
| BR-12 | Una ausencia aprobada no puede solaparse con otra aprobada del mismo empleado. |
| BR-13 | Los días marcados como no laborables en el calendario de empresa no computan como días hábiles. |

## 7. Requisitos no funcionales

| Categoría | Requisito |
|-----------|-----------|
| Seguridad | Hash de contraseñas, validación de entradas, autorización por roles, consultas parametrizadas (EF Core), CORS restrictivo configurable, *rate limiting* en login y asistente IA, sin secretos en el repo, logs sin datos sensibles, manejo global de excepciones, ocultación de detalles internos en producción, protección frente a acceso horizontal, sanitización de parámetros hacia la IA. |
| Calidad | Compila sin errores, se ejecuta localmente sin dependencias externas, migraciones de BD, validación de datos, tests de reglas críticas, Swagger, interfaz coherente, datos demo, README completo, listo para GitHub. |
| Usabilidad | Diseño responsive, accesibilidad básica (roles ARIA, foco, contraste), estados de carga y vacío, mensajes claros de error/éxito. |
| Mantenibilidad | Arquitectura modular por capas (backend) y por funcionalidades (frontend), DTOs, validadores, cliente HTTP centralizado con interceptores. |
| Observabilidad | Logging estructurado, endpoint `/health`, auditoría. |
| Internacionalización | Fechas en UTC en BD; presentación en zona horaria del usuario. |

## 8. Criterios de aceptación globales (Definition of Done del MVP)

- [x] Backend y frontend compilan sin errores.
- [x] El proyecto se ejecuta localmente (`dotnet run` + `npm run dev`) sin dependencias externas.
- [x] Existen migraciones de BD y seeding de datos demo.
- [x] `/health` responde y Swagger está disponible **solo** en desarrollo.
- [x] Login funciona con las credenciales demo y respeta roles.
- [x] Reglas de negocio del control horario cubiertas por tests (xUnit).
- [x] El asistente de IA funciona en modo demo sin API key y respeta permisos.
- [x] No hay secretos en el repositorio; existe configuración de ejemplo.
- [x] Manejo global de errores y logs sin datos sensibles.
- [x] README y documentación de seguridad/testing completas.
- [x] Aplicación desplegada y accesible por HTTPS con certificado de CA reconocida.

## 9. Trazabilidad

Cada requisito funcional se enlaza con historias de usuario en [`user-stories.md`](./user-stories.md), con casos de uso en [`use-cases.md`](./use-cases.md) y con endpoints en [`api-design.md`](./api-design.md).
