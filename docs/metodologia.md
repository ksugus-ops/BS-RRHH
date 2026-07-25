# BinsaRRHH — Metodología, paradigma y principios de diseño

Este documento explica **cómo** está construido el proyecto y **con qué criterio**, contrastando
cada decisión con las **buenas prácticas de ingeniería de software** habituales. La regla que lo
gobierna: decir lo que hay, decir lo que no hay, y razonar cada decisión. Enumerar solo aciertos no
serviría para juzgar nada.

---

## 1. Paradigma de la aplicación

BinsaRRHH es una **aplicación web cliente-servidor** con **programación orientada a objetos** como
paradigma principal en el backend, sobre una **arquitectura monolítica modular por capas**.

- **Orientación a objetos**: entidades de dominio con comportamiento, servicios de aplicación,
  interfaces (puertos) e inyección de dependencias. Es el paradigma habitual en aplicaciones de
  dominio de negocio; la inversión de dependencias admite también un estilo funcional, pero aquí se
  usa el orientado a objetos, que es el idiomático en .NET.
- **Monolito modular**: un único backend desplegable con fronteras internas por módulo. La
  elección frente a microservicios está razonada en
  [ADR-007](./adr/ADR-007-monolito-modular-y-comunicacion-entre-servicios.md).
- **Reactivo/declarativo en el frontend**: Vue 3 con composición y estado reactivo (Pinia). No es
  el paradigma del backend, sino el propio de una SPA moderna.

---

## 2. Principios de diseño (SOLID y afines)

El proyecto no persigue SOLID como etiqueta, pero sus decisiones lo materializan:

| Principio | Cómo aparece en el código |
|-----------|---------------------------|
| **Responsabilidad única** | Servicios de aplicación por caso de uso (`TimeTrackingService`, `AbsenceService`…); controladores finos que solo validan y delegan. |
| **Abierto/cerrado** | El proveedor de IA se extiende (Claude, OpenAI, demo) sin tocar el consumidor, gracias a `IAiAssistant`. |
| **Sustitución de Liskov** | Cualquier implementación de `IAiAssistant` o `IPasswordHasher` es intercambiable sin que el llamador lo note. |
| **Segregación de interfaces** | 15 interfaces pequeñas y específicas en la capa Application (`ICurrentUser`, `IJwtTokenGenerator`, `IExpectedMinutesCalculator`…), no una interfaz gigante. |
| **Inversión de dependencias** | El dominio y la aplicación dependen de **abstracciones**; las implementaciones concretas (EF Core, JWT, hashing, IA) viven en Infrastructure y se inyectan. |

**Matiz honesto sobre la inyección de dependencias.** Una práctica recomendada en Clean Architecture
es preferir la **inversión manual o mediante factorías**, evitando los "contenedores de inyección
pesados" y el anti-patrón *Service Locator*. BinsaRRHH usa el **contenedor nativo de .NET**, pero de
la forma que respeta ese principio: registro **explícito** en un único punto de composición
([`DependencyInjection.cs`](../backend/src/HRIA.Infrastructure/DependencyInjection.cs)), sin
resolución dinámica tipo Service Locator, y con configuración tipada. No es la inversión manual pura,
pero mantiene lo que esta persigue: un composition root único y visible.

---

## 3. Clean Architecture

El backend sigue *Clean Architecture* con la nomenclatura habitual de capas (convergente con el
patrón *Hexagonal / Ports & Adapters*):

```
        Api ─────► Application ─────► Domain
                        ▲
                  Infrastructure
        (las dependencias apuntan hacia el dominio)
```

| Capa (Clean Architecture) | Capa en BinsaRRHH | Contenido |
|---------------------------|-------------------|-----------|
| Dominio ("núcleo inviolable") | `HRIA.Domain` | Entidades, enums, reglas de negocio. Sin EF Core ni ASP.NET. |
| Aplicación ("casos de uso", "puertos") | `HRIA.Application` | Servicios de caso de uso, DTOs, **interfaces (puertos)**, validadores. |
| Infraestructura ("wiring") | `HRIA.Infrastructure` | EF Core, JWT, hashing, clientes de IA, seeding. **Composition root.** |
| — | `HRIA.Api` | Controladores finos, middleware, DI. |

Lo que Clean Architecture busca y **está presente**:

- **Dependencias hacia adentro**: el dominio no conoce EF Core. Verificable: las 173 pruebas de
  backend corren sin base de datos.
- **Puertos = interfaces**: la aplicación define interfaces que la infraestructura implementa.
- **DTOs por caso de uso**, no entidades expuestas.
- **Composition root único** en Infraestructura.
- **Sin anemia total de dominio**: entidades como [`Workday`](../backend/src/HRIA.Domain/Entities/Workday.cs)
  contienen comportamiento (`WorkedDuration`, `TotalBreakDuration`, `HasOpenBreak`), no son meras
  bolsas de datos.

Lo que el DDD táctico busca y **NO está**, dicho con honestidad:

- **No hay Value Objects ni Aggregate Roots** en el sentido táctico de DDD. Las entidades usan
  tipos primitivos y setters públicos (modelo pragmático, cómodo para EF Core), no objetos de
  valor inmutables ni raíces de agregado que emitan eventos de dominio. Es DDD **ligero**, no
  estricto — una decisión de proporción para el alcance, no un olvido.
- **El frontend no aplica Clean Architecture.** La literatura de Clean Architecture trata el
  backend/dominio; no cubre la arquitectura de frontend. La SPA se organiza **por funcionalidad**
  (`features/`), con cliente HTTP centralizado y alias de rutas (`@/*`) —una práctica recomendada
  en proyectos TypeScript—. Es una separación de intereses razonable, pero mapearla como "Clean
  Architecture" sería forzado, y no se hace.

### ¿Clean Architecture o Hexagonal?

Son dos estilos distintos pero convergentes. BinsaRRHH usa el **vocabulario de capas** de Clean
Architecture; el principio de fondo —puertos y adaptadores, dominio agnóstico— es común a ambos. La
abstracción del proveedor de IA (`IAiAssistant` como puerto, `ClaudeAssistant` como adaptador) es un
ejemplo literal de *puertos y adaptadores*.

---

## 4. Enfoque dirigido por el diseño (design-first)

El proyecto se construyó **especificando antes de programar**, en fases: la primera fue análisis
y diseño —requisitos, historias de usuario, casos de uso, modelo de datos, ADRs— **antes** de
escribir código de negocio.

Es importante ser preciso con el término: esto es **desarrollo dirigido por el diseño
(design-first)**, no *Spec-Driven Development* en su acepción estricta (especificaciones ejecutables
que generan el código). No hay generación automática desde una especificación formal. Lo que sí hay,
y es lo que el enfoque persigue, es que la documentación **precede y guía** a la implementación y se
mantiene con ella:

| Artefacto de especificación | Precede a |
|-----------------------------|-----------|
| [`requirements.md`](./requirements.md) (RF, RNF, reglas de negocio) | Toda la implementación |
| [`user-stories.md`](./user-stories.md) (criterios de aceptación) | Las pruebas y la UI |
| [`use-cases.md`](./use-cases.md) (flujos principales y alternativos) | Los servicios de aplicación |
| [`data-model.md`](./data-model.md) | Las entidades y migraciones |
| [`adr/`](./adr/) (decisiones razonadas) | Cada elección estructural |

La trazabilidad es explícita: cada requisito se enlaza con su historia, su caso de uso y su prueba.

---

## 5. El asistente de IA: sin framework, y por qué

Los frameworks de orquestación de IA (**LangChain**, **LlamaIndex**) se usan habitualmente para
construir asistentes, sobre todo en escenarios **RAG** (*Retrieval-Augmented Generation*):
recuperación de conocimiento documental con *embeddings* y bases de datos vectoriales.

BinsaRRHH **no usa ningún framework de IA**. Implementa el bucle de *tool use* directamente sobre
HTTP contra la API del proveedor. Es una decisión consciente, razonada en
[ADR-008](./adr/ADR-008-asistente-sin-framework-de-ia.md):

- Un framework de orquestación aporta su valor en **RAG/documental**. BinsaRRHH no hace RAG: sus
  datos son **estructurados y relacionales**, y el asistente los consulta con herramientas cerradas,
  no recuperando documentos. El caso de uso que justifica el framework no se da aquí.
- Estos frameworks tienen fama de **volatilidad e inestabilidad** entre versiones. Prescindir de esa
  dependencia, para cinco herramientas bien acotadas, reduce la superficie de fallo.

Lo que un asistente de IA debe cumplir **con independencia del framework**, y BinsaRRHH sí cumple:

- **Guardrails contra *prompt injection***: el modelo no genera SQL, solo elige entre herramientas
  autorizadas; una inyección no tiene superficie donde aterrizar.
- **Transparencia mostrando el origen**: la interfaz muestra **qué herramienta** usó el asistente y
  en qué **modo** (demo / IA) — el equivalente a "mostrar las fuentes", una buena práctica de UX
  conversacional.
- **Trazabilidad**: cada consulta queda auditada (pregunta, herramientas, duración, estado).

---

## 6. Desarrollo potenciado por IA y uso responsable

El proyecto se construyó usando IA como **herramienta de desarrollo**, y de la forma que se
considera un uso responsable. No es lo mismo que "generado por IA": la autoría, las decisiones y la
validación son humanas; la IA amplía la capacidad de entrega.

### Herramientas usadas

| Herramienta | Tipo | Uso en el proyecto |
|-------------|------|--------------------|
| **Claude** (Anthropic) | Asistente de IA con contexto de proyecto | Diseño, generación asistida de código y documentación, revisión |
| **Visual Studio Code** | IDE | Editor principal de desarrollo |

### Método de trabajo

- **Desarrollador aumentado**: la IA no sustituye el criterio; lo amplifica. La responsabilidad
  legal y de calidad sigue siendo humana.
- **Método sándwich**: el humano diseña la estructura, la IA rellena los componentes, el humano
  audita la calidad. Es el flujo real de cada fase de este proyecto.
- **Planificar antes de ejecutar**: investigar y planificar —sin tocar ficheros— antes de actuar, y
  confirmar antes de las acciones que cambian el sistema. Es el patrón con el que se ha trabajado de
  forma sistemática, visible en el historial de decisiones.
- **Bloques pequeños y validación**: cambios acotados, verificados con compilación y pruebas antes
  de darlos por buenos. Una suite de tests que pasa puede ocultar fallos estructurales; la
  validación humana es el filtro final.
- **Gobernanza de datos / no fuga de información**: el proyecto opera con **datos ficticios**; el
  análisis de qué exigiría tratar datos reales con un proveedor de IA está en
  [ADR-006](./adr/ADR-006-proveedor-de-ia.md).

### Fallos que solo aparecieron con validación humana

Un riesgo conocido de la IA generativa es la **plausibilidad frente a veracidad**: produce
resultados que *parecen* correctos. Varios defectos de este proyecto lo confirman, y se encontraron
**probando contra el despliegue real**, no confiando en el código generado: WebDAV bloqueando
peticiones en IIS, el asistente resolviendo «esta semana» contra una fecha de su corpus de
entrenamiento, el autorrelleno del navegador ensuciando un buscador, y celdas de una rejilla
desplazándose. Cada uno se corrigió y, donde importaba, quedó documentado en el propio código con
un comentario que explica el porqué.

### Revisión de código con IA (CodeRabbit): evaluado, no integrado

Existen **agentes especializados en la revisión de calidad de código** que analizan el repositorio.
**CodeRabbit** es uno: un bot que revisa *pull requests* con IA y deja comentarios y resúmenes
automáticos.

**No se ha integrado**, y la razón es de flujo, no de rechazo: el desarrollo se llevó con
**push directo a `main`**, sin *pull requests*, que es el gancho que CodeRabbit necesita. Integrarlo
habría exigido cambiar a un flujo de PR a mitad de un proyecto de una sola persona. Queda como
**mejora directa**: activar la app de CodeRabbit en el repositorio y abrir PRs haría que cada cambio
pasara por una revisión con IA antes de integrarse, complementando el gate de lint/formato del CI.

---

## 7. Calidad

Criterios de calidad aplicados:

**Lo que se cumple:**

- **Coverage honesto (100/80/0)**: es el criterio declarado del proyecto — cubrir al 100 % la lógica
  crítica (reglas de negocio, autorización, hashing) antes que perseguir un porcentaje global.
  Detalle en [`testing.md`](./testing.md).
- **Pirámide de pruebas**: base ancha de unitarias e integración (173 backend + 9 frontend =
  **182**), coherente con "más unitarias, menos E2E".
- **Gate de calidad en CI**: el pipeline ejecuta **lint** (ESLint en el frontend, `dotnet format
  --verify` en el backend), compilación y pruebas. Bloquea la integración si algo falla.

**Lo que NO se cumple del todo, dicho claro:**

- **No hay hooks de pre-commit con Husky.** Una práctica recomendada es llevar el gate a local
  (*shift-left*) con hooks de pre-commit. Aquí el gate vive **solo en CI**. Es una desviación de esa
  técnica, no un incumplimiento: un gate equivalente puede vivir en la revisión de PR / CI. En un
  proyecto **mixto .NET + Vue**, un gate único en el pipeline evita duplicar la configuración de
  hooks por stack.
- **No hay gate de umbral de cobertura numérico** (p. ej. "bloquear si <80 %"). El criterio de
  coverage honesto se aplica por juicio, no por un umbral automático. Es la mejora de calidad más
  directa que queda.
- **No hay pruebas E2E automatizadas** (Playwright). La verificación de extremo a extremo se hizo
  manualmente contra el despliegue. Recogido como mejora futura en [`testing.md`](./testing.md).

---

## 8. Infraestructura y Cloud

**La decisión: on-premise, en IIS, sin contenedores.** BinsaRRHH se despliega en **IIS sobre Windows
Server**, no en Render/Railway/Kubernetes ni en contenedores. Es una **desviación de plataforma
consciente**, no una carencia: la nube no es obligatoria, es una opción por agilidad y escalado.
Para un ERP de RR. HH. la **soberanía del dato on-premise** es una ventaja defendible, y la
infraestructura Windows/IIS/SQL Server es la real disponible.

| Concepto | Estado en BinsaRRHH |
|----------|---------------------|
| Cloud (Render/Railway/Cloud Run) | ❌ No — on-premise deliberado |
| **Contenedores (Docker) y orquestación (Kubernetes)** | ❌ No — despliegue directo en IIS. Razonado en [ADR-009](./adr/ADR-009-despliegue-sin-contenedores.md). |
| **Infraestructura como Código** (Terraform) | ❌ No — configuración manual de IIS. **Carencia real**, aunque con caso de uso débil en un único servidor estable. Mejora futura. |
| **Gestión de secretos** | ✅ Sí — secretos inyectados por el **host (IIS)**, fuera del repositorio. Es el mismo patrón estándar, con IIS en el rol de Railway/Vercel. |
| Segregación por entorno | ✅ Sí — `Development` / `Production` con configuración distinta; SQLite en desarrollo. |
| CI/CD | ✅ Sí — GitHub Actions (build + lint + test). |

### Por qué sin contenedores

La containerización se presenta a menudo como "el lenguaje universal de la infraestructura moderna",
pero es una herramienta, no un requisito. La decisión se tomó comprobando si el proyecto tiene los
problemas que un contenedor resuelve, y **tres de los cuatro no aplican**:

- **Consistencia dev → prod**: mitigada sin contenedores — SQLite en desarrollo (sin instalar base
  de datos) y `dotnet publish` que empaqueta el runtime.
- **Portabilidad entre nubes**: no aplica — el despliegue es on-premise deliberado.
- **Escalado elástico**: no aplica — carga pequeña, fija y predecible de una pyme; sin picos.
- **Resiliencia por orquestación**: no aplica al alcance — una instancia sobre IIS cubre la
  disponibilidad necesaria; un clúster para una réplica es maquinaria sin carga.

Contenerizar añadiría un motor de contenedores (y, para orquestar, un clúster) **sobre un Windows
Server que ya opera IIS con otras doce aplicaciones**, sin un problema que lo exija. El coste
asumido —despliegue manual y atado a un servidor— se declara abiertamente. El razonamiento completo,
incluido **cuándo habría que revisar la decisión** (migración a la nube, varios entornos, escalado),
está en [ADR-009](./adr/ADR-009-despliegue-sin-contenedores.md).

**Lo importante**: los principios de infraestructura *core* —secretos fuera del código, `.gitignore`
para `.env`, plantilla `.env.example`, menor privilegio del usuario de BD, segregación de entornos—
**son independientes de la nube y de los contenedores, y se cumplen** en el despliegue on-premise.

---

## 9. Seguridad

Es el eje donde el proyecto está **a la altura sin desviación**, y con independencia de dónde se
despliegue. Aplicando *Security by Design*, *Security by Default* y el *OWASP Top 10*:

- **Security by Design**: seguridad desde el diseño, no como parche — SQL parametrizado (EF Core),
  hashing fuerte con sal (PBKDF2), autorización por rol y protección horizontal validadas en el
  servidor.
- **Security by Default**: configuración segura por defecto — HTTPS con certificado de CA (no
  autofirmado, no HTTP en claro), Swagger solo en desarrollo, sin credenciales de administración
  publicadas en el despliegue.
- **Gestión de secretos**: fuera del repositorio, inyectados por el host, con `.env.example` como
  plantilla y usuario de SQL Server de permisos mínimos.

El análisis completo —OWASP Top 10 aplicado, tabla de 14 riesgos con evidencias y limitaciones
asumidas— está en [`security.md`](./security.md).
