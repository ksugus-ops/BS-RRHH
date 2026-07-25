# BinsaRRHH — Testing

> **Cuándo aplicar:** Convenciones de testing de BinsaRRHH (xUnit en backend, Vitest en frontend), qué reglas críticas cubrir con prioridad y enfoque de coverage honesto. Úsala al escribir o revisar pruebas.

Aplica estas convenciones al escribir pruebas.

## Enfoque
- **Coverage honesto:** priorizar las reglas críticas sobre el porcentaje total. Un test debe poder fallar por la razón correcta.
- Nombres descriptivos: `Metodo_Escenario_ResultadoEsperado` (back) / `describe/it` claros (front).
- Arrange-Act-Assert. Datos de prueba mínimos y explícitos.

## Backend (xUnit) — cobertura obligatoria
1. **Control horario (BR-01..BR-08):**
   - Doble entrada rechazada; descanso sin jornada rechazado; doble descanso rechazado; fin de descanso inexistente rechazado; salida con descanso abierto rechazada; salida sin jornada rechazada.
   - Cálculo de total trabajado = (salida − entrada) − descansos.
   - Marcado de jornada incompleta.
2. **Autenticación:** login correcto, password incorrecta, usuario inactivo, token caducado.
3. **Autorización:** acceso permitido/denegado por rol; protección horizontal (empleado no accede a datos de otro → 403).
4. **Empleados:** alta válida, correo duplicado (409), datos inválidos (400), edición, baja lógica.
5. **Asistente IA:** herramienta autorizada vs no autorizada, argumentos inválidos, fallo de proveedor, ausencia de API key (modo demo), prompt injection, acceso a datos de otro empleado.

## Frontend (Vitest)
- Store de auth (login, logout, persistencia de token, expiración).
- Utilidades de fecha/horas (UTC ↔ local, formato de duración).
- Guards de router.
- Componentes críticos: panel de fichaje (botones según estado), formulario de empleado (validación).

## Ejecución
- Back: `dotnet test`. Front: `npm run test`.
- Ambos deben estar en verde para cerrar una fase. No dar por buena una prueba sin ejecutarla.
