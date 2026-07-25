# BinsaRRHH — Asistente de IA seguro

> **Cuándo aplicar:** Patrón para implementar el asistente de IA seguro de BinsaRRHH (function calling con OpenAI tras una abstracción de proveedor, herramientas autorizadas por rol, validación y saneamiento de argumentos, consultas parametrizadas, resultados limitados, modo demo sin API key y auditoría). Úsala en el módulo de IA.

Aplica este patrón al construir o modificar el asistente de RRHH (solo lectura).

## Principios
1. **El modelo NUNCA accede a la base de datos.** Solo recibe la definición de herramientas y decide cuál llamar con qué argumentos.
2. **Abstracción de proveedor** (`IAiAssistant`): implementaciones `OpenAiAssistant` y `DemoAssistant`. Se selecciona `DemoAssistant` si no hay API key (RF-AI-09).
3. **Herramientas autorizadas por rol:** antes de llamar al modelo, filtra el catálogo de herramientas según el rol del usuario y envía **solo** las permitidas.
4. **Solo lectura:** ninguna herramienta crea, modifica o borra datos.

## Catálogo de herramientas
`get_current_working_employees`, `get_open_time_entries`, `get_incomplete_workdays` → **Admin**.
`get_employee_hours_summary` → Admin (cualquier empleado) / Employee (forzado a su propio `employeeId`).
`get_department_hours_summary` → **Admin**.

## Pipeline de cada consulta
1. Recibir pregunta + identidad + rol.
2. Sanear la pregunta (longitud máx., quitar contenido de control).
3. Seleccionar herramientas autorizadas por rol.
4. Llamar al proveedor (o modo demo) con esas herramientas.
5. Validar los argumentos de la `tool_call` (whitelist de valores, rangos de fecha, límites).
6. Para rol Employee, **forzar** `employeeId = usuario actual` (protección horizontal); ignorar cualquier id recibido.
7. Ejecutar la consulta **parametrizada** con resultados limitados (top-N, sin campos sensibles).
8. Devolver los resultados al modelo para redactar la respuesta final.
9. Registrar en `AiQueryLog`: pregunta, herramientas usadas, estado (`Success/Denied/ProviderError/Demo`), duración.

## Defensa ante prompt injection
- Las instrucciones del usuario no pueden ampliar permisos: la autorización se decide en el backend, no según lo que diga el texto.
- Si la herramienta solicitada no está autorizada para el rol → responder denegado, registrar `Denied`, no ejecutar nada.
- Nunca construir SQL a partir del texto del usuario.

## Rate limiting
`/ai/ask` limitado por usuario. Manejar fallo del proveedor con error controlado (`ProviderError`) sin exponer detalles internos.

## Tests obligatorios
Consulta autorizada, no autorizada, parámetros inválidos, fallo del proveedor, ausencia de API key (modo demo), pregunta ambigua, intento de prompt injection, intento de acceder a datos de otro empleado.
