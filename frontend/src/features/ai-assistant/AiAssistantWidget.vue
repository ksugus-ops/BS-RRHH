<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import AiChat from './AiChat.vue'

const open = ref(false)
const everOpened = ref(false)
const chatRef = ref<InstanceType<typeof AiChat> | null>(null)
const launcherRef = ref<HTMLButtonElement | null>(null)

function toggle() {
  open.value = !open.value
  if (open.value) everOpened.value = true
}

/** Al cerrar se devuelve el foco al icono, para no perderlo al navegar con teclado. */
function close() {
  open.value = false
  launcherRef.value?.focus()
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && open.value) close()
}

watch(open, (isOpen) => {
  if (isOpen) chatRef.value?.focusInput()
})

// La conversación se conserva mientras dure la sesión: el componente sigue
// montado aunque el panel esté cerrado, así no se pierde el hilo al cerrarlo.
onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <div class="ai-widget">
    <transition name="panel">
      <section
        v-show="open"
        class="panel"
        role="dialog"
        aria-label="Asistente de RR. HH."
        :aria-hidden="!open"
      >
        <header class="panel-head">
          <span class="title">
            <i class="pi pi-sparkles" aria-hidden="true" />
            Asistente de RR. HH.
          </span>
          <button type="button" class="icon-btn" aria-label="Cerrar el asistente" @click="close">
            <i class="pi pi-times" aria-hidden="true" />
          </button>
        </header>

        <p class="panel-sub">Solo lectura, limitado a los datos que tienes autorizados.</p>

        <div class="panel-body">
          <AiChat ref="chatRef" compact />
        </div>
      </section>
    </transition>

    <button
      ref="launcherRef"
      type="button"
      class="launcher"
      :class="{ open, idle: !open && !everOpened }"
      :aria-expanded="open"
      :aria-label="open ? 'Cerrar el asistente' : 'Abrir el asistente de RR. HH.'"
      @click="toggle"
    >
      <!-- Anillos de atención: solo hasta que se abre por primera vez. -->
      <span class="halo" aria-hidden="true"></span>
      <i :class="open ? 'pi pi-chevron-down' : 'pi pi-sparkles'" aria-hidden="true" />
      <span class="hint" aria-hidden="true">Pregúntame lo que quieras</span>
    </button>
  </div>
</template>

<style scoped>
.ai-widget {
  position: fixed;
  right: 1.25rem;
  bottom: 1.25rem;
  z-index: 1100;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.75rem;
}

.launcher {
  position: relative;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  background: linear-gradient(140deg, #0d9973 0%, #16b98a 45%, #4bdcac 100%);
  color: #fff;
  font-size: 1.85rem;
  display: grid;
  place-items: center;
  box-shadow: 0 8px 26px rgba(13, 153, 115, 0.45), 0 0 0 6px rgba(22, 185, 138, 0.12);
  transition: transform 0.18s ease, box-shadow 0.18s ease;
}
.launcher > i { position: relative; z-index: 1; }
.launcher:hover {
  transform: translateY(-3px) scale(1.05);
  box-shadow: 0 14px 34px rgba(13, 153, 115, 0.55), 0 0 0 10px rgba(22, 185, 138, 0.16);
}
.launcher:active { transform: translateY(-1px) scale(1.02); }
.launcher:focus-visible { outline: 3px solid var(--hria-accent-700); outline-offset: 4px; }
.launcher.open {
  width: 56px; height: 56px; font-size: 1.3rem;
  background: var(--hria-surface-3); color: var(--hria-text);
  box-shadow: var(--hria-card-shadow);
}

/* Anillo expansivo que llama la atención mientras nadie lo ha abierto todavía.
   Es el mismo lenguaje visual que el indicador "Online" de la barra superior. */
.halo {
  position: absolute;
  inset: 0;
  border-radius: 50%;
  pointer-events: none;
}
.launcher.idle .halo { animation: halo 2.6s ease-out infinite; }
@keyframes halo {
  0%   { box-shadow: 0 0 0 0 rgba(22, 185, 138, 0.55); }
  70%  { box-shadow: 0 0 0 22px rgba(22, 185, 138, 0); }
  100% { box-shadow: 0 0 0 0 rgba(22, 185, 138, 0); }
}

/* Etiqueta al pasar por encima: dice qué hace sin ocupar sitio permanente. */
.hint {
  position: absolute;
  right: calc(100% + 0.7rem);
  top: 50%;
  transform: translateY(-50%) translateX(6px);
  white-space: nowrap;
  background: var(--hria-surface);
  color: var(--hria-strong);
  border: 1px solid var(--hria-border-strong);
  box-shadow: var(--hria-card-shadow);
  padding: 0.42rem 0.75rem;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 500;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.18s ease, transform 0.18s ease;
}
.launcher:hover .hint,
.launcher:focus-visible .hint { opacity: 1; transform: translateY(-50%) translateX(0); }
.launcher.open .hint { display: none; }

@media (prefers-reduced-motion: reduce) {
  .launcher, .hint { transition: none; }
  .launcher.idle .halo { animation: none; }
  .launcher:hover { transform: none; }
}

.panel {
  width: min(380px, calc(100vw - 2.5rem));
  height: min(540px, calc(100vh - 8rem));
  display: flex;
  flex-direction: column;
  background: var(--hria-surface);
  border: 1px solid var(--hria-border-strong);
  border-radius: 16px;
  box-shadow: 0 12px 40px rgba(16, 24, 40, 0.18);
  overflow: hidden;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.8rem 0.9rem 0.4rem;
}
.title { display: inline-flex; align-items: center; gap: 0.5rem; font-weight: 600; color: var(--hria-heading); }
.title i { color: var(--hria-accent); }
.panel-sub {
  margin: 0 0.9rem 0.5rem;
  font-size: 0.74rem;
  color: var(--hria-muted-2);
  border-bottom: 1px solid var(--hria-divider);
  padding-bottom: 0.5rem;
}
.panel-body { flex: 1; min-height: 0; display: flex; }

.icon-btn {
  border: none;
  background: transparent;
  color: var(--hria-muted);
  cursor: pointer;
  width: 30px;
  height: 30px;
  border-radius: 50%;
  display: grid;
  place-items: center;
}
.icon-btn:hover { background: var(--hria-surface-3); color: var(--hria-text); }

.panel-enter-active, .panel-leave-active { transition: opacity 0.16s ease, transform 0.16s ease; }
.panel-enter-from, .panel-leave-to { opacity: 0; transform: translateY(10px); }

@media (max-width: 480px) {
  .ai-widget { right: 0.75rem; bottom: 0.75rem; }
  .panel { height: min(70vh, calc(100vh - 6rem)); }
}
</style>
