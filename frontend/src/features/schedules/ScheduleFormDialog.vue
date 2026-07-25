<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import Checkbox from 'primevue/checkbox'
import { schedulesApi } from './api'
import { WEEKDAYS, type ScheduleDetail } from './types'
import type { ApiError } from '@/shared/http/client'

const props = defineProps<{ visible: boolean; schedule: ScheduleDetail | null }>()
const emit = defineEmits<{ 'update:visible': [boolean]; saved: [] }>()

interface EditableSlot {
  dayOfWeek: number
  startTime: string
  endTime: string
}

const name = ref('')
const description = ref('')
const isActive = ref(true)
const slots = ref<EditableSlot[]>([])
const saving = ref(false)
const error = ref<string | null>(null)

const isEdit = computed(() => props.schedule !== null)

/** Días con al menos un tramo, para las casillas de selección rápida. */
const selectedDays = computed(() => [...new Set(slots.value.map((s) => s.dayOfWeek))])

const weeklyMinutes = computed(() =>
  slots.value.reduce((total, s) => total + minutesBetween(s.startTime, s.endTime), 0),
)

function minutesBetween(start: string, end: string): number {
  const [sh, sm] = start.split(':').map(Number)
  const [eh, em] = end.split(':').map(Number)
  const diff = eh * 60 + em - (sh * 60 + sm)
  return diff > 0 ? diff : 0
}

function formatHours(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return m === 0 ? `${h} h` : `${h} h ${m} min`
}

function toggleDay(day: number) {
  if (selectedDays.value.includes(day)) {
    slots.value = slots.value.filter((s) => s.dayOfWeek !== day)
  } else {
    slots.value.push({ dayOfWeek: day, startTime: '09:00', endTime: '17:00' })
    sortSlots()
  }
}

function addSlot(day: number) {
  slots.value.push({ dayOfWeek: day, startTime: '15:00', endTime: '18:00' })
  sortSlots()
}

function removeSlot(index: number) {
  slots.value.splice(index, 1)
}

function sortSlots() {
  const order = WEEKDAYS.map((d) => d.value)
  slots.value.sort(
    (a, b) => order.indexOf(a.dayOfWeek) - order.indexOf(b.dayOfWeek) || a.startTime.localeCompare(b.startTime),
  )
}

function slotsOfDay(day: number) {
  return slots.value
    .map((slot, index) => ({ slot, index }))
    .filter((x) => x.slot.dayOfWeek === day)
}

watch(
  () => props.visible,
  (open) => {
    if (!open) return
    error.value = null
    if (props.schedule) {
      name.value = props.schedule.name
      description.value = props.schedule.description ?? ''
      isActive.value = props.schedule.isActive
      slots.value = props.schedule.slots.map((s) => ({
        dayOfWeek: s.dayOfWeek,
        startTime: s.startTime.slice(0, 5),
        endTime: s.endTime.slice(0, 5),
      }))
    } else {
      name.value = ''
      description.value = ''
      isActive.value = true
      // Arranque razonable: jornada de lunes a viernes.
      slots.value = [1, 2, 3, 4, 5].map((d) => ({ dayOfWeek: d, startTime: '09:00', endTime: '17:00' }))
    }
  },
)

async function save() {
  error.value = null

  if (!name.value.trim()) {
    error.value = 'Indica un nombre para el horario.'
    return
  }
  if (slots.value.length === 0) {
    error.value = 'El horario debe tener al menos un tramo.'
    return
  }
  const invalid = slots.value.find((s) => minutesBetween(s.startTime, s.endTime) <= 0)
  if (invalid) {
    error.value = 'Cada tramo debe terminar después de empezar.'
    return
  }

  saving.value = true
  try {
    const payload = {
      name: name.value.trim(),
      description: description.value.trim() || null,
      slots: slots.value.map((s) => ({
        dayOfWeek: s.dayOfWeek,
        startTime: `${s.startTime}:00`,
        endTime: `${s.endTime}:00`,
      })),
    }
    if (props.schedule) {
      await schedulesApi.update(props.schedule.id, { ...payload, isActive: isActive.value })
    } else {
      await schedulesApi.create(payload)
    }
    emit('saved')
    emit('update:visible', false)
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo guardar el horario.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    modal
    :header="isEdit ? 'Editar horario' : 'Nuevo horario'"
    :style="{ width: '46rem' }"
    :breakpoints="{ '960px': '95vw' }"
  >
    <div class="form">
      <div class="field">
        <label for="sched-name">Nombre</label>
        <InputText id="sched-name" v-model="name" placeholder="Oficina 9-17" fluid />
      </div>

      <div class="field">
        <label for="sched-desc">Descripción</label>
        <InputText id="sched-desc" v-model="description" placeholder="Opcional" fluid />
      </div>

      <div class="field" v-if="isEdit">
        <label class="inline">
          <Checkbox v-model="isActive" :binary="true" inputId="sched-active" />
          <span>Horario activo</span>
        </label>
      </div>

      <fieldset class="days">
        <legend>Días de la semana</legend>
        <div class="day-toggles">
          <button
            v-for="d in WEEKDAYS"
            :key="d.value"
            type="button"
            class="day-toggle"
            :class="{ on: selectedDays.includes(d.value) }"
            :aria-pressed="selectedDays.includes(d.value)"
            @click="toggleDay(d.value)"
          >
            {{ d.short }}
          </button>
        </div>
      </fieldset>

      <div class="slots">
        <div v-for="d in WEEKDAYS.filter((x) => selectedDays.includes(x.value))" :key="d.value" class="day-block">
          <div class="day-head">
            <strong>{{ d.label }}</strong>
            <Button
              icon="pi pi-plus"
              text
              rounded
              size="small"
              :aria-label="`Añadir tramo el ${d.label}`"
              @click="addSlot(d.value)"
            />
          </div>
          <div v-for="entry in slotsOfDay(d.value)" :key="entry.index" class="slot-row">
            <label class="sr-only" :for="`start-${entry.index}`">Hora de inicio</label>
            <input :id="`start-${entry.index}`" type="time" v-model="entry.slot.startTime" class="time" />
            <span aria-hidden="true">–</span>
            <label class="sr-only" :for="`end-${entry.index}`">Hora de fin</label>
            <input :id="`end-${entry.index}`" type="time" v-model="entry.slot.endTime" class="time" />
            <span class="dur">{{ formatHours(minutesBetween(entry.slot.startTime, entry.slot.endTime)) }}</span>
            <Button
              icon="pi pi-trash"
              text
              rounded
              size="small"
              severity="danger"
              aria-label="Eliminar tramo"
              @click="removeSlot(entry.index)"
            />
          </div>
        </div>
        <p v-if="slots.length === 0" class="empty">Selecciona al menos un día.</p>
      </div>

      <p class="total">Total semanal: <strong>{{ formatHours(weeklyMinutes) }}</strong></p>

      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    </div>

    <template #footer>
      <Button label="Cancelar" text @click="emit('update:visible', false)" />
      <Button label="Guardar" icon="pi pi-check" :loading="saving" @click="save" />
    </template>
  </Dialog>
</template>

<style scoped>
.form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.4rem; }
.field label { font-weight: 600; font-size: 0.9rem; }
.field label.inline { flex-direction: row; align-items: center; gap: 0.5rem; }

.days { border: 1px solid var(--hria-border); border-radius: 12px; padding: 0.75rem 1rem; margin: 0; }
.days legend { font-weight: 600; font-size: 0.9rem; padding: 0 0.35rem; }
.day-toggles { display: flex; gap: 0.4rem; flex-wrap: wrap; }
.day-toggle {
  width: 38px; height: 38px; border-radius: 50%; cursor: pointer;
  border: 1px solid var(--hria-border-strong); background: transparent;
  color: var(--hria-muted); font-weight: 700; font-size: 0.9rem;
}
.day-toggle.on { background: var(--hria-accent); border-color: var(--hria-accent); color: #fff; }

.slots { display: flex; flex-direction: column; gap: 0.75rem; }
.day-block { border: 1px solid var(--hria-border); border-radius: 12px; padding: 0.6rem 0.85rem; }
.day-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.35rem; }
.slot-row { display: flex; align-items: center; gap: 0.5rem; padding: 0.2rem 0; flex-wrap: wrap; }
.time {
  padding: 0.35rem 0.5rem; border-radius: 8px; font: inherit;
  border: 1px solid var(--hria-border-strong);
  background: var(--hria-surface); color: var(--hria-text);
}
.dur { font-size: 0.8rem; color: var(--hria-muted); margin-left: auto; }
.empty { color: var(--hria-muted); font-size: 0.9rem; margin: 0; }
.total { margin: 0; font-size: 0.95rem; }

.sr-only {
  position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px;
  overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0;
}
</style>
