<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    name: string
    avatarUrl?: string | null
    /** Diámetro en píxeles. */
    size?: number
  }>(),
  { avatarUrl: null, size: 36 },
)

/**
 * Si la imagen no carga se cae a las iniciales en lugar de dejar un hueco roto.
 * Es un caso real: la ruta puede quedar obsoleta y el avatar no merece romper
 * la interfaz.
 */
const imageFailed = ref(false)
watch(() => props.avatarUrl, () => (imageFailed.value = false))

const showImage = computed(() => !!props.avatarUrl && !imageFailed.value)

const initials = computed(() => {
  const parts = props.name?.trim().split(/\s+/) ?? []
  if (parts.length === 0) return '?'
  return parts
    .slice(0, 2)
    .map((p) => p[0])
    .join('')
    .toUpperCase()
})

/**
 * Color estable derivado del nombre: la misma persona tiene siempre el mismo,
 * y personas distintas se distinguen de un vistazo en listados y calendarios.
 * Antes todos compartían el mismo verde y eran indistinguibles.
 */
const PALETTE = [
  ['#0d9973', '#33cc9a'],
  ['#2563eb', '#60a5fa'],
  ['#7c3aed', '#a78bfa'],
  ['#db2777', '#f472b6'],
  ['#ea580c', '#fb923c'],
  ['#0891b2', '#22d3ee'],
  ['#65a30d', '#a3e635'],
  ['#e11d48', '#fb7185'],
]

const gradient = computed(() => {
  let hash = 0
  for (const ch of props.name ?? '') hash = (hash * 31 + ch.charCodeAt(0)) >>> 0
  const [from, to] = PALETTE[hash % PALETTE.length]
  return `linear-gradient(135deg, ${from}, ${to})`
})

const style = computed(() => ({
  width: `${props.size}px`,
  height: `${props.size}px`,
  fontSize: `${Math.round(props.size * 0.36)}px`,
}))
</script>

<template>
  <img
    v-if="showImage"
    class="avatar"
    :src="avatarUrl!"
    :alt="name"
    :style="style"
    @error="imageFailed = true"
  />
  <span v-else class="avatar initials" :style="{ ...style, background: gradient }" :title="name" aria-hidden="true">
    {{ initials }}
  </span>
</template>

<style scoped>
.avatar {
  border-radius: 50%;
  flex: none;
  object-fit: cover;
  background: var(--hria-surface-3);
}
.initials {
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  letter-spacing: 0.5px;
}
</style>
