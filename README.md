# BinsaRRHH — ERP de Recursos Humanos con asistente de IA

> Trabajo de Fin de Máster · **Máster de Desarrollo con IA**
> ERP de RRHH con control horario de fichajes de los empleados, planificación de las horarios, ausencias y vacaciones. Con un Asistente IA para potenciar el analisis de datos.

> **¿Vienes a evaluar el proyecto?** La [guía de revisión](./docs/guia-de-revision.md) indica
> dónde mirar cada cosa en el código y cómo comprobar que funciona.

---

## a. Descripción general del proyecto

### El punto de partida: registrar la jornada es obligatorio

Desde el **12 de mayo de 2019**, el Real Decreto-ley 8/2019 —que añadió el artículo 34.9 al Estatuto de los Trabajadores— obliga a **todas las empresas españolas** a registrar diariamente
la jornada de cada trabajador, con su hora de inicio y de finalización. Los registros deben **conservarse cuatro años** y estar a disposición de los trabajadores, sus representantes y la
Inspección de Trabajo. No tenerlos es una infracción grave, sancionable por cada centro de trabajo.

No es, por tanto, una herramienta que una empresa adopta si le apetece: es algo que **necesita tener**. La única decisión es cómo lo resuelve.

### Qué problema resuelve

En la pequeña y mediana empresa eso se resuelve casi siempre con una **hoja de cálculo que alguien mantiene a mano**, o con partes en papel. Y ese remedio tiene un coste que no aparece en
ninguna factura: cada mes alguien de Recursos Humanos dedica horas a cuadrar fichajes, sumar descansos, cruzar vacaciones con festivos y perseguir a quien olvidó firmar. Trabajo repetitivo,
fácil de equivocar, y que no aporta nada al negocio.

Cuando además llega la pregunta de verdad —cuántas horas hizo Operaciones en marzo, quién falta la semana que viene—, la respuesta tarda y no siempre es fiable.

**BinsaRRHH convierte ese proceso manual en algo que se sostiene solo.** Registra los fichajes, calcula las horas descontando descansos, compara lo trabajado con el horario asignado, y gestiona ausencias y vacaciones con su flujo de aprobación.

### Qué gana el departamento de Recursos Humanos

| Tarea | Antes | Con BinsaRRHH |
|-------|-------|----------|
| Recoger los fichajes del mes | Perseguir partes y transcribir | Ya están registrados |
| Calcular horas y descansos | A mano, hoja a hoja | Automático |
| Saber quién falta la semana que viene | Revisar correos y notas | Una pantalla |
| Comprobar el cumplimiento de horarios | En la práctica, no se hace | Un gráfico |
| Responder «¿cuántas horas hizo X?» | Buscar y sumar | Preguntarlo en español |
| Estar listo para una inspección | Reconstruir a la carrera | El registro ya existe |

El objetivo no es que el departamento trabaje más rápido, sino que **deje de hacer el trabajo que
no debería estar haciendo**. Las horas que hoy se van cuadrando hojas se recuperan para
planificar plantilla, resolver situaciones y acompañar a las personas: la gestión del personal se
vuelve más ágil porque el dato deja de construirse cada mes y pasa a estar siempre disponible.

> ⚠️ **Alcance respecto a la obligación legal.** BinsaRRHH registra y conserva la jornada, que es el
> núcleo del requisito. Una implantación real necesitaría además una **política explícita de
> conservación a cuatro años** y una **exportación del registro** para entregar a la Inspección o
> a la representación de los trabajadores. Ambas están identificadas y **no implementadas**: ver
> [`security.md` §4](./docs/security.md).

### Por qué lleva un asistente de IA

Aquí está la parte que hace al proyecto distinto de un CRUD, y conviene explicar el razonamiento porque condicionó toda la arquitectura.

Un ERP acumula datos que **están ahí pero nadie encuentra**. La persona que lleva RR. HH. no
sabe en qué pantalla mirar para saber cuántas horas hizo un departamento la semana pasada. La
tentación fácil sería dejar que un modelo de lenguaje consulte la base de datos y responda.

**Ese planteamiento se descartó desde el diseño**, por dos razones:

1. **Un modelo que genera SQL puede leer lo que no debe.** Basta con que alguien le pida los datos de un compañero con la frase adecuada.
2. **Un modelo que redacta libremente puede inventarse una cifra.** Un dato inventado es peor que no tener dato.

La solución adoptada: el modelo **no ve la base de datos**. Recibe un catálogo de herramientas
—consultas cerradas y parametrizadas— filtrado según el rol de quien pregunta. Elige cuál
quiere, y es el backend quien la ejecuta, valida los argumentos y devuelve el resultado.

> Un empleado no obtiene datos de otro no porque el modelo se porte bien, sino porque **esa
> herramienta no se le ofrece**. La autorización se resuelve antes de la conversación.

### Qué demuestra el proyecto

| Aspecto | Cómo se demuestra |
|---------|-------------------|
| Arquitectura | Backend por capas (Clean Architecture) con dependencias hacia dentro; decisiones razonadas en [ADR](./docs/adr/) |
| Seguridad | OWASP Top 10 aplicado, con [tabla de riesgos y evidencias](./docs/security.md) |
| IA controlada | Herramientas autorizadas por rol, sin SQL generado por el modelo |
| Calidad | **182 pruebas** + gate de lint/formato en CI; criterio de *coverage honesto* |
| Operación real | Desplegado en IIS con HTTPS y certificado de CA reconocida |
| Metodología con IA | Desarrollo dirigido por el diseño y uso responsable de la IA — ver [`metodologia.md`](./docs/metodologia.md) |

> **¿Cómo está construido y con qué criterio?** [`metodologia.md`](./docs/metodologia.md) explica el
> paradigma, los principios de diseño (SOLID, Clean Architecture), el enfoque *design-first*, el
> flujo de desarrollo con IA y el uso responsable, y **mapea honestamente** decisiones conscientes, y lo que queda como mejora.

> **Sobre el nombre.** *BinsaRRHH* es el nombre del producto. En el código y la infraestructura se
> conserva el nombre en clave **HRIA** (*Human Resources Intelligence Assistant*): los proyectos
> .NET (`HRIA.Api`, `HRIA.Domain`…), la base de datos `HRIA` y los sitios de IIS. Que el producto y
> su código en clave no coincidan es habitual, y aquí es deliberado: renombrar el producto no
> obliga a redesplegar nada.

---

## b. Stack tecnológico utilizado

| Capa | Tecnologías | Por qué |
|------|-------------|---------|
| **Backend** | ASP.NET Core 8 (LTS), C#, EF Core 8, FluentValidation, xUnit | .NET 8 es **LTS**: soporte hasta 2026 sin sobresaltos. Se descartó .NET 9 por ser STS, y para un TFM que se evalúa meses después la estabilidad importa más que la novedad ([ADR-002](./docs/adr/)) |
| **Frontend** | Vue 3, TypeScript, Vite, Pinia, Vue Router, PrimeVue, Vitest | TypeScript estricto para que los contratos de la API estén tipados. PrimeVue aporta tabla con paginación, diálogos y gráficos ya accesibles: reimplementarlos habría consumido tiempo sin aportar nada evaluable ([ADR-003](./docs/adr/)) |
| **Base de datos** | SQL Server 2022 (producción) · **SQLite** (desarrollo) | El proveedor es configurable. Con SQLite el proyecto **arranca sin instalar nada**, que es justo lo que necesita quien lo evalúa |
| **Autenticación** | JWT Bearer | Sin estado en servidor, adecuado para una SPA. Expiración configurable |
| **Contraseñas** | PBKDF2-HMAC-SHA256, 100.000 iteraciones, sal por contraseña | Irreversible por diseño. Ver [apartado f](#f-usuario-y-contraseña-de-prueba) |
| **IA** | **Groq** (capa gratuita) tras la abstracción `IAiAssistant` | Intercambiable por Claude, OpenAI, OpenRouter u Ollama **cambiando configuración, no código**. Con modo demo funcional sin ninguna clave ([ADR-006](./docs/adr/ADR-006-proveedor-de-ia.md)) |
| **CI** | GitHub Actions | Compilación y pruebas de backend y frontend en cada push |

### Lo que se decidió NO usar

Tan informativo como lo anterior:

- **CQRS y MediatR.** Descartados por escrito. Para este alcance habrían añadido indirección sin
  resolver ningún problema real: servicios de aplicación explícitos e inyectados son más
  legibles y más fáciles de evaluar ([ADR-001](./docs/adr/)).
- **Microservicios.** Dos piezas desplegables ya cubren la separación necesaria. Trocear más
  habría multiplicado la operación sin ganancia.
- **Un ORM ligero o SQL a mano.** EF Core da consultas parametrizadas por defecto, que es la
  mitigación directa de la inyección SQL (A03).

---

## c. Instalación y ejecución

### Requisitos previos

- [.NET SDK 8](https://dotnet.microsoft.com/)
- [Node.js 22+](https://nodejs.org/)

**No hace falta instalar ninguna base de datos.** En desarrollo la aplicación usa SQLite y crea
el fichero `hria.db` con datos de demostración al arrancar. Fue una decisión deliberada: si
evaluar el proyecto exigiera instalar y configurar SQL Server, la barrera de entrada arruinaría
la evaluación.

### Puesta en marcha

Dos terminales, cada una en su carpeta.

**Terminal 1 — Backend**
```bash
cd backend
dotnet run --project src/HRIA.Api --urls http://localhost:5099
```

**Terminal 2 — Frontend**
```bash
cd frontend
npm install
npm run dev
```

Después abre <http://localhost:5173> y entra con las credenciales del [apartado f](#f-usuario-y-contraseña-de-prueba).

| Servicio | URL |
|----------|-----|
| Aplicación | <http://localhost:5173> |
| API + Swagger | <http://localhost:5099/swagger> |
| Estado del servicio | <http://localhost:5099/health> |

> En Windows con PowerShell, si `npm` da error de *execution policy*, usa `cmd /c "npm run dev"`.

### Ejecutar las pruebas

```bash
cd backend && dotnet test     # 173 pruebas
```

```bash
cd frontend && npm run test   # 9 pruebas
```

### Configurar el asistente de IA (opcional)

**Sin configurar nada, el asistente ya funciona en modo demo**: sin modelo de lenguaje, elige la
herramienta por palabras clave y devuelve datos reales de la base. Eso permite evaluar el
proyecto sin dar de alta ninguna cuenta ni gastar un céntimo.

Para activarlo contra un modelo real:

| Variable | Valor |
|----------|-------|
| `Ai__Provider` | `Claude`, `OpenAI` o `Demo` |
| `OpenAI__BaseUrl` | `https://api.groq.com/openai/v1` (o cualquier servicio compatible) |
| `OpenAI__Model` | `llama-3.3-70b-versatile` |
| `OpenAI__ApiKey` | La clave del proveedor |

> ⚠️ **La capa gratuita solo es admisible con datos ficticios.** Con plantilla real, lo que
> viaja al proveedor son nombres y horas de trabajadores identificados, y esas capas suelen
> reservarse el derecho a entrenar con ellos. Antes de un uso real hay dos vías válidas:
> **suscripción de pago** con acuerdo de encargado del tratamiento, o **inferencia local**.
> Razonado en [ADR-006](./docs/adr/ADR-006-proveedor-de-ia.md), con la medición del hardware que
> descartó la opción local en el servidor actual.

### Instalación en un servidor (producción)

Lo anterior es para desarrollo. Para **desplegar en un servidor** —que es como está publicado
este proyecto— el patrón es el mismo que se usó en el despliegue real, sobre **IIS + Windows
Server + SQL Server**. En resumen:

1. **Prerrequisitos en el servidor**: IIS con el módulo *ASP.NET Core*, el **.NET 8 Hosting
   Bundle** (necesario para que IIS ejecute la API), y una instancia de **SQL Server**. En el
   frontend basta servir estáticos, no hace falta Node en el servidor.
2. **Base de datos**: crear la base y el esquema con los scripts de [`db/`](./db/), y un login de
   SQL con **permisos mínimos** limitados a esa base (nunca el usuario administrador).
3. **Backend**: publicar con `dotnet publish -c Release` y copiar el resultado a un **sitio propio
   de IIS**, en un puerto nuevo, aislado del resto.
4. **Frontend**: compilar con `npm run build` apuntando `VITE_API_BASE_URL` a la URL pública de la
   API, y servir el `dist/` como un **segundo sitio** de IIS.
5. **Secretos por variable de entorno**, nunca en el repositorio: la cadena de conexión, el
   secreto JWT y la clave del proveedor de IA se inyectan como variables del sitio en IIS
   (`ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `OpenAI__ApiKey`…).
6. **HTTPS** con certificado de una CA reconocida en los dos sitios, y **CORS** restringido al
   origen del frontend.

> 📄 **Guía completa paso a paso**, con los problemas reales encontrados y cómo se resolvieron
> (WebDAV, el puerto de SQL, el bundle obsoleto): [`docs/deployment-iis.md`](./docs/deployment-iis.md).
> Variante centrada en la base de datos: [`docs/deployment-sqlserver.md`](./docs/deployment-sqlserver.md).
> Por qué el despliegue es **on-premise y sin contenedores**, razonado en
> [ADR-009](./docs/adr/ADR-009-despliegue-sin-contenedores.md).

También puede desplegarse en la **nube** (Render, Railway, un servicio gestionado): el backend es
una API ASP.NET Core estándar y el frontend son estáticos. No se hizo por decisión consciente
—soberanía del dato de RR. HH. e infraestructura Windows ya disponible—, no por impedimento
técnico.

---

## d. Estructura del proyecto

```
BS-RRHH/
├─ backend/                      # Solución .NET
│  ├─ src/
│  │  ├─ HRIA.Domain/            # Entidades, enums y reglas de negocio puras
│  │  ├─ HRIA.Application/       # Casos de uso, DTOs, validadores, interfaces
│  │  ├─ HRIA.Infrastructure/    # EF Core, JWT, hashing, clientes de IA, seeding
│  │  └─ HRIA.Api/               # Controladores, middleware, DI, Swagger, /health
│  └─ tests/HRIA.Tests/          # 173 pruebas xUnit
├─ frontend/
│  └─ src/
│     ├─ features/               # Una carpeta por funcionalidad
│     ├─ shared/                 # Cliente HTTP, componentes y utilidades comunes
│     ├─ stores/                 # Estado con Pinia
│     └─ router/                 # Rutas y guards
├─ db/                           # Scripts SQL de esquema y datos de demostración
├─ docs/                         # Documentación del proyecto
│  ├─ adr/                       # Registros de decisión arquitectónica
│  ├─ conventions/               # Convenciones de código por área
│  └─ img/                       # Capturas
├─ .github/workflows/            # Integración continua
└─ AGENTS.md                     # Guía de contribución al código
```

### Por qué está organizado así

**El backend se organiza por capas, con las dependencias apuntando hacia dentro.**

```
Api ──→ Application ──→ Domain
         ↑
    Infrastructure
```

La consecuencia práctica: **el dominio no sabe que existe Entity Framework**. Las reglas del
control horario se pueden probar sin base de datos, sin servidor y sin contenedor, y por eso las
173 pruebas tardan nueve segundos. Si mañana se cambiara EF Core por otra cosa, `HRIA.Domain` no
se enteraría.

**El frontend se organiza por funcionalidad, no por tipo de fichero.** Todo lo de ausencias
—vista, tipos, llamadas a la API— vive en `features/absences/`. La alternativa habitual
(`components/`, `views/`, `services/`) obliga a saltar entre tres carpetas para entender una
sola pantalla.

**Los scripts SQL están versionados** en `db/`, y el *seeding* es idempotente: se puede ejecutar
dos veces sin duplicar datos.

---

## e. Funcionalidades principales

### Autenticación y control de acceso

Login con JWT y dos roles, **Administrador** y **Empleado**.

La regla que atraviesa todo el proyecto: el frontend oculta lo que no corresponde por comodidad,
pero **la autorización real se valida siempre en el backend**. Se puede comprobar en vivo:
entrando como empleado y llamando directamente a `/api/employees`, la respuesta es `403` aunque
el enlace no aparezca en el menú.

Existe además **protección horizontal**: el identificador del empleado en las operaciones propias
se deriva del token, nunca de lo que envía el cliente. Un empleado no puede leer ni escribir
datos de otro aunque manipule la petición.

### Control horario

Fichaje de entrada, inicio y fin de descanso, y salida, con **ocho reglas de negocio** que
impiden secuencias imposibles: no se puede salir con un descanso abierto, no puede haber dos
jornadas abiertas a la vez, no se puede iniciar un descanso sin haber fichado.

Esas reglas **viven en el dominio, no en el botón**. El botón deshabilitado es la cortesía
visual; la regla se cumple aunque la petición llegue desde fuera de la interfaz. La de «dos
jornadas abiertas» está además **en la base de datos**, con un índice único filtrado: no depende
de que el código se acuerde.

### Horarios y desviación

Plantillas de horario con tramos por día de la semana, asignables a cada empleado con fecha de
vigencia. Con eso, cada jornada muestra la **desviación** entre lo trabajado y lo previsto.

Detalle que costó un error y merece la pena contar: **los tramos se guardan en hora local del
centro de trabajo, no en UTC**. Todo lo demás del sistema es UTC, pero un horario de 08:00 debe
seguir siendo las 08:00 en enero y en julio; almacenarlo en UTC lo desplazaría con el cambio de
estación. La excepción está razonada en [`architecture.md` §8](./docs/architecture.md).

### Ausencias, vacaciones y calendario laboral

Solicitud por parte del empleado, aprobación o denegación por parte de administración, saldo
anual de días y **calendario anual con toda la plantilla** en una rejilla de doce meses.

El **calendario laboral** permite marcar los festivos de convenio y los fines de semana. Un día
marcado ahí deja de contar como hábil **en todos los cálculos**: en el resumen mensual del
empleado, en el cómputo de días de vacaciones y en la previsión de ausencias.

### Dashboard

Para administración: indicadores del día, **cuatro gráficos** —estado de plantilla, horas
trabajadas por día, reparto mensual entre trabajo y ausencias, y puntualidad frente al horario
asignado— y la previsión de ausencias a dos semanas vista.

Los colores están **validados para daltonismo**, y ninguna serie se identifica solo por el
color: todas llevan etiqueta. No es adorno; un gráfico que un 8 % de los hombres no puede leer
está mal hecho.

Para el empleado: resumen de sus fichajes del mes, **contando solo días laborables**.

### Asistente de IA

Se abre desde un **botón flotante disponible en cualquier pantalla**, y la conversación
sobrevive a la navegación. No tiene ruta propia: se retiró para que hubiera una única puerta.

Cinco herramientas, repartidas según el rol:

| Herramienta | Quién puede usarla |
|-------------|--------------------|
| `get_current_working_employees` | Solo administración |
| `get_open_time_entries` | Solo administración |
| `get_incomplete_workdays` | Solo administración |
| `get_department_hours_summary` | Solo administración |
| `get_employee_hours_summary` | Ambos — el empleado queda forzado a su propio identificador |

Cada consulta queda **auditada**: pregunta, herramientas usadas, duración y estado. Nunca el
contenido de los datos devueltos.

> **¿Qué se le puede preguntar, y qué cambia según el rol?** La guía de usuario
> [`asistente-guia.md`](./docs/asistente-guia.md) lo explica con ejemplos: qué sabe responder, qué
> puede pedir un empleado frente a un administrador, y por qué no se le puede sacar un dato que no
> le corresponde.

### Gestión de contraseñas

Cualquier usuario cambia la suya aportando la actual. Administración puede restablecer la de un
empleado **sin llegar a conocerla**: la nueva se muestra una única vez.

### Auditoría

Registro de acciones sensibles —login, altas y bajas de empleados, consultas al asistente—
consultable solo por administración, y **sin datos sensibles**: hay una prueba que verifica que
una contraseña no aparece en el registro.

---

## f. Usuario y contraseña de prueba

### Para probar la aplicación desplegada

| Rol | Usuario | Contraseña |
|-----|---------|-----------|
| **Empleado** | `empleado@hria.local` | `Demo1234!` |
| Administrador | `admin@hria.local` | **no publicada** — se facilita en el formulario de entrega |

Con la cuenta de empleado se recorre toda la aplicación y **se comprueba que el control de acceso
funciona**: Empleados, Registros y Auditoría devuelven `403`, no solo se ocultan del menú.

### Al ejecutar el proyecto en local

Ambos usuarios tienen la contraseña `Demo1234!`, creados por el *seeding*.

### Por qué la del administrador no se publica aquí

La aplicación **está accesible desde Internet**. Publicar en un repositorio público una
credencial con permisos de administración de un entorno accesible sería dejar la puerta abierta.
Tras el despliegue se cambió por una aleatoria de 24 caracteres que no está en ningún fichero del
repositorio.

La del empleado sí se publica, deliberadamente: su alcance está acotado por el propio modelo de
autorización, y permite evaluar el proyecto sin gestionar altas. Los datos son **ficticios** —10
empleados inventados— así que no hay ninguna persona real detrás.

### Por qué las contraseñas no se pueden consultar

Se guardan con **PBKDF2-HMAC-SHA256**, 100.000 iteraciones y sal por contraseña. Es
**irreversible**: no hay pantalla ni endpoint que las muestre, tampoco a administración.

Durante el desarrollo se planteó que el administrador pudiera verlas. No es posible sin
guardarlas en claro o cifradas de forma reversible, y eso convierte una fuga de hashes en una
fuga de credenciales utilizables —agravada porque las personas reutilizan contraseñas—. La
necesidad operativa real, desbloquear a quien la ha olvidado, se cubre restableciéndola sin
leerla. Razonado en [`security.md` §3-bis](./docs/security.md).

> Antes de cualquier uso real hay que eliminar ambos usuarios demo y arrancar con
> `Demo__Enabled=false`. Análisis completo del riesgo en [`security.md` §4](./docs/security.md).

---

## Despliegue

La aplicación está **desplegada y accesible**. La **URL de acceso no se publica en este
repositorio** porque el despliegue está sobre la **infraestructura real de la empresa (Binsa)**, no
en un servidor público del proyecto; se facilita en el **formulario de entrega**.

Desplegado en **IIS sobre Windows Server 2022**, en dos sitios independientes: uno sirve la SPA de
Vue (estáticos) y otro la API de ASP.NET Core. Base de datos `HRIA` en SQL Server 2022, con login
propio y permisos limitados a esa base: un compromiso de la aplicación no da acceso a las bases
vecinas del servidor. HTTPS con certificado de CA reconocida en ambos sitios, sin avisos del
navegador.

Ningún secreto vive en el repositorio: la cadena de conexión, el secreto JWT y la clave del
proveedor de IA están en variables de entorno del sitio en IIS.

Guías completas: [`deployment-iis.md`](./docs/deployment-iis.md) ·
[`deployment-sqlserver.md`](./docs/deployment-sqlserver.md)

---

## Seguridad

Autenticación JWT, autorización por roles validada en backend, protección horizontal, consultas
parametrizadas, CORS restrictivo, limitación de peticiones en login y asistente, manejo global de
excepciones y logs sin datos sensibles.

El análisis completo —OWASP Top 10 aplicado al alcance, tabla de riesgos con evidencias y
**limitaciones asumidas conscientemente**— está en [`security.md`](./docs/security.md).

---

## Documentación

| Documento | Contenido |
|-----------|-----------|
| [`guia-de-revision.md`](./docs/guia-de-revision.md) | **Por dónde empezar**: qué mirar en el código y cómo comprobarlo |
| [`metodologia.md`](./docs/metodologia.md) | **Paradigma, principios de diseño, flujo con IA ** |
| [`requirements.md`](./docs/requirements.md) | Requisitos funcionales, no funcionales y reglas de negocio |
| [`use-cases.md`](./docs/use-cases.md) | Casos de uso por actor, con flujos principales y alternativos |
| [`asistente-guia.md`](./docs/asistente-guia.md) | **Qué se le puede pedir al asistente y qué cambia según el rol** |
| [`user-stories.md`](./docs/user-stories.md) | Historias de usuario con criterios de aceptación |
| [`architecture.md`](./docs/architecture.md) | Arquitectura, diagramas C4 y flujos |
| [`data-model.md`](./docs/data-model.md) | Modelo de datos |
| [`api-design.md`](./docs/api-design.md) | Diseño de la API |
| [`security.md`](./docs/security.md) | OWASP aplicado, riesgos y limitaciones |
| [`testing.md`](./docs/testing.md) | Estrategia y mapa de pruebas |
| [`adr/`](./docs/adr/) | Decisiones arquitectónicas razonadas |
| [`deployment-iis.md`](./docs/deployment-iis.md) | Despliegue en IIS, con los problemas reales encontrados |

---

## Entrega

> **Qué se facilita en el formulario de entrega, no en este repositorio.** La aplicación está
> desplegada sobre la **infraestructura real de la empresa (Binsa)**, no en un servidor público del
> proyecto. Por eso **no se publican aquí** la **URL de acceso**, la **contraseña de administrador**,
> las **diapositivas** ni el **vídeo** —este último recorre la aplicación desplegada—. Todo ello se
> entrega a través del **formulario**.

---

## Autor

**falvarez@binsa.com** — Máster de Desarrollo con IA.
