# BinsaRRHH — Convenciones de frontend

> **Cuándo aplicar:** Convenciones del frontend de BinsaRRHH (Vue 3 + TypeScript + Vite, organización por funcionalidades, Pinia, Vue Router con guards, cliente HTTP con interceptores, PrimeVue, accesibilidad y estados de UI). Úsala al crear o modificar el frontend.

Aplica estas reglas al trabajar en la SPA de BinsaRRHH.

## Organización (por funcionalidad)
`src/features/{auth,employees,time-tracking,dashboard,ai-assistant}` + `src/shared` (cliente HTTP, interceptores, componentes UI reutilizables, utilidades de fecha) + `src/stores` (Pinia) + `src/router`.

## Reglas
- **TypeScript estricto.** Tipar respuestas de API con interfaces en cada feature.
- **Cliente HTTP centralizado** (`shared/http`): interceptor de request añade `Authorization: Bearer`; interceptor de response gestiona `401` (logout + redirect a login) y normaliza errores a un formato común.
- **Pinia** para estado (auth/sesión, y estado por feature cuando aporte). El token y el usuario viven en el store de auth; persistir el token de forma coherente.
- **Vue Router guards:** `requiresAuth` y `requiresAdmin`. El frontend oculta/deshabilita por UX, pero **la seguridad real está en el backend**.
- **Estados obligatorios** en cada vista que carga datos: cargando, error (con reintento) y vacío. Mensajes claros de éxito/error (toasts de PrimeVue).
- **Fechas:** el backend envía UTC; convertir a la zona del navegador al mostrar; enviar UTC al backend.
- **PrimeVue** para componentes: `DataTable` (paginación/filtros) para empleados y jornadas, `Dialog` para formularios/confirmaciones, `Toast`, `Chart` para el dashboard.
- **Accesibilidad básica:** labels asociados a inputs, foco visible, roles ARIA en componentes propios, contraste suficiente, navegación por teclado en acciones clave.
- **Responsive:** layout adaptable (grid/flex), tablas con scroll en móvil.

## Testing (Vitest)
- Cubre stores (auth), utilidades de fecha/horas, guards y componentes críticos (panel de fichaje, formulario de empleado).

## Verificación de cierre
`npm run build` y `npm run test` en verde. Comprobar la vista en el navegador antes de dar por terminada una fase visual.
