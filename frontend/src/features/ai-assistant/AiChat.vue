<script setup lang="ts">
import { nextTick, ref } from 'vue'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import Tag from 'primevue/tag'
import { aiApi } from './api'
import { useAuthStore } from '@/stores/auth'
import type { ApiError } from '@/shared/http/client'

interface ChatMessage {
  role: 'user' | 'assistant'
  text: string
  mode?: string
  tools?: string[]
}

withDefaults(defineProps<{ compact?: boolean }>(), { compact: false })

const auth = useAuthStore()
const question = ref('')
const sending = ref(false)
const messages = ref<ChatMessage[]>([])
const listRef = ref<HTMLElement | null>(null)
const composerRef = ref<HTMLElement | null>(null)

const adminSuggestions = [
  '¿Cuántos empleados están trabajando ahora?',
  '¿Quién tiene una jornada abierta?',
  '¿Qué empleados tienen jornadas incompletas?',
  'Resume las horas del departamento de Desarrollo esta semana',
]
const employeeSuggestions = [
  'Resume mis horas de esta semana',
  '¿Cuántas horas he trabajado?',
]
const suggestions = auth.isAdmin ? adminSuggestions : employeeSuggestions

async function scrollDown() {
  await nextTick()
  listRef.value?.scrollTo({ top: listRef.value.scrollHeight, behavior: 'smooth' })
}

async function send(text?: string) {
  const q = (text ?? question.value).trim()
  if (!q || sending.value) return

  messages.value.push({ role: 'user', text: q })
  question.value = ''
  sending.value = true
  scrollDown()

  try {
    const res = await aiApi.ask(q)
    messages.value.push({ role: 'assistant', text: res.answer, mode: res.mode, tools: res.toolsUsed })
  } catch (e) {
    const err = e as ApiError
    const msg = err.status === 429
      ? 'Has hecho demasiadas consultas seguidas. Espera un momento e inténtalo de nuevo.'
      : (err.title ?? 'No se pudo obtener respuesta del asistente.')
    messages.value.push({ role: 'assistant', text: msg, mode: 'error' })
  } finally {
    sending.value = false
    scrollDown()
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    send()
  }
}

/** La ventana flotante enfoca el cuadro de texto al abrirse. */
function focusInput() {
  nextTick(() => composerRef.value?.querySelector('textarea')?.focus())
}

defineExpose({ focusInput })
</script>

<template>
  <div class="chat" :class="{ compact }">
    <div ref="listRef" class="messages">
      <div v-if="messages.length === 0" class="welcome">
        <p>Prueba con una de estas preguntas:</p>
        <div class="suggestions">
          <button v-for="s in suggestions" :key="s" class="chip" type="button" @click="send(s)">{{ s }}</button>
        </div>
      </div>

      <div v-for="(m, i) in messages" :key="i" class="msg" :class="m.role">
        <div class="bubble">
          <p class="text">{{ m.text }}</p>
          <div v-if="m.role === 'assistant' && m.mode" class="meta">
            <Tag v-if="m.mode === 'demo'" value="Modo demo" severity="warn" />
            <Tag v-else-if="m.mode === 'live'" value="IA" severity="success" />
            <Tag v-else-if="m.mode === 'error'" value="Error" severity="danger" />
            <span v-if="m.tools && m.tools.length" class="tools">
              <i class="pi pi-wrench" /> {{ m.tools.join(', ') }}
            </span>
          </div>
        </div>
      </div>

      <div v-if="sending" class="msg assistant">
        <div class="bubble typing"><span></span><span></span><span></span></div>
      </div>
    </div>

    <div ref="composerRef" class="composer">
      <Textarea
        v-model="question"
        rows="1"
        autoResize
        placeholder="Escribe tu pregunta…"
        @keydown="onKeydown"
        aria-label="Pregunta al asistente"
      />
      <Button icon="pi pi-send" :loading="sending" :disabled="!question.trim()"
        aria-label="Enviar" @click="send()" />
    </div>
  </div>
</template>

<style scoped>
.chat {
  display: flex; flex-direction: column;
  background: var(--hria-surface); border: 1px solid var(--hria-border); border-radius: 16px;
  box-shadow: var(--hria-card-shadow); overflow: hidden;
  height: 62vh; min-height: 420px;
}
/* Dentro de la ventana flotante el alto lo fija el contenedor. */
.chat.compact { height: 100%; min-height: 0; border: none; border-radius: 0; box-shadow: none; background: transparent; }

.messages { flex: 1; overflow-y: auto; padding: 1.25rem; display: flex; flex-direction: column; gap: 0.9rem; }
.compact .messages { padding: 0.9rem; gap: 0.7rem; }

.welcome { color: var(--hria-muted); }
.welcome p { margin: 0 0 0.75rem; }
.compact .welcome p { font-size: 0.85rem; }
.suggestions { display: flex; flex-wrap: wrap; gap: 0.5rem; }
.chip {
  border: 1px solid var(--hria-border-strong); background: var(--hria-accent-soft); color: var(--hria-accent-700);
  padding: 0.5rem 0.85rem; border-radius: 999px; font-size: 0.85rem; cursor: pointer;
  text-align: left;
}
.compact .chip { font-size: 0.78rem; padding: 0.4rem 0.7rem; }
.chip:hover { background: var(--hria-accent-soft-2); }

.msg { display: flex; }
.msg.user { justify-content: flex-end; }
.bubble { max-width: 78%; padding: 0.75rem 1rem; border-radius: 14px; }
.compact .bubble { max-width: 88%; padding: 0.6rem 0.8rem; font-size: 0.88rem; }
.msg.user .bubble { background: #16b98a; color: #fff; border-bottom-right-radius: 4px; }
.msg.assistant .bubble { background: var(--hria-surface-3); color: var(--hria-strong); border-bottom-left-radius: 4px; }
.text { margin: 0; white-space: pre-wrap; line-height: 1.5; }
.meta { display: flex; align-items: center; gap: 0.6rem; margin-top: 0.5rem; flex-wrap: wrap; }
.tools { font-size: 0.75rem; color: var(--hria-muted-2); }

.typing { display: flex; gap: 4px; }
.typing span { width: 7px; height: 7px; border-radius: 50%; background: var(--hria-muted-2); animation: blink 1.2s infinite; }
.typing span:nth-child(2) { animation-delay: 0.2s; }
.typing span:nth-child(3) { animation-delay: 0.4s; }
@keyframes blink { 0%, 60%, 100% { opacity: 0.3; } 30% { opacity: 1; } }

.composer {
  display: flex; align-items: end; gap: 0.6rem; padding: 0.85rem;
  border-top: 1px solid var(--hria-border); background: var(--hria-surface-2);
}
.compact .composer { padding: 0.6rem; }
.composer :deep(textarea) { flex: 1; resize: none; max-height: 120px; }
</style>
