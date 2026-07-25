# ADR-009 — Despliegue directo en IIS, sin contenedores ni orquestación

- **Estado:** aceptada
- **Fecha:** 2026-07-24
- **Relacionada con:** ADR-007 (monolito modular), [`metodologia.md`](../metodologia.md) §8

## Contexto

BinsaRRHH **no usa contenedores**: se despliega directamente en **IIS sobre Windows Server**, con la
API como aplicación de ASP.NET Core y el frontend como estáticos. Este registro razona esa decisión.

## Qué problemas resuelven los contenedores, y si BinsaRRHH los tiene

La decisión se toma comprobando, una por una, si el proyecto tiene el problema que cada ventaja resuelve.

| Ventaja del contenedor | ¿BinsaRRHH tiene ese problema? |
|------------------------------------------|-------------------------------|
| **Consistencia dev → prod** ("funciona en mi máquina" eliminado: mismo comportamiento en local y en el clúster) | **Parcialmente mitigado sin contenedores.** El proyecto arranca en desarrollo con **SQLite** (sin instalar ninguna base de datos) y se publica con `dotnet publish`, que empaqueta el runtime. La reproducibilidad no es la de una imagen, pero el "funciona en mi máquina" está muy acotado. |
| **Portabilidad entre nubes** (mover cargas entre proveedores sin reescribir) | **No aplica.** El despliegue es **on-premise deliberado**, en la infraestructura Windows/IIS/SQL Server que la empresa ya tiene, por soberanía del dato de RR. HH. No hay requisito de portar entre nubes. |
| **Escalado elástico** (arrancar instancias en segundos según demanda) | **No aplica.** Es un ERP de RR. HH. de una pyme: carga pequeña, fija y predecible. No hay picos tipo *Black Friday* que justifiquen autoescalado. |
| **Resiliencia por orquestación** (Kubernetes reprograma réplicas caídas) | **No aplica al alcance.** Una sola instancia sobre IIS cubre la disponibilidad que el caso necesita; un orquestador para una réplica es maquinaria sin carga. |

De las cuatro razones habituales por las que se recomienda el uso de contenedores, **tres no aplican**
a este proyecto y la cuarta está razonablemente cubierta por otros medios.

## Decisión

Desplegar **directamente en IIS**, sin Docker ni Kubernetes. La razón de fondo: contenerizar
resolvería problemas que BinsaRRHH no tiene, a cambio de introducir **superficie operativa nueva**
—un motor de contenedores y, para la orquestación, un clúster— **sobre un Windows Server que ya
opera IIS con otras doce aplicaciones**. Añadir esa capa sin un problema que la exija sería el mismo
sobre-diseño que el proyecto evita en su arquitectura (ADR-001, ADR-007).

Es una decisión de **proporción**, no de desconocimiento: el flujo `Dockerfile → imagen → registro`
es directo para una app .NET (imagen base oficial, contenedor Linux), y técnicamente el proyecto se
podría contenerizar en poco tiempo. No se hace porque no hay driver.

## Coste asumido, dicho con honestidad

No contenerizar tiene un precio real, y conviene declararlo:

- **El despliegue es manual y está atado a un servidor.** Se documenta paso a paso en
  [`deployment-iis.md`](../deployment-iis.md), pero no es "descargar una imagen y ejecutar". Mover
  la aplicación a otra máquina exige repetir esa configuración.
- **La reproducibilidad no es la de una imagen inmutable.** Depende de que el servidor destino tenga
  el *Hosting Bundle* correcto y la configuración de IIS descrita.

Es la misma familia de limitación que la **ausencia de Infraestructura como Código** (ver
`metodologia.md` §8): la infraestructura se configura a mano, no se declara. Ambas se asumen
conscientemente para el alcance de un TFM desplegado en un servidor único y estable.

## Cuándo habría que revisar esta decisión

Contenerizar dejaría de ser sobre-diseño y pasaría a estar justificado si el proyecto:

1. **Migrara a la nube** (Render, Railway, un servicio gestionado de Kubernetes): ahí el contenedor
   es el formato de entrega natural y enlaza con un despliegue cloud.
2. **Necesitara varios entornos reproducibles** más allá de un servidor (integración, preproducción,
   producción idénticos).
3. **Requiriera escalado horizontal** por crecimiento de carga o de número de clientes.

En ese momento, el primer paso sería un `Dockerfile` para la API y otro para servir el frontend estático.

## Consecuencias

**Positivas**

- Infraestructura proporcional al problema; sin motor de contenedores ni clúster que operar.
- Aprovecha la infraestructura Windows/IIS que la empresa ya mantiene.
- La decisión es explícita y reversible: hay un camino claro para contenerizar el día que aporte.

**Negativas / a vigilar**

- El proyecto **no ejercita Docker/Kubernetes en la práctica**: se dominan como decisión razonada,
  no como implementación. Si el objetivo fuera demostrar containerización en ejecución, habría que
  añadir los `Dockerfile` y un `docker-compose` para el conjunto API + base de datos.
- Despliegue manual y atado al servidor, como se detalla arriba.

