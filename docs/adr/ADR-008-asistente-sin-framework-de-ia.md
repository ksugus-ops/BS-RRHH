# ADR-008 — Asistente por *tool use* directo, sin framework de IA

- **Estado:** aceptada
- **Fecha:** 2026-07-24
- **Relacionada con:** ADR-004 (abstracción `IAiAssistant` + modo demo), ADR-006 (proveedor de IA)

## Contexto

El asistente de BinsaRRHH responde preguntas de RR. HH. en lenguaje natural consultando datos
mediante herramientas. Para construir asistentes de este tipo existen **frameworks de orquestación
de IA** —**LangChain**, **LlamaIndex**—, especialmente útiles en escenarios **RAG**
(*Retrieval-Augmented Generation*): recuperación de conocimiento desde documentos, con *embeddings*
y bases de datos vectoriales.

Había que decidir si adoptar uno de esos frameworks o implementar el diálogo con el modelo
directamente.

## Decisión

Implementar el bucle de **uso de herramientas (*tool use*) directamente sobre HTTP** contra la API
del proveedor, sin ningún framework de IA. La lógica vive en
[`ClaudeAssistant`](../../backend/src/HRIA.Infrastructure/Ai/ClaudeAssistant.cs) y
[`OpenAiAssistant`](../../backend/src/HRIA.Infrastructure/Ai/OpenAiAssistant.cs), detrás de la
abstracción `IAiAssistant`.

## Por qué

1. **El caso de uso que justifica un framework no se da aquí.** LangChain y LlamaIndex aportan su
   valor en **RAG**: trocear documentos, indexarlos, recuperarlos por similitud semántica.
   BinsaRRHH **no hace RAG**. Sus datos son **estructurados y relacionales** (empleados, jornadas,
   ausencias), y el asistente los consulta con **herramientas cerradas y parametrizadas**, no
   recuperando fragmentos de texto. Añadir un framework de orquestación documental para un problema
   que no es documental sería complejidad sin función.

2. **Control y transparencia.** El bucle propio deja ver exactamente qué se envía al modelo (solo
   las herramientas que el rol permite), qué devuelve y cómo se valida. No hay una capa de
   abstracción opaca entre la aplicación y el proveedor.

3. **Coste de aprendizaje/atadura.** El proyecto ya define su propia abstracción de proveedor
   (`IAiAssistant`, ADR-004), que cumple el objetivo de desacoplamiento que daría un framework, sin
   heredar su modelo mental ni su ciclo de versiones.

## Lo que esta decisión NO exime de cumplir

El framework es opcional; los controles que se exigen para un asistente **no** lo son, y se
implementan igualmente:

- **Guardrails contra *prompt injection***: el modelo no genera SQL ni accede a la base; solo puede
  pedir herramientas autorizadas. Una instrucción maliciosa no tiene superficie donde aterrizar.
- **Transparencia / explicabilidad**: la interfaz muestra **qué herramienta** usó el asistente y en
  qué **modo** (demo / IA) — el equivalente a "mostrar las fuentes".
  
- **Trazabilidad/observabilidad**: cada consulta se audita (pregunta, herramientas, duración,
  estado), sin registrar los datos devueltos.
- **Autorización previa a la conversación**: las herramientas se filtran por rol **antes** de hablar
  con el modelo (ver [`use-cases.md`](../use-cases.md), CU-14).

## Consecuencias

**Positivas**

- Sin dependencia de un framework volátil para un caso que no lo necesita.
- Control total del diálogo y de lo que se expone al modelo.
- El cambio de proveedor es configuración, no reescritura (ADR-004, ADR-006).

**Negativas / a vigilar**

- El proyecto **no ejercita LangChain/LlamaIndex** en código: se conocen como decisión, no como
  implementación. Si el objetivo fuera demostrar RAG, habría que añadir un caso documental real
  (p. ej. consultar el convenio colectivo en lenguaje natural), y ahí un framework y una base
  vectorial sí tendrían sentido.
- El bucle de *tool use* es código propio que hay que mantener y probar — cubierto por
  `ClaudeAssistantTests`.


