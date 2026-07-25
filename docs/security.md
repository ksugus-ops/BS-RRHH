# BinsaRRHH — Seguridad

Revisión de seguridad del MVP siguiendo OWASP Top 10 (2021) aplicado al alcance.
Última revisión: Fase 8.

## 1. Resumen de controles implementados

| Control | Implementación |
|---------|----------------|
| Hash de contraseñas | PBKDF2-HMAC-SHA256, 100 000 iteraciones, sal aleatoria de 128 bits, comparación en tiempo constante (`Pbkdf2PasswordHasher`). |
| Autenticación | JWT firmado (HS256), expiración configurable, validación de emisor/audiencia/firma/vigencia. |
| Autorización | Políticas `AdminOnly` y `[Authorize]`; validación **en backend**, no solo en frontend. |
| Protección horizontal | El `employeeId` de las operaciones propias se deriva del **token**, nunca del cliente. El asistente de IA fuerza el id propio para empleados. |
| Inyección | EF Core con LINQ parametrizado; sin SQL concatenado. El asistente no genera SQL. |
| Validación de entrada | FluentValidation en los DTOs (login, alta/edición de empleado); saneamiento de la pregunta a la IA. |
| CORS | Restrictivo y configurable por entorno (`Cors:AllowedOrigins`); sin comodín en producción. |
| Rate limiting | Ventana fija por IP en `/api/auth/login` (5/min) y `/api/ai/ask` (10/min). |
| Gestión de secretos | Sin secretos en el repositorio; `appsettings.json` con valores vacíos; `.env` ignorado; `.env.example` documentado. |
| Manejo de errores | Middleware global; en producción oculta detalles internos y *stack traces*. |
| Logs | Estructurados, sin contraseñas, tokens ni PII sensible. |
| Auditoría | Acciones sensibles (login, CRUD de empleados) y consultas de IA registradas. |
| Swagger | Solo habilitado en desarrollo. |

## 2. OWASP Top 10 (2021) — aplicación

### A01: Broken Access Control
- Todas las rutas (salvo `/health` y `/auth/login`) exigen JWT.
- Endpoints de administración protegidos con la política `AdminOnly` (`/employees`, `/dashboard`, `/audit`, herramientas IA de agregación).
- **Protección horizontal:** un empleado no puede leer la ficha ni las jornadas de otro; el asistente de IA fuerza el `employeeId` propio.
- **Evidencia:** tests `GetById_AsEmployee_ForOtherEmployee_ThrowsForbidden`, `Employee_CannotListEmployees_Returns403`, `Audit_AsEmployee_Returns403`, `EmployeeHoursSummary_AsEmployee_IgnoresRequestedOtherEmployeeId`.

### A02: Cryptographic Failures
- Contraseñas con PBKDF2 + sal aleatoria; nunca en texto plano.
- Secreto JWT desde configuración/entorno; en producción es obligatorio (la app falla si falta).
- **Evidencia:** `Pbkdf2PasswordHasherTests`, `Program.cs` (validación del secreto).

### A03: Injection
- Acceso a datos exclusivamente vía EF Core (consultas parametrizadas).
- El asistente de IA no construye SQL a partir del texto del usuario; solo ejecuta herramientas predefinidas con argumentos validados.
- **Evidencia:** revisión de código; ausencia de SQL crudo.

### A04: Insecure Design
- Máquina de estados del fichaje con reglas de negocio explícitas (BR-01..BR-08) e índice único filtrado de jornada abierta en BD.
- El asistente es de **solo lectura** y con herramientas acotadas por rol.
- **Evidencia:** `TimeTrackingServiceTests`, `AiToolRegistryTests`.

### A05: Security Misconfiguration
- CORS restrictivo configurable; Swagger solo en desarrollo; manejo global de excepciones que oculta detalles en producción.
- **Evidencia:** `Program.cs`, `ExceptionHandlingMiddleware`.

### A06: Vulnerable and Outdated Components
- Dependencias fijadas a versiones de la línea .NET 8 LTS y del ecosistema Vue 3 actual.
- `npm audit` sin vulnerabilidades en la instalación.
- **Riesgo residual:** requiere revisión periódica de avisos (Dependabot recomendado).

### A07: Identification and Authentication Failures
- Mensajes de error genéricos en login (no se revela si falla el correo o la contraseña).
- Verificación "señuelo" para igualar tiempos y mitigar enumeración de usuarios.
- Usuario/empleado inactivo no puede autenticarse. Rate limiting en login.
- **Evidencia:** `AuthServiceTests` (contraseña incorrecta, usuario inactivo, empleado inactivo).

### A08: Software and Data Integrity Failures
- Migraciones versionadas; seeding idempotente; sin deserialización insegura.

### A09: Security Logging and Monitoring Failures
- Auditoría de acciones sensibles y de consultas de IA (usuario, acción, estado, duración).
- Logs sin datos sensibles.
- **Evidencia:** `AuditLog`, `AiQueryLog`, endpoints `/api/audit`.

### A10: Server-Side Request Forgery (SSRF)
- La única llamada saliente es a la API del proveedor de IA (Groq en el despliegue actual) con URL de configuración fija; el usuario no controla destinos.

## 3. Tabla de riesgos

| # | Riesgo | Nivel | Mitigación aplicada | Evidencia | Riesgo residual |
|---|--------|:-----:|--------------------|-----------|-----------------|
| R1 | Acceso horizontal a datos de otros empleados | Alto | `employeeId` desde el token; validación en servicio; filtro forzado en IA | Tests 403 y de IA | Bajo |
| R2 | Prompt injection / abuso del asistente | Alto | Herramientas por rol; sin SQL libre; validación de argumentos; límites; rate limiting | `Ask_PromptInjection…`, `AiToolRegistryTests` | Bajo |
| R3 | Secretos en el repositorio | Alto | `.gitignore`; `appsettings` sin secretos; `.env.example`; escaneo | Escaneo Fase 8 (solo contraseña demo en tests) | Bajo |
| R4 | Estados de fichaje inconsistentes | Medio | Máquina de estados + índice único filtrado + tests | `TimeTrackingServiceTests` | Bajo |
| R5 | Errores de zona horaria | Medio | UTC en BD (convertidor global) + conversión en presentación | Corrección Fase 5 | Bajo |
| R6 | Enumeración de usuarios en login | Medio | Mensaje genérico + verificación señuelo + rate limiting | `AuthServiceTests` | Bajo |
| R7 | Fuerza bruta en login | Medio | Rate limiting por IP (5/min) | `Program.cs` | Medio (sin bloqueo de cuenta ni MFA en el MVP) |
| R8 | Exposición de detalles internos en errores | Medio | Manejo global; detalle solo en desarrollo | `ExceptionHandlingMiddleware` | Bajo |
| R9 | Dependencias vulnerables | Medio | Versiones fijadas; `npm audit` limpio | Instalación | Medio (requiere seguimiento continuo) |
| R10 | Coste/disponibilidad del proveedor de IA | Bajo | Abstracción + modo demo sin API key | `DemoAssistant` | Bajo |
| R11 | **Credenciales demo públicas en un despliegue accesible** | Alto *(si el despliegue fuese público)* | Contraseña del **administrador cambiada tras el despliegue** por una aleatoria y no publicada; despliegue en red interna; datos ficticios; sin información real de personas | `db/03-seed-demo.sql`, `README.md` | **Bajo-medio**, ver §4 |
| R12 | Certificado HTTPS no confiable | Medio | Certificado comodín de **CA reconocida** en los dos sitios públicos; sin avisos de navegador | `docs/deployment-iis.md` §6 | Bajo |
| R13 | **Aplicación expuesta a Internet** | Medio | Publicación **temporal** limitada al periodo de evaluación; solo HTTPS; sin credencial de administración pública; base de datos aislada con usuario de permisos mínimos; datos ficticios | Regla de firewall revocable | Medio mientras dure la exposición |
| R14 | **Datos de plantilla enviados a un proveedor de IA externo** | Alto *(solo con datos reales)* | Hoy se opera con **datos ficticios**; con datos reales la capa gratuita queda **excluida**: se exige suscripción de pago con acuerdo de encargado del tratamiento, o **inferencia local** | [ADR-006](./adr/ADR-006-proveedor-de-ia.md), `db/03-seed-demo.sql` | **Bajo** con datos demo · **bloqueante** antes de una puesta en marcha real, ver §4 |

## 3-bis. Gestión de contraseñas

Las contraseñas se guardan como hash **PBKDF2-HMAC-SHA256** (100.000 iteraciones, sal por
contraseña). Es **irreversible**: no hay forma de recuperar el texto original, y por tanto
**ninguna pantalla ni endpoint las muestra**, tampoco al administrador.

| Operación | Quién | Expuesta en la interfaz | Exige | Devuelve |
|-----------|-------|-------------------------|-------|----------|
| Cambiar la propia | Cualquier usuario | Sí, botón de la barra superior | La contraseña actual | Nada (`204`) |
| Restablecer la de otro | Solo administrador | **No**, solo API | Nada (no debe conocerla) | La nueva, **una única vez** |

> El restablecimiento **ya no tiene interfaz**: se retiró de la lista de empleados para dejar
> una única puerta visible a la gestión de contraseñas, la del propio usuario. El endpoint
> sigue existiendo, protegido por la política `AdminOnly` y cubierto por pruebas, como vía
> operativa para desbloquear a quien olvide la suya.

Decisiones y su porqué:

- **El cambio propio exige la actual.** Sin ese requisito, un token robado bastaría para
  apropiarse de la cuenta. Además el endpoint está **limitado a 5 peticiones/minuto por IP**,
  porque recibe una contraseña y sería un objetivo de fuerza bruta.
- **El restablecimiento no la exige**, porque el administrador no debe conocer la contraseña de
  nadie. La nueva se muestra una sola vez para que pueda comunicarla y no queda almacenada de
  forma recuperable.
- La contraseña generada usa el **generador criptográfico** (no `Random`) y excluye caracteres
  que se confunden al dictarla.
- Ambas quedan en **auditoría** con el hecho y el autor, **nunca con la contraseña**. Hay una
  prueba que verifica que el texto no aparece ni en el registro ni en el usuario.

> **Por qué un administrador no puede ver las contraseñas.** Sería técnicamente posible solo
> guardándolas en claro o cifradas de forma reversible, lo que contradiría A02 (fallos
> criptográficos): ante una fuga no se perderían hashes sino credenciales utilizables, y dado
> que las personas reutilizan contraseñas, comprometería también sus cuentas ajenas al sistema.
> La necesidad operativa real —desbloquear a quien la ha olvidado— se cubre con el
> restablecimiento, sin necesidad de leerla.

## 4. Limitaciones de seguridad conocidas (MVP)

- No hay **refresh tokens** ni revocación de JWT (el token vive hasta su expiración). En
  particular, tras **restablecer** la contraseña de un empleado su token anterior sigue siendo
  válido hasta caducar (60 min). Se mitigaría con una marca de «contraseña cambiada en»
  comparada al validar el token.
- No hay **bloqueo de cuenta** tras N intentos ni **MFA** (solo rate limiting).
- El rate limiting es **en memoria por instancia** (no distribuido); en despliegue multi-instancia conviene un almacén compartido.

### Credenciales de demostración: riesgo asumido conscientemente

El *seeding* crea `admin@hria.local` y `empleado@hria.local` con la contraseña **`Demo1234!`**,
que aparece publicada en el `README.md`, en esta documentación, en `db/03-seed-demo.sql` y en el
vídeo de presentación del TFM.

Es una decisión deliberada para que cualquiera pueda levantar el proyecto en local y evaluarlo sin
gestionar altas de usuarios.

**Mitigación aplicada en el entorno desplegado:** tras el despliegue se cambió la contraseña del
usuario **administrador** por una aleatoria de 24 caracteres, que **no se publica** en ningún
sitio del repositorio y se facilita en el **formulario de entrega del TFM**. Se verificó que el acceso con
`Demo1234!` como administrador devuelve `401` y que el usuario empleado sigue operativo con la
credencial conocida. Con ese usuario se puede recorrer toda la aplicación y comprobar el control
de acceso por roles.

Se conserva la credencial pública del usuario **empleado**, cuyo alcance está limitado por el
propio modelo de autorización (no puede ver Empleados, Registros ni Auditoría, y solo accede a sus
propios datos). La pantalla de inicio de sesión **muestra y ofrece acceso directo** a esa
credencial de empleado (es pública y sirve para evaluar sin gestionar altas), y **explica ambos
roles**. De la cuenta de **administrador** solo prerrellena el correo: la contraseña **nunca se
muestra** en el login —se facilita en el formulario de entrega—, precisamente porque la aplicación
es accesible desde Internet. Los factores que hacen aceptable ese riesgo residual son:

- La publicación en Internet es **temporal**, acotada al periodo de evaluación, y se revoca
  eliminando una regla del firewall.
- La base de datos contiene **exclusivamente datos ficticios**: 10 empleados inventados, sin
  ningún dato personal real, por lo que **no hay implicaciones de RGPD**.
- El entorno es de **pruebas**, aislado en sitios y base de datos propios; un compromiso de BinsaRRHH
  no da acceso a las aplicaciones ni a las bases de datos vecinas (el login `hria_app` solo tiene
  permisos dentro de la base `HRIA`).

**Qué habría que hacer antes de un uso real**, y que queda fuera del alcance del MVP:

1. Eliminar los usuarios demo o cambiarles la contraseña, y arrancar con `Demo__Enabled=false`
   (no ejecutar `db/03-seed-demo.sql`). La aplicación ya permite cambiarlas desde la interfaz
   (ver §3-bis), sin tocar la base de datos.
2. Forzar cambio de contraseña en el primer inicio de sesión tras un restablecimiento.
3. Añadir bloqueo de cuenta tras N intentos fallidos y, preferiblemente, MFA para el rol
   administrador.
4. Sustituir el certificado autofirmado por uno de una CA reconocida y publicar solo por HTTPS.
5. **Cambiar el proveedor de IA.** Ver el punto siguiente: es un requisito legal, no una mejora.

Estas limitaciones se documentan como mejoras futuras y son aceptables para el alcance del MVP.

### Obligación legal de registro de jornada: qué cubre BinsaRRHH y qué no

El control horario no es una funcionalidad opcional del producto: el **Real Decreto-ley 8/2019**,
que añadió el artículo 34.9 al Estatuto de los Trabajadores, obliga desde el 12 de mayo de 2019 a
todas las empresas españolas a registrar diariamente la jornada de cada trabajador. Conviene por
tanto ser preciso sobre hasta dónde llega la aplicación.

| Exigencia legal | Estado en BinsaRRHH |
|-----------------|----------------|
| Registro diario con hora de inicio y fin | ✅ Implementado |
| Registro **objetivo y fiable**, no manipulable a posteriori | ✅ No hay edición de jornadas cerradas; toda acción sensible queda auditada |
| Inclusión de los descansos en el cómputo | ✅ Implementado (BR-07) |
| **Conservación durante cuatro años** | ⚠️ Los datos persisten, pero **no hay política de retención explícita** ni purga controlada |
| **Entrega a trabajadores, representantes e Inspección** | ❌ **No implementado**: no existe exportación del registro |
| Acceso del trabajador a su propio registro | ✅ Resumen mensual y consulta de jornadas propias |

**Las dos casillas pendientes son requisitos previos a un uso real**, no mejoras estéticas. Una
empresa que implantase BinsaRRHH tal cual seguiría teniendo que producir el registro a mano ante un
requerimiento de la Inspección, que es justo el trabajo que la aplicación pretende eliminar.

Lo razonable sería:

1. Una **exportación** del registro por empleado y rango de fechas, en un formato legible y
   verificable, con sello de generación.
2. Una **política de retención** configurable, con purga de lo anterior a cuatro años y registro
   de esa purga en auditoría.

Ninguna de las dos entraba en el alcance del TFM. Se documentan aquí para que conste como
decisión y no como descuido.

> Esta sección describe **lo que la norma exige**, no asesora jurídicamente. Antes de una
> implantación real conviene contrastarla con asesoría laboral, entre otras cosas porque la
> regulación del registro horario ha seguido evolucionando.

### El proveedor de IA con datos reales: capa gratuita excluida

En la evaluación del TFM el asistente usa la **capa gratuita** de un proveedor en la nube
(Groq). Es una decisión válida **porque la base de datos contiene únicamente datos ficticios**:
al proveedor no viaja información de ninguna persona real.

Esa validez desaparece en cuanto BinsaRRHH gestione plantilla real. Lo que se envía al proveedor son
nombres de empleados, departamentos y horas trabajadas —datos personales de trabajadores
identificados—, y las capas gratuitas **suelen reservarse el derecho a usar el contenido de las
peticiones para entrenar sus modelos**. Es la contrapartida habitual de no pagar, y con datos de
plantilla no es asumible.

Antes de cualquier puesta en marcha con datos reales hay que adoptar **una** de estas dos vías:

| Vía | Qué exige | Ventaja |
|-----|-----------|---------|
| **Suscripción de pago** | Proveedor que garantice por contrato **no entrenar con los datos del cliente**, **acuerdo de encargado del tratamiento** firmado y, preferiblemente, procesamiento en la UE | Sin cambios de infraestructura |
| **Inferencia local** | Servidor con soporte **AVX2**, memoria suficiente y preferiblemente GPU | **Los datos no salen de la organización**: elimina el problema en origen en vez de contractualizarlo |

La segunda es preferible para un sistema de RR. HH.; la primera es la razonable cuando no hay
hardware que la sostenga. La inferencia local se evaluó sobre el servidor de despliegue actual y
**se descartó por hardware**, con las mediciones recogidas en
[ADR-006](./adr/ADR-006-proveedor-de-ia.md).

> El cambio de proveedor es **solo configuración** (`Ai__Provider`, `OpenAI__BaseUrl`,
> `Claude__ApiKey`…): la arquitectura no lo impide. Lo que exige revisión previa es el
> **cumplimiento**, no el código.
