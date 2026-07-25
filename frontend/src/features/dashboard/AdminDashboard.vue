<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import Chart from 'primevue/chart'
import Message from 'primevue/message'
import Paginator from 'primevue/paginator'
import ProgressSpinner from 'primevue/progressspinner'
import WidgetCard from '@/shared/components/WidgetCard.vue'
import PlanningWidgets from './PlanningWidgets.vue'
import { dashboardApi } from './api'
import type { DashboardSummary, HoursByDayPoint } from './types'
import { formatDateTime } from '@/shared/utils/format'
import type { ApiError } from '@/shared/http/client'
import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()
const summary = ref<DashboardSummary | null>(null)
const hours = ref<HoursByDayPoint[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

// Actividad reciente paginada: el backend devuelve hasta 100 eventos.
// Cinco por página deja la tarjeta a la altura del gráfico de al lado.
const PUNCHES_PER_PAGE = 5
const punchFirst = ref(0)

const pagedPunches = computed(() =>
  (summary.value?.recentPunches ?? []).slice(punchFirst.value, punchFirst.value + PUNCHES_PER_PAGE),
)

const cards = computed(() => {
  const s = summary.value
  if (!s) return []
  return [
    { label: 'Empleados activos', value: s.activeEmployees, icon: 'pi pi-users', color: '#16b98a' },
    { label: 'Trabajando ahora', value: s.working, icon: 'pi pi-play-circle', color: '#0d9973' },
    { label: 'En descanso', value: s.onBreak, icon: 'pi pi-pause-circle', color: '#f59e0b' },
    { label: 'Jornadas incompletas', value: s.incompleteWorkdays, icon: 'pi pi-exclamation-triangle', color: '#f43f5e' },
    { label: 'Ausentes hoy', value: s.onLeaveToday, icon: 'pi pi-sun', color: '#f59e0b' },
    { label: 'Solicitudes pendientes', value: s.pendingAbsenceRequests, icon: 'pi pi-inbox', color: '#f43f5e' },
  ]
})

const chartData = computed(() => ({
  labels: hours.value.map((p) => new Date(p.date + 'T00:00:00').toLocaleDateString([], { weekday: 'short', day: '2-digit' })),
  datasets: [
    {
      label: 'Horas trabajadas',
      data: hours.value.map((p) => p.hours),
      backgroundColor: '#16b98a',
      borderRadius: 6,
      maxBarThickness: 34,
    },
  ],
}))

// Chart.js pinta sobre un <canvas>, así que no hereda las variables CSS:
// los colores de ejes y rejilla hay que calcularlos según el esquema activo.
const chartOptions = computed(() => {
  const tick = theme.isDark ? '#93a59e' : '#64748b'
  const grid = theme.isDark ? 'rgba(203,213,225,0.10)' : 'rgba(15,23,42,0.06)'
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { grid: { display: false }, ticks: { color: tick } },
      y: { beginAtZero: true, grid: { color: grid }, ticks: { color: tick } },
    },
  }
})

function actionColor(action: string) {
  if (action === 'Entrada') return '#16b98a'
  if (action === 'Salida') return '#f43f5e'
  return '#f59e0b'
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [s, h] = await Promise.all([dashboardApi.summary(), dashboardApi.hoursByDay()])
    summary.value = s
    hours.value = h
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cargar el dashboard.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="dash">
    <div v-if="loading" class="center"><ProgressSpinner style="width: 48px; height: 48px" /></div>
    <Message v-else-if="error" severity="error" :closable="false">{{ error }}</Message>

    <template v-else-if="summary">
      <div class="cards">
        <div v-for="c in cards" :key="c.label" class="stat">
          <span class="stat-icon" :style="{ color: c.color, background: c.color + '1a' }">
            <i :class="c.icon" />
          </span>
          <div class="stat-body">
            <div class="stat-value">{{ c.value }}</div>
            <div class="stat-label">{{ c.label }}</div>
          </div>
        </div>
      </div>

      <PlanningWidgets :summary="summary" />

      <div class="grid">
        <WidgetCard title="Horas trabajadas por día" icon="pi pi-chart-bar" class="span-2">
          <div class="chart-wrap">
            <Chart type="bar" :data="chartData" :options="chartOptions" />
          </div>
        </WidgetCard>

        <WidgetCard title="Actividad reciente" icon="pi pi-bell">
          <template v-if="summary.recentPunches.length">
            <ul class="punches">
              <li v-for="(p, i) in pagedPunches" :key="punchFirst + i">
                <span class="dot" :style="{ background: actionColor(p.action) }" aria-hidden="true"></span>
                <div class="punch-main">
                  <strong>{{ p.employeeName }}</strong>
                  <small>{{ p.department }}</small>
                </div>
                <div class="punch-meta">
                  <span class="action">{{ p.action }}</span>
                  <small>{{ formatDateTime(p.timeUtc) }}</small>
                </div>
              </li>
            </ul>
            <Paginator
              v-if="summary.recentPunches.length > PUNCHES_PER_PAGE"
              :rows="PUNCHES_PER_PAGE"
              :totalRecords="summary.recentPunches.length"
              :first="punchFirst"
              @page="punchFirst = $event.first"
            />
          </template>
          <div v-else class="empty">Sin fichajes recientes.</div>
        </WidgetCard>
      </div>
    </template>
  </div>
</template>

<style scoped>
.dash { display: flex; flex-direction: column; gap: 1.25rem; }
.center { display: grid; place-items: center; padding: 3rem; }

.cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(175px, 1fr));
  gap: 1rem;
}
.stat {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  background: var(--hria-surface);
  border: 1px solid var(--hria-border);
  border-radius: 16px;
  padding: 1.1rem 1.2rem;
  box-shadow: var(--hria-card-shadow);
}
.stat-icon {
  width: 46px; height: 46px; border-radius: 12px;
  display: grid; place-items: center; font-size: 1.25rem;
}
.stat-value { font-size: 1.55rem; font-weight: 700; color: var(--hria-heading); line-height: 1.1; }
.stat-label { font-size: 0.82rem; color: var(--hria-muted); }

/* Las dos tarjetas de esta fila deben quedar a la misma altura: la de
   actividad estira su lista y baja el paginador al pie. */
.grid {
  display: grid;
  grid-template-columns: 1.5fr 1fr;
  gap: 1.25rem;
  align-items: stretch;
}
.grid > :deep(.widget) { display: flex; flex-direction: column; }
.grid > :deep(.widget) > .widget-body { flex: 1; display: flex; flex-direction: column; }
.span-2 { grid-column: auto; }
@media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }

.chart-wrap { position: relative; height: 280px; }

/* Reserva el hueco de las cinco filas aunque la última página traiga menos,
   para que la tarjeta no cambie de alto al pasar de página. */
.punches {
  list-style: none;
  margin: 0 0 auto;
  padding: 0;
  display: flex;
  flex-direction: column;
  min-height: 272px;
}
.punches li {
  display: flex; align-items: center; gap: 0.75rem;
  padding: 0.55rem 0; border-bottom: 1px solid var(--hria-divider);
}
.punches li:last-child { border-bottom: none; }
.dot { width: 9px; height: 9px; border-radius: 50%; flex: none; }
.punch-main { display: flex; flex-direction: column; flex: 1; min-width: 0; }
.punch-main strong { font-size: 0.9rem; color: var(--hria-strong); }
.punch-main small { font-size: 0.75rem; color: var(--hria-muted-2); }
.punch-meta { display: flex; flex-direction: column; align-items: end; }
.punch-meta .action { font-size: 0.82rem; color: var(--hria-accent-600); }
.punch-meta small { font-size: 0.72rem; color: var(--hria-muted-2); }
.empty { padding: 1.5rem; text-align: center; color: var(--hria-muted-2); }
</style>
