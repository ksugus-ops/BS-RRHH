<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import Card from 'primevue/card'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import { timeApi } from './api'
import { TimeState, type TimeStatus } from './types'
import { formatTime, formatMinutes } from '@/shared/utils/format'
import type { ApiError } from '@/shared/http/client'
import { useAuthStore } from '@/stores/auth'
import UserAvatar from '@/shared/components/UserAvatar.vue'

const props = withDefaults(defineProps<{ showUser?: boolean }>(), { showUser: true })
const emit = defineEmits<{ (e: 'changed'): void }>()
const toast = useToast()
const auth = useAuthStore()

const status = ref<TimeStatus | null>(null)
const loading = ref(false)
const acting = ref(false)
const error = ref<string | null>(null)

const state = computed(() => status.value?.state ?? TimeState.NotStarted)
const isWorking = computed(() => state.value === TimeState.Working)
const isOnBreak = computed(() => state.value === TimeState.OnBreak)
const isIdle = computed(() => state.value === TimeState.NotStarted)

const stateLabel = computed(() => {
  if (isWorking.value) return { text: 'Trabajando', severity: 'success' as const }
  if (isOnBreak.value) return { text: 'En descanso', severity: 'warn' as const }
  return { text: 'Sin fichar', severity: 'secondary' as const }
})

async function loadStatus() {
  loading.value = true
  error.value = null
  try {
    status.value = await timeApi.status()
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cargar el estado.'
  } finally {
    loading.value = false
  }
}

async function run(action: () => Promise<unknown>, successMsg: string) {
  acting.value = true
  error.value = null
  try {
    await action()
    await loadStatus()
    toast.add({ severity: 'success', summary: 'Fichaje', detail: successMsg, life: 2500 })
    emit('changed')
  } catch (e) {
    const err = e as ApiError
    toast.add({ severity: 'error', summary: 'No permitido', detail: err.title, life: 3500 })
  } finally {
    acting.value = false
  }
}

onMounted(loadStatus)
</script>

<template>
  <Card class="clock">
    <template #content>
      <div class="panel" :class="{ 'no-user': !props.showUser }">
        <!-- Usuario: avatar + nombre -->
        <div v-if="props.showUser" class="user-side">
          <UserAvatar
            :name="auth.user?.fullName ?? ''"
            :avatar-url="auth.user?.avatarUrl"
            :size="64"
          />
          <span class="user-name">{{ auth.user?.fullName }}</span>
          <span v-if="auth.user?.department" class="user-dept">{{ auth.user?.department }}</span>
        </div>

        <!-- Reloj ilustrado (SVG propio) + estado -->
        <div class="clock-side">
          <svg viewBox="0 0 120 120" width="104" height="104" role="img" aria-label="Control de fichaje">
            <circle cx="56" cy="58" r="40" fill="none" stroke="var(--hria-clock-stroke)" stroke-width="9" />
            <path d="M 56 10 A 48 48 0 0 1 104 58" fill="none" stroke="var(--hria-clock-stroke)" stroke-width="9" stroke-linecap="round" />
            <path d="M 104 58 l -13 -5 l 3 15 z" fill="var(--hria-clock-stroke)" />
            <line x1="56" y1="58" x2="56" y2="31" stroke="var(--hria-clock-stroke)" stroke-width="7" stroke-linecap="round" />
            <line x1="56" y1="58" x2="38" y2="58" stroke="var(--hria-clock-stroke)" stroke-width="7" stroke-linecap="round" />
            <circle cx="56" cy="58" r="6" fill="#16b98a" />
            <circle cx="94" cy="93" r="21" fill="#16b98a" />
            <path d="M 84 93 l 6 7 l 13 -14" fill="none" stroke="#fff" stroke-width="5"
              stroke-linecap="round" stroke-linejoin="round" />
          </svg>
          <div class="state-row">
            <span>Estado actual:</span>
            <Tag :value="stateLabel.text" :severity="stateLabel.severity" />
          </div>
          <p v-if="status?.workday" class="since">
            Entrada {{ formatTime(status.workday.checkIn) }} · {{ formatMinutes(status.workday.workedMinutes) }}
          </p>
        </div>

        <!-- Cuadrícula 2x2 de acciones -->
        <div class="buttons">
          <Button
            label="Fichar entrada" icon="pi pi-sign-in" size="large" fluid
            :disabled="!isIdle || acting" :loading="acting && isIdle"
            @click="run(timeApi.checkIn, 'Entrada registrada.')"
          />
          <Button
            label="Fichar salida" icon="pi pi-sign-out" severity="danger" size="large" fluid
            :disabled="!isWorking || acting"
            @click="run(timeApi.checkOut, 'Salida registrada.')"
          />
          <Button
            label="Iniciar descanso" icon="pi pi-pause" severity="warn" size="large" fluid
            :disabled="!isWorking || acting"
            @click="run(timeApi.startBreak, 'Descanso iniciado.')"
          />
          <Button
            label="Finalizar descanso" icon="pi pi-play" severity="warn" outlined size="large" fluid
            :disabled="!isOnBreak || acting"
            @click="run(timeApi.endBreak, 'Descanso finalizado.')"
          />
        </div>
      </div>

      <Message v-if="error" severity="error" :closable="false" class="err">{{ error }}</Message>
    </template>
  </Card>
</template>

<style scoped>
.clock {
  width: 100%;
}
.panel {
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 1.75rem;
  align-items: center;
}
.panel.no-user {
  grid-template-columns: auto 1fr;
}
.user-side {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
  text-align: center;
  min-width: 120px;
  padding-right: 1.5rem;
  border-right: 1px solid var(--hria-border);
}
.user-side .avatar {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 1.25rem;
  font-weight: 700;
  color: #ffffff;
  background: linear-gradient(135deg, #0d9973, #33cc9a);
}
.user-name { font-weight: 600; font-size: 0.95rem; color: var(--hria-strong); }
.user-dept { font-size: 0.78rem; color: var(--hria-muted-2); }
.clock-side {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
  min-width: 150px;
}
.state-row {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
  justify-content: center;
  font-size: 0.9rem;
}
.since {
  text-align: center;
  color: var(--p-text-muted-color, #6b7280);
  font-size: 0.85rem;
  margin: 0;
}
.buttons {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.85rem;
}
.buttons :deep(.p-button) {
  padding-top: 1rem;
  padding-bottom: 1rem;
  font-size: 1rem;
  border-radius: 12px;
}
.err { margin-top: 1rem; }

@media (max-width: 820px) {
  .panel { grid-template-columns: 1fr; gap: 1.25rem; justify-items: center; }
  .user-side { border-right: none; padding-right: 0; border-bottom: 1px solid var(--hria-border); padding-bottom: 1rem; }
  .buttons { grid-template-columns: 1fr; width: 100%; }
}
</style>
