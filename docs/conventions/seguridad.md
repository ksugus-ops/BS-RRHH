# BinsaRRHH — Seguridad (OWASP aplicado)

> **Cuándo aplicar:** Checklist de seguridad de BinsaRRHH basado en OWASP Top 10 aplicado al alcance (autenticación/JWT, autorización por roles y protección horizontal, validación y saneamiento, CORS, rate limiting, gestión de secretos, logs sin PII, manejo global de errores). Úsala en revisiones de seguridad y al implementar features sensibles.

Verifica estos controles al implementar o revisar código sensible.

## Autenticación y sesión
- Contraseñas con hash fuerte + sal (PBKDF2/BCrypt). Nunca texto plano ni comparación no constante.
- JWT firmado con secreto fuerte (de configuración/entorno, no en el repo), expiración configurable.
- Usuario inactivo no puede autenticarse. Mensajes de error genéricos (no revelar si falla email o password).
- Rate limiting en `/auth/login`.

## Autorización (A01 Broken Access Control)
- Toda ruta protegida exige JWT; roles validados en backend con políticas.
- **Protección horizontal:** el `employeeId` de operaciones propias se deriva del token; un empleado no puede leer/escribir datos de otro (tests que lo prueben con 403).
- El asistente de IA aplica los mismos límites (ver skill `hria-ai-tools`).

## Inyección (A03) y validación
- EF Core con consultas parametrizadas; nunca SQL concatenado.
- Validar todos los DTOs de entrada (FluentValidation): tipos, rangos, longitudes, formato email.
- Sanear la entrada hacia la IA (longitud, caracteres de control).

## Configuración segura (A05)
- CORS restrictivo configurable por entorno (no `*` en producción).
- Swagger solo en desarrollo.
- Cabeceras de seguridad razonables.
- Manejo global de excepciones; en producción ocultar detalles internos y stack traces.

## Secretos (A02/A05)
- Sin secretos en el repositorio. `.gitignore` cubre `appsettings.*.local.json`, `.env`, etc.
- Proveer `appsettings.Example.json` / `.env.example` con placeholders.
- Escanear el repo en busca de secretos antes de publicar.

## Logging y monitorización (A09)
- Logs estructurados **sin** contraseñas, tokens ni PII sensible.
- Auditoría de acciones sensibles y consultas de IA.

## Dependencias (A06)
- Fijar versiones; revisar vulnerabilidades conocidas de paquetes.

## Entregable de la fase de seguridad
Tabla en `docs/security.md`: Riesgo · Nivel · Mitigación aplicada · Evidencia · Riesgo residual.
