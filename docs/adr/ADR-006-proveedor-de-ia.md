# ADR-006 — Proveedor de IA: capa gratuita en demostración, de pago o local en producción real

- **Estado:** aceptada
- **Fecha:** 2026-07-23
- **Relacionada con:** ADR-004 (abstracción `IAiAssistant` + modo demo)

## Contexto

El asistente de BinsaRRHH necesita un modelo de lenguaje con **uso de herramientas**. La
abstracción `IAiAssistant` (ADR-004) ya permite cambiar de proveedor sin tocar la lógica de
aplicación, pero quedaba por decidir **cuál usar en cada entorno**, y esa decisión no es
solo técnica ni solo económica: al proveedor le viajan **nombres de empleados,
departamentos y horas trabajadas**, es decir, datos personales de la plantilla.

Las opciones evaluadas fueron cuatro:

1. **Modo demo** — sin LLM: selección de herramienta por palabras clave.
2. **Capa gratuita de un proveedor en la nube** (Groq, Gemini, OpenRouter, GitHub Models).
3. **Suscripción de pago** (Anthropic, OpenAI, Azure OpenAI).
4. **Inferencia local** con Ollama en el propio servidor.

## Evaluación de la inferencia local

La opción local es la única en la que **los datos de personal no salen de la organización**,
lo que la hace especialmente atractiva para un ERP de RR. HH. Se descartó **para el servidor
de despliegue actual** (BINSAWEBTEST) por restricciones de hardware medidas, no estimadas:

| Recurso | Medido | Necesario (modelo 7-8B cuantizado a 4 bits) |
|---------|--------|---------------------------------------------|
| CPU | 2× Intel Xeon **E5520** (Nehalem, 2009) | — |
| Instrucciones vectoriales | `SSE4.2 ✓` · **`AVX ✗` · `AVX2 ✗` · `FMA ✗`** | AVX2 en los binarios habituales |
| RAM | 4 GB totales, **1,2 GB libres** | 5-6 GB |
| GPU | Ninguna | Opcional, pero determinante |
| Disco libre | 12 GB | 2-5 GB por modelo |

El bloqueo determinante es la **ausencia de AVX**: Ollama distribuye binarios de llama.cpp
compilados asumiendo esa extensión, de modo que el proceso no arranca — no es una cuestión de
lentitud. Compilar solo con SSE daría un rendimiento incompatible con una demostración en
directo. Añadido a eso, la memoria libre no admite ni el modelo más pequeño, y el servidor
comparte esos 4 GB con SQL Server y 15 sitios de IIS: reservar memoria para inferencia
degradaría el resto de aplicaciones.

## Decisión

**Se selecciona el proveedor según la naturaleza de los datos, no según el entorno técnico.**

| Escenario | Proveedor | Motivo |
|-----------|-----------|--------|
| Desarrollo y evaluación del TFM, **datos ficticios** | **Groq** (capa gratuita, API compatible con OpenAI) | Coste nulo y sin cambios de código: el cliente existente ya habla ese formato |
| Sin conectividad o sin clave configurada | **Modo demo** | Degradación elegante: la aplicación sigue respondiendo |
| **Producción con datos reales de plantilla** | **Suscripción de pago** con acuerdo de tratamiento de datos, **o inferencia local** en hardware adecuado | Ver la sección siguiente |

Configuración efectiva del despliegue de evaluación:

```
Ai__Provider    = OpenAI
OpenAI__BaseUrl = https://api.groq.com/openai/v1
OpenAI__Model   = llama-3.3-70b-versatile
OpenAI__ApiKey  = gsk_…  (variable de entorno del sitio en IIS, no versionada)
```

No hay cliente específico de Groq: se reutiliza el de OpenAI cambiando `BaseUrl`, porque Groq
expone el mismo contrato `/chat/completions`. Cambiar de proveedor es, por tanto, **configuración
y no código**.

## Producción real: la capa gratuita queda excluida

Esta es la parte que condiciona cualquier puesta en marcha con personal real de la empresa.

Las capas gratuitas de los proveedores en la nube **suelen reservarse el derecho a usar el
contenido de las peticiones para entrenar sus modelos**. Es la contrapartida habitual de no
pagar. Mientras BinsaRRHH opera con datos sembrados (`db/03-seed-demo.sql`), eso es irrelevante:
no hay ninguna persona real detrás de "Eva Empleada".

En el momento en que BinsaRRHH gestione plantilla real, esa contrapartida deja de ser aceptable.
Los datos que viajan al proveedor son datos personales de trabajadores identificados, y su
tratamiento por un tercero exige base jurídica, encargo de tratamiento e información a los
interesados. Por tanto, para producción real solo hay dos caminos válidos:

1. **Suscripción de pago** con un proveedor que ofrezca, por contrato, **no entrenar con los
   datos del cliente** y que permita firmar el correspondiente **acuerdo de encargado del
   tratamiento**; conviene además elegir región de procesamiento en la UE cuando esté
   disponible.
2. **Inferencia local** (Ollama o equivalente) en un servidor con soporte de AVX2, memoria
   suficiente y preferiblemente GPU. **Los datos no salen de la organización**, lo que elimina
   el problema en origen en lugar de contractualizarlo.

La opción 2 es la preferible desde el punto de vista de protección de datos; la 1 es la
razonable cuando no hay hardware que la sostenga. Lo que **no** es admisible con datos reales
es la capa gratuita.

## Consecuencias

**Positivas**

- Coste cero durante el desarrollo y la defensa del TFM.
- Cambiar de proveedor es configuración, no código: `Ai__Provider`, `OpenAI__BaseUrl`,
  `OpenAI__Model` y la clave correspondiente.
- El modo demo mantiene la aplicación utilizable sin conectividad ni clave, lo que permite
  defender el proyecto sin depender de la red del aula.
- La migración a local no requiere rediseño: es una implementación más de `IAiAssistant`.

**Negativas / a vigilar**

- La capa gratuita impone límites de peticiones y **no ofrece garantías de disponibilidad**;
  no sirve para un servicio con compromiso de nivel.
- Los identificadores de modelo de las capas gratuitas **se retiran con frecuencia**: conviene
  verificar el modelo configurado antes de una demostración.
- Queda una **deuda explícita**: pasar a producción real obliga a revisar esta decisión. No es
  un cambio de configuración sin más, sino un requisito legal previo a la puesta en marcha.

## Verificación de la decisión

El soporte de instrucciones vectoriales del servidor se comprobó ejecutando código, no por
las especificaciones del fabricante:

```csharp
Console.WriteLine("AVX  : " + System.Runtime.Intrinsics.X86.Avx.IsSupported);   // False
Console.WriteLine("AVX2 : " + System.Runtime.Intrinsics.X86.Avx2.IsSupported);  // False
```
