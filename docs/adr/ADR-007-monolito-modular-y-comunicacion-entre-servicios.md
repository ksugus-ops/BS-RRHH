# ADR-007 — Monolito modular, comunicación entre servicios y dónde entraría Outbox

- **Estado:** aceptada
- **Fecha:** 2026-07-24
- **Relacionada con:** ADR-001 (capas pragmáticas, sin CQRS/MediatR)

## Contexto

Este registro deja por escrito qué estilo
de arquitectura usa BinsaRRHH, por qué, qué comunicación entre servicios tiene realmente, y en qué
condiciones se introduciría lo que hoy no está.

La pregunta no es «¿por qué no es distribuido?» sino «¿el problema que resuelve el proyecto
justifica un sistema distribuido?». Y la respuesta honesta es que no.

## Decisión

BinsaRRHH es un **monolito modular**: un único servicio backend desplegable (`HRIA.Api`),
organizado internamente en módulos con fronteras claras (autenticación, empleados, control
horario, horarios, ausencias, calendario, dashboard, asistente, auditoría) sobre una arquitectura
por capas con las dependencias apuntando al dominio.

No se adopta una arquitectura de microservicios ni dirigida por eventos.

## Por qué un monolito modular y no microservicios

Los microservicios resuelven problemas de **organización y escala** —equipos independientes desplegando por separado,
partes del sistema con necesidades de escalado muy distintas— a cambio de un coste operativo alto:
red entre servicios, consistencia eventual, observabilidad distribuida, despliegue coordinado.

BinsaRRHH no tiene ninguno de esos problemas:

| Motivo típico para trocear | ¿Aplica aquí? |
|----------------------------|---------------|
| Varios equipos trabajando en paralelo | No: un autor |
| Módulos con escalado muy dispar | No: todo escala igual, y el volumen es el de una pyme |
| Tecnologías distintas por servicio | No: todo .NET |
| Despliegue independiente de partes | No aporta: se despliega como una unidad |

Trocear este sistema multiplicaría la operación —varios procesos, comunicación por red, fallos
parciales, coordinación de despliegue— **sin resolver ningún problema real**. 

El monolito modular conserva la ventaja que se busca —**fronteras internas limpias**— sin pagar el
coste de la distribución. Si algún día un módulo necesitara separarse, las fronteras por capas y
por funcionalidad ya están, y la extracción sería acotada.

## Qué comunicación entre servicios SÍ existe

Aunque el sistema no sea distribuido, tiene un punto de **integración con un servicio externo**, y
está resuelto con criterio:

- El backend llama al **proveedor de IA** (Groq / Claude / OpenAI) por **HTTP síncrono**.
- La llamada está detrás de la abstracción `IAiAssistant` (ADR-004): el resto de la aplicación no
  sabe con qué proveedor habla, ni siquiera si hay proveedor —existe un modo demo sin red—.
- Es una **lectura en tiempo de petición**, no una escritura ni un evento: el usuario pregunta,
  se consulta al modelo, se responde. No hay estado que sincronizar entre sistemas.
- Es la única salida a la red, con URL de configuración fija (mitigación de SSRF, A10 en
  `security.md`).

Esto es comunicación **síncrona y de solo lectura** con un tercero. Es el patrón adecuado para el
caso: no hay nada que ganar haciéndolo asíncrono, porque el usuario espera la respuesta en
pantalla.

## Dónde entraría el patrón Outbox (y por qué hoy no)

El **patrón Outbox** resuelve el problema del *dual-write*: cuando una operación debe, de forma
atómica, **guardar en la base de datos propia y publicar un mensaje a otro sistema**. Escribir en
dos sitios sin transacción común abre la puerta a que uno tenga éxito y el otro no —el dato se
guarda pero la notificación se pierde, o al revés—. Outbox lo evita escribiendo el evento en una
tabla `outbox` **dentro de la misma transacción** que el dato de negocio; un proceso aparte lee esa
tabla y publica al destino, garantizando que el mensaje sale si y solo si la transacción se
confirmó.

**BinsaRRHH no tiene hoy ese problema.** Las escrituras son locales a su base de datos, incluida la
auditoría, que ocurre en la misma transacción que la acción auditada. No se publica ningún evento a
ningún sistema externo. Introducir Outbox ahora sería infraestructura sin mensaje que transportar:
una tabla, un publicador en segundo plano y un consumidor, todo para no mover nada. Sería el
sobre-diseño que ADR-001 se compromete a evitar.

### El caso que sí lo justificaría

Hay una funcionalidad **pendiente y con base real** que introduciría el problema del *dual-write* y,
con él, la necesidad de Outbox: **notificar a un sistema externo cuando se aprueba una ausencia**
—un servicio de correo, o el futuro módulo de nóminas— o, en la misma línea, la **exportación del
registro horario** para la Inspección de Trabajo (ver la limitación legal en `security.md` §4).

En ese escenario, aprobar una ausencia tendría que hacer dos cosas a la vez: marcarla como
aprobada en la base y avisar al sistema externo. Ahí Outbox es la respuesta correcta:

```
Aprobar ausencia
   └─ UNA transacción:  UPDATE AbsenceRequest SET status = Approved
                        INSERT INTO Outbox (evento = "AbsenceApproved", payload)
   └─ Proceso publicador (en segundo plano):
        lee Outbox → publica el evento al broker / servicio → marca la fila como enviada
   └─ Garantía: si la aprobación se guardó, la notificación acabará saliendo;
                si la transacción falló, no se envía nada.
```

Eso convertiría al sistema en **parcialmente dirigido por eventos** e introduciría comunicación
**asíncrona** entre servicios, esta vez con justificación real. Es el momento —y no antes— en que
los conceptos del módulo de arquitecturas distribuidas dejan de ser teoría y pasan a resolver un
problema del producto.

## Consecuencias

**Positivas**

- La arquitectura es proporcional al problema: sin coste operativo de distribución que nadie
  necesita.
- Las fronteras modulares dejan la puerta abierta a extraer un servicio el día que haga falta.
- La decisión está razonada, no asumida: se demuestra conocer los microservicios, el
  event-driven y el Outbox, **y saber cuándo no aplicarlos**, que es la competencia de fondo.

**Negativas / a vigilar**

- El proyecto **no ejercita en código** la comunicación asíncrona entre servicios propios ni el
  Outbox: se dominan como decisión de diseño, no como implementación. Si el objetivo fuera
  demostrarlos en ejecución, habría que añadir la funcionalidad de notificación descrita arriba.
- Queda una deuda explícita ligada a un requisito legal: la exportación del registro y la
  notificación de ausencias, que son también el punto de entrada natural de Outbox y del
  event-driven.

