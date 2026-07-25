<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import WidgetCard from '@/shared/components/WidgetCard.vue'
import { timeApi } from '@/features/time-tracking/api'
import { WorkdayStatus, type Workday } from '@/features/time-tracking/types'
import { workCalendarApi } from '@/features/work-calendar/api'
import type { CalendarDay } from '@/features/work-calendar/types'
import { formatMinutes, formatTime } from '@/shared/utils/format'
import type { ApiError } from '@/shared/http/client'

const workdays = ref<Workday[]>([])
const nonWorkingDates = ref<Set<string>>(new Set())
const loading = ref(false)
const error = ref<string | null>(null)

const today = new Date()
const monthName = today.toLocaleDateString('es-ES', { month: 'long', year: 'numeric' })
const daysInMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0).getDate()

function isoDay(day: number): string {
  return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

async function load() {
  loading.value = true
  error.value = null
  try {
    // Las jornadas son las del propio empleado: el identificador se deriva del
    // token, no se envía desde aquí.
    const [w, cal] = await Promise.all([
      timeApi.workdays({ from: isoDay(1), to: isoDay(daysInMonth) }),
      workCalendarApi.days(today.getFullYear()),
    ])
    workdays.value = w
    // Festivos y días no laborables según el calendario de la empresa, no por
    // el día de la semana: un centro que trabaje el sábado los conserva.
    nonWorkingDates.value = new Set(
      cal.filter((d: CalendarDay) => !d.isWorkingDay).map((d: CalendarDay) => d.date.slice(0, 10)),
    )
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar tus fichajes.'
  } finally {
    loading.value = false
  }
}

/**
 * Fichajes del mes en días laborables, en orden cronológico.
 *
 * Se recorre la lista de jornadas, no un mapa por fecha: un mismo día puede
 * tener más de un fichaje y agrupar por fecha perdería todos menos uno.
 */
const rows = computed(() =>
  workdays.value
    .filter((w) => !nonWorkingDates.value.has(w.date.slice(0, 10)))
    .sort((a, b) => a.checkIn.localeCompare(b.checkIn)),
)

const totalMinutes = computed(() => rows.value.reduce((t, w) => t + w.workedMinutes, 0))

/** Solo se suman los días con previsión; sin horario asignado no hay desviación. */
const totalDeviation = computed(() => {
  const withExpected = rows.value.filter((w) => w.deviationMinutes !== null)
  if (withExpected.length === 0) return null
  return withExpected.reduce((t, w) => t + (w.deviationMinutes ?? 0), 0)
})

function dayLabel(w: Workday): string {
  const d = new Date(w.date.slice(0, 10) + 'T00:00:00')
  const txt = d.toLocaleDateString('es-ES', { weekday: 'short', day: '2-digit', month: '2-digit' })
  return txt.charAt(0).toUpperCase() + txt.slice(1)
}

function statusTag(status: WorkdayStatus) {
  switch (status) {
    case WorkdayStatus.Completed: return { text: 'Completa', severity: 'success' as const }
    case WorkdayStatus.Open: return { text: 'Abierta', severity: 'info' as const }
    default: return { text: 'Incompleta', severity: 'danger' as const }
  }
}

function formatDeviation(minutes: number): string {
  if (minutes === 0) return '0 min'
  return `${minutes > 0 ? '+' : '−'}${formatMinutes(Math.abs(minutes))}`
}

onMounted(load)
</script>

<template>
  <WidgetCard title="Mis fichajes de este mes" icon="pi pi-history">
    <p class="sub">{{ monthName.charAt(0).toUpperCase() + monthName.slice(1) }} · días laborables</p>

    <Message v-if="error" severity="error" :closable="false">
      {{ error }}
      <Button label="Reintentar" text size="small" @click="load" />
    </Message>

    <div v-else-if="loading" class="loading"><ProgressSpinner style="width: 36px; height: 36px" /></div>

    <template v-else-if="rows.length">
      <div class="totals">
        <div>
          <span>Trabajado</span>
          <strong>{{ formatMinutes(totalMinutes) }}</strong>
        </div>
        <div>
          <span>Fichajes</span>
          <strong>{{ rows.length }}</strong>
        </div>
        <div v-if="totalDeviation !== null">
          <span>Desviación</span>
          <strong :class="totalDeviation < 0 ? 'neg' : 'pos'">{{ formatDeviation(totalDeviation) }}</strong>
        </div>
      </div>

      <table class="days">
        <thead>
          <tr>
            <th scope="col">Día</th>
            <th scope="col">Entrada</th>
            <th scope="col">Salida</th>
            <th scope="col" class="num">Trabajado</th>
            <th scope="col" class="num">Desviación</th>
            <th scope="col">Estado</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="w in rows" :key="w.id">
            <td class="day">{{ dayLabel(w) }}</td>
            <td class="hour">{{ formatTime(w.checkIn) }}</td>
            <td class="hour">{{ w.checkOut ? formatTime(w.checkOut) : '—' }}</td>
            <td class="num">{{ formatMinutes(w.workedMinutes) }}</td>
            <td class="num">
              <span v-if="w.deviationMinutes === null" class="muted">—</span>
              <span v-else :class="w.deviationMinutes < 0 ? 'neg' : 'pos'">
                {{ formatDeviation(w.deviationMinutes) }}
              </span>
            </td>
            <td>
              <Tag :severity="statusTag(w.status).severity" :value="statusTag(w.status).text" />
            </td>
          </tr>
        </tbody>
      </table>
    </template>

    <p v-else class="empty">No hay fichajes en días laborables este mes.</p>
  </WidgetCard>
</template>

<style scoped>
.sub { margin: 0 0 0.75rem; font-size: 0.78rem; color: var(--hria-muted); }
.loading { display: grid; place-items: center; padding: 2rem; }

.totals { display: flex; gap: 1.5rem; flex-wrap: wrap; margin-bottom: 0.9rem; }
.totals div { display: flex; flex-direction: column; }
.totals span { font-size: 0.72rem; color: var(--hria-muted); }
.totals strong { font-size: 1.2rem; color: var(--hria-heading); }

.days { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.days th {
  text-align: left;
  font-weight: 600;
  font-size: 0.74rem;
  color: var(--hria-muted);
  padding: 0.35rem 0.5rem;
  border-bottom: 1px solid var(--hria-border-strong);
}
.days td { padding: 0.42rem 0.5rem; border-bottom: 1px solid var(--hria-divider); }
.days tr:last-child td { border-bottom: none; }
.days .num { text-align: right; }
.day { color: var(--hria-strong); }
.hour, .days td.num { font-variant-numeric: tabular-nums; }
.muted { color: var(--hria-muted-2); }
.neg { color: #e34948; font-weight: 600; }
.pos { color: var(--hria-accent-600); font-weight: 600; }

.empty { padding: 1.5rem; text-align: center; color: var(--hria-muted-2); margin: 0; }
</style>
