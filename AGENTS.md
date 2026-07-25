# BinsaRRHH — Guía de contribución al código

Convenciones del proyecto. Están escritas para que las siga cualquiera que trabaje
en el repositorio: una persona, o un asistente de programación de cualquier
fabricante. `AGENTS.md` es un nombre de fichero abierto que varias herramientas
cargan automáticamente; su contenido es markdown normal, sin formato propietario.

## Qué es BinsaRRHH

ERP de recursos humanos con control horario y asistente de consulta en lenguaje
natural. Backend ASP.NET Core 8 por capas, frontend Vue 3 + TypeScript, SQL Server
en producción y SQLite en desarrollo. Descripción completa en el
[README](README.md) y documentación de diseño en [`docs/`](docs/).

## Convenciones por área

Antes de tocar código, lee la que corresponda:

| Área | Documento |
|------|-----------|
| Backend .NET | [`docs/conventions/backend.md`](docs/conventions/backend.md) |
| Frontend Vue | [`docs/conventions/frontend.md`](docs/conventions/frontend.md) |
| Asistente de IA | [`docs/conventions/asistente-ia.md`](docs/conventions/asistente-ia.md) |
| Seguridad | [`docs/conventions/seguridad.md`](docs/conventions/seguridad.md) |
| Pruebas | [`docs/conventions/pruebas.md`](docs/conventions/pruebas.md) |

## Reglas transversales

- **Fechas en UTC** en la base de datos y la API; la conversión a la zona del
  usuario ocurre solo en presentación.
- **Ningún secreto en el repositorio.** Claves y cadenas de conexión viven en
  variables de entorno; hay plantillas de ejemplo en `docs/examples/`.
- **Autorización en el backend.** El frontend oculta lo que no corresponde por
  comodidad, pero la comprobación real está siempre en el servidor.
- **La documentación se actualiza con el código**, en el mismo cambio.

## Antes de dar por cerrado un cambio

```bash
cd backend && dotnet build && dotnet test
```

```bash
cd frontend && npm run build && npm run test
```

Ambas en verde, y comprobación funcional en el navegador si el cambio es visible.
No se afirma que algo funciona sin haberlo ejecutado.
