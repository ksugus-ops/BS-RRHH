# BinsaRRHH — Estrategia y mapa de pruebas

Enfoque de **coverage honesto**: se prioriza cubrir las reglas críticas de negocio, la
autorización y la seguridad, sobre el porcentaje total. Un test debe poder fallar por la
razón correcta.

## 1. Resumen

| Suite | Framework | Nº pruebas | Estado |
|-------|-----------|:---------:|:------:|
| Backend | xUnit + FluentAssertions | **173** | ✅ verde |
| Frontend | Vitest | **9** | ✅ verde |

Ejecución:
```bash
cd backend && dotnet test        # 173 pruebas
cd frontend && npm run test      # 9 pruebas
```

## 2. Backend (xUnit)

### Unitarias — reglas de dominio y servicios
| Área | Pruebas | Reglas cubiertas |
|------|---------|------------------|
| Hash de contraseñas | `Pbkdf2PasswordHasherTests` | Hash verificable, contraseña incorrecta, sal aleatoria, formato inválido |
| JWT | `JwtTokenGeneratorTests` | Claims correctos, token caducado, firma inválida |
| Autenticación | `AuthServiceTests` | Login correcto, contraseña incorrecta, usuario inactivo, empleado inactivo, email case-insensitive, usuario actual |
| Empleados | `EmployeeServiceTests` | Alta válida, correo duplicado (409), departamento inexistente (400), edición, baja lógica, protección horizontal (403), filtros y paginación |
| Control horario | `WorkdayCalculationTests` | BR-07 (total = entrada−salida−descansos), descansos múltiples, jornada/descanso abierto, no negativo |
| Control horario | `TimeTrackingServiceTests` | BR-01..BR-06 (secuencias inválidas), flujo completo, estados, BR-08 (incompleta), protección horizontal en histórico |
| Dashboard | `DashboardServiceTests` | Indicadores, jornada obsoleta como incompleta, serie continua, regresión de horas infladas |
| Asistente IA | `AiToolRegistryTests` | Herramientas por rol, protección horizontal, empleado inexistente, empleados trabajando |
| Asistente IA | `AiAssistantServiceTests` | Modo demo, consulta autorizada, pregunta ambigua, prompt injection, fallo de proveedor, auditoría |
| Asistente IA | `ClaudeAssistantTests` | Bucle de herramientas contra un `HttpMessageHandler` de prueba: emparejado `tool_use`→`tool_result`, rechazo de herramienta no autorizada, prompt de sistema fuera de los mensajes, corte del bucle |
| Asistente IA | `AiPromptTests` | La fecha de hoy viaja en el prompt; se exige declarar el periodo; restricciones de seguridad intactas |
| Horarios | `ScheduleServiceTests` | Alta y edición de plantillas, tramos inválidos, asignación con vigencia, autorización |
| Horarios | `ExpectedMinutesCalculatorTests` | Minutos previstos según el horario vigente; sin horario asignado no hay previsión |
| Puntualidad | `PunctualityTests` | Dentro y fuera de horario con tolerancia; **entrar antes de hora no es un retraso** (regresión de la resta de `TimeOnly`) |
| Ausencias | `AbsenceServiceTests` | Solicitud, resolución, estados, saldo anual, solapamientos (BR-12), protección horizontal |
| Calendario laboral | `WorkCalendarServiceTests` | Marcado de festivos, fines de semana en bloque, año completo, autorización |
| Días hábiles | `WorkingDayCalculatorTests` | Cómputo excluyendo días no laborables del calendario (BR-13) |
| Contraseñas | `PasswordChangeTests` | Cambio con la actual, longitud mínima, distinta de la anterior, restablecimiento por admin, empleado recibe 403, **la contraseña no aparece en auditoría** |

### Integración — pipeline HTTP (WebApplicationFactory + BD en memoria)
| Área | Pruebas |
|------|---------|
| Auth | `/health`, login demo admin/empleado, contraseña incorrecta (401), `/auth/me` con y sin token |
| Empleados | Empleado no puede listar (403), admin lista (200), admin crea (201) |
| Auditoría | Empleado 403, admin 200, sin token 401 |

> Las pruebas de integración verifican el pipeline real (JWT + middleware + controladores +
> seeding) **sin depender de un servidor de base de datos**, usando la base en memoria.

## 3. Frontend (Vitest)

| Área | Pruebas |
|------|---------|
| Cliente HTTP | `client.test.ts` — baseURL y cabeceras por defecto |
| Store de auth | `auth.test.ts` — estado inicial, login (persistencia de token), logout |
| Utilidades | `format.test.ts` — formato de minutos a "Xh Ym" |

## 4. Pruebas de las reglas críticas del control horario (BR)

| Regla | Test |
|-------|------|
| BR-01 No doble entrada | `CheckIn_Twice_ThrowsConflict_BR01` |
| BR-02 No descanso sin jornada | `StartBreak_WithoutWorkday_ThrowsConflict_BR02` |
| BR-03 No doble descanso | `StartBreak_Twice_ThrowsConflict_BR03` |
| BR-04 No fin de descanso inexistente | `EndBreak_WithoutOpenBreak_ThrowsConflict_BR04` |
| BR-05 No salida con descanso abierto | `CheckOut_WithOpenBreak_ThrowsConflict_BR05` |
| BR-06 No salida sin jornada | `CheckOut_WithoutWorkday_ThrowsConflict_BR06` |
| BR-07 Cálculo de horas | `WorkedDuration_SubtractsBreaks_BR07` |
| BR-08 Jornada incompleta | `GetStatus_MarksStaleOpenWorkdayAsIncomplete_BR08` |

## 5. Pruebas de seguridad del asistente de IA

| Escenario | Test |
|-----------|------|
| Consulta autorizada | `Ask_AuthorizedQuestion_UsesTool` |
| Consulta no autorizada | `BuildTools_Employee_OnlyExposesOwnHoursSummary` |
| Parámetros inválidos | `EmployeeHoursSummary_InvalidEmployee_ReturnsControlledMessage` |
| Fallo del proveedor | `Ask_ProviderFailure_ReturnsProviderError_AndLogs` |
| Ausencia de API key (modo demo) | `Ask_NoApiKey_UsesDemoMode_AndLogs` |
| Pregunta ambigua | `Ask_AmbiguousQuestion_ReturnsControlledAnswer_NoTool` |
| Prompt injection | `Ask_PromptInjection_AsEmployee_DoesNotExposeAdminData` |
| Acceso a datos de otro empleado | `EmployeeHoursSummary_AsEmployee_IgnoresRequestedOtherEmployeeId` |

## 6. Verificación manual (navegador)

Durante el desarrollo se verificó end-to-end en navegador, **contra el despliegue real**, no solo
en local:
- Login con roles y redirección por guards.
- Gestión de empleados: listado, búsqueda server-side, alta/edición/detalle/baja.
- Control horario: transiciones de estado, cálculo de horas (8h), histórico.
- Horarios: asignación y desviación mostrada en las jornadas.
- Ausencias: solicitud, aprobación y calendario anual de plantilla.
- Calendario laboral: marcado de festivos y fines de semana.
- Dashboard: indicadores, los cuatro gráficos y previsión de ausencias con datos reales.
- Asistente de IA: ventana flotante, permisos por rol, modo demo y modo live.
- Contraseñas: cambio propio, y comprobación de que no aparece en la auditoría.

> **Esta verificación encontró defectos que las pruebas unitarias no podían encontrar**, porque
> dependían del entorno o de datos reales: WebDAV interceptando `PUT`/`DELETE` en IIS, un bundle
> obsoleto tras cambiar de puerto, el asistente resolviendo «esta semana» contra una fecha de su
> corpus de entrenamiento, y el autorrelleno del navegador escribiendo el correo en el buscador
> de empleados. Ninguno era detectable sin ejecutar la aplicación desplegada.

## 7. Accesibilidad y responsive (revisión)

- Formularios con `label` asociados a inputs y `aria-label` en controles clave.
- Navegación con estado activo; foco visible (estilos por defecto de PrimeVue).
- Layouts responsive (grids `auto-fit`, apilado en móvil, tablas con scroll horizontal).
- Estados de carga, error y vacío en todas las vistas que cargan datos.

## 8. Mejoras futuras de testing

- Pruebas E2E automatizadas (Playwright) del flujo completo en navegador.
- Pruebas de componente Vue (Vitest + @vue/test-utils) para el panel de fichaje, los formularios
  y la ventana flotante del asistente.
- El proveedor Claude ya se cubre con un `HttpMessageHandler` simulado (`ClaudeAssistantTests`);
  falta dar el mismo trato al cliente compatible con OpenAI.
- Pruebas de contraste y daltonismo automatizadas sobre la paleta de los gráficos, hoy validadas
  con una herramienta externa en el momento de elegirla.
