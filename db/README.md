# HRIA — Scripts de base de datos (SQL Server)

Scripts para preparar la base de datos en un servidor con SQL Server.
Guía completa paso a paso: [`docs/deployment-sqlserver.md`](../docs/deployment-sqlserver.md).

## Orden de ejecución

| # | Script | Conectado a | Obligatorio |
|---|--------|-------------|:-----------:|
| 1 | `01-create-database.sql` | `master` (admin) | ✅ |
| 2 | `02-schema.sql` | `HRIA` | ✅ * |
| 3 | `03-seed-demo.sql` | `HRIA` | ⬜ opcional |
| 4 | `04-seed-planificacion.sql` | `HRIA` | ⬜ opcional (requiere el 3) |

\* Puedes omitir el paso 2 si dejas que la API aplique las migraciones al arrancar
(requiere que el usuario tenga `db_ddladmin`).

```bash
sqlcmd -S <servidor> -U sa       -P '<pwd>' -i 01-create-database.sql
sqlcmd -S <servidor> -d HRIA -U hria_app -P '<pwd>' -i 02-schema.sql
sqlcmd -S <servidor> -d HRIA -U hria_app -P '<pwd>' -i 03-seed-demo.sql            # opcional
sqlcmd -S <servidor> -d HRIA -U hria_app -P '<pwd>' -i 04-seed-planificacion.sql   # opcional
```

`04-seed-planificacion.sql` añade lo necesario para que los calendarios y el
contraste previsto/real tengan datos suficientes en una demostración:

- Calendario laboral 2026 con los **festivos nacionales** y dos **días de convenio**.
- Cuatro horarios (completa, intensiva, media jornada y turno de tarde), repartidos
  entre los empleados.
- 23 días de vacaciones para toda la plantilla.
- 16 solicitudes de ausencia distribuidas por el año, con estados y tipos mezclados.
- Avatares para los usuarios con acceso.

## Notas

- **`02-schema.sql` se genera desde las migraciones de EF Core** y es idempotente.
  Para regenerarlo tras añadir migraciones:
  ```bash
  cd backend
  dotnet ef migrations script --idempotent -p src/HRIA.Infrastructure -s src/HRIA.Api -o ../db/02-schema.sql
  ```
- **`03-seed-demo.sql`** es idempotente (no hace nada si ya hay usuarios). Si lo usas,
  arranca la API con `Demo__Enabled=false` para evitar una siembra duplicada.
- Las contraseñas demo (`Demo1234!`) usan PBKDF2-HMAC-SHA256 con 100.000 iteraciones.
  El hash incluido está validado por una prueba unitaria
  (`Verify_SeedScriptHash_IsValidForDemoPassword`).
- **No** hay contraseñas reales en estos scripts: los valores a rellenar están marcados
  con `<<< CAMBIAR >>>`.
