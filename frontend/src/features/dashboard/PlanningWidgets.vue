<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import Chart from 'primevue/chart'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import WidgetCard from '@/shared/components/WidgetCard.vue'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import { dashboardApi } from './api'
import type { DashboardSummary, MonthActivity, Punctuality, UpcomingAbsences } from './types'
import { useThemeStore } from '@/stores/theme'
import type { ApiError } from '@/shared/http/client'

const props = defineProps<{ summary: DashboardSummary | null }>()

const theme = useThemeStore()

const month = ref<MonthActivity | null>(null)
const punctuality = ref<Punctuality | null>(null)
const upcoming = ref<UpcomingAbsences | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

/* Chart.js pinta sobre canvas y no hereda variables CSS. */
const ink = computed(() => (theme.isDark ? '#e2e8f0' : '#334155'))
const muted = computed(() => (theme.isDark ? '#93a59e' : '#64748b'))
const surface = computed(() => (theme.isDark ? '#15201c' : '#ffffff'))
const grid = computed(() => (theme.isDark ? 'rgba(203,213,225,0.10)' : 'rgba(15,23,42,0.06)'))

/* Paleta validada para daltonismo y contraste en ambos modos. */
const SERIES = computed(() =>
  theme.isDark
    ? { aqua: '#199e70', orange: '#d95926', blue: '#3987e5', violet: '#9085e9' }
    : { aqua: '#1baf7a', orange: '#eb6834', blue: '#2a78d6', violet: '#4a3aa7' },
)

// ------------------------------------------------------------------
// 1. Estado de la plantilla — parte de un todo, se lee de un vistazo → anillo
// ------------------------------------------------------------------

const staffSlices = computed(() => {
  const s = props.summary
  if (!s) return []
  const sinFichar = Math.max(0, s.activeEmployees - s.working - s.onBreak - s.onLeaveToday)
  return [
    { label: 'Trabajando', value: s.working, color: SERIES.value.aqua },
    { label: 'Ausentes', value: s.onLeaveToday, color: SERIES.value.violet },
    { label: 'En descanso', value: s.onBreak, color: SERIES.value.orange },
    { label: 'Sin fichar', value: sinFichar, color: SERIES.value.blue },
  ].filter((x) => x.value > 0)
})

const staffData = computed(() => ({
  labels: staffSlices.value.map((s) => s.label),
  datasets: [
    {
      data: staffSlices.value.map((s) => s.value),
      backgroundColor: staffSlices.value.map((s) => s.color),
      borderColor: surface.value,
      borderWidth: 2,
    },
  ],
}))

const doughnutOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '66%',
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (c: { label: string; raw: number; dataset: { data: number[] } }) => {
          const total = c.dataset.data.reduce((a, b) => a + b, 0)
          return ` ${c.label}: ${c.raw} (${Math.round((c.raw / total) * 100)}%)`
        },
      },
    },
  },
}))

// ------------------------------------------------------------------
// 2. Actividad del mes — comparar tres magnitudes → barras horizontales.
// ------------------------------------------------------------------

const MONTH_NAMES = [
  'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
  'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre',
]

const monthLabel = computed(() =>
  month.value ? `${MONTH_NAMES[month.value.month - 1]} de ${month.value.year}` : '',
)

const monthData = computed(() => {
  const m = month.value
  if (!m) return { labels: [], datasets: [] }
  return {
    labels: ['Trabajando', 'Vacaciones', 'Otras ausencias'],
    datasets: [
      {
        data: [m.workedDays, m.vacationDays, m.otherAbsenceDays],
        backgroundColor: [SERIES.value.aqua, SERIES.value.blue, SERIES.value.orange],
        borderRadius: 4,
        maxBarThickness: 22,
      },
    ],
  }
})

const monthOptions = computed(() => ({
  indexAxis: 'y' as const,
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: { callbacks: { label: (c: { raw: number }) => ` ${c.raw} días` } },
  },
  scales: {
    x: { beginAtZero: true, grid: { color: grid.value }, ticks: { color: muted.value } },
    y: { grid: { display: false }, ticks: { color: ink.value } },
  },
}))

// ------------------------------------------------------------------
// 3. Puntualidad — un porcentaje contra su total. Dos categorías no dan
//    para un anillo: se muestra la cifra grande y una barra de proporción.
// ------------------------------------------------------------------

const punctualityData = computed(() => {
  const p = punctuality.value
  if (!p) return { labels: [], datasets: [] }
  return {
    labels: [''],
    datasets: [
      { label: 'Dentro de horario', data: [p.onScheduleCount], backgroundColor: SERIES.value.aqua, borderColor: surface.value, borderWidth: 2, borderRadius: 4 },
      { label: 'Fuera de horario', data: [p.offScheduleCount], backgroundColor: SERIES.value.orange, borderColor: surface.value, borderWidth: 2, borderRadius: 4 },
    ],
  }
})

const punctualityOptions = computed(() => ({
  indexAxis: 'y' as const,
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      callbacks: {
        label: (c: { dataset: { label: string }; raw: number }) =>
          ` ${c.dataset.label}: ${c.raw} jornadas`,
      },
    },
  },
  // Barra de proporción: los ejes solo añadirían ruido.
  scales: { x: { stacked: true, display: false }, y: { stacked: true, display: false } },
}))

const hasPunctualityData = computed(
  () => !!punctuality.value && punctuality.value.onScheduleCount + punctuality.value.offScheduleCount > 0,
)

// ------------------------------------------------------------------

function shortDate(iso: string): string {
  const [, m, d] = iso.slice(0, 10).split('-')
  return `${d}/${m}`
}

function statusTag(status: number) {
  return status === 2
    ? { text: 'Aprobada', severity: 'success' as const }
    : { text: 'Pendiente', severity: 'warn' as const }
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [act, punt, up] = await Promise.all([
      dashboardApi.monthActivity(),
      dashboardApi.punctuality(5),
      dashboardApi.upcomingAbsences(),
    ])
    month.value = act
    punctuality.value = punt
    upcoming.value = up
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar los indicadores de planificación.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="planning">
    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <div v-if="loading" class="loading"><ProgressSpinner style="width: 40px; height: 40px" /></div>

    <template v-else-if="month && summary">
      <WidgetCard title="Previsión ausencias (2 semanas vista)" icon="pi pi-calendar">
        <p v-if="upcoming" class="lead-sub">
          Semana actual {{ shortDate(upcoming.thisWeekStart) }}–{{ shortDate(upcoming.thisWeekEnd) }}
          · próxima {{ shortDate(upcoming.nextWeekStart) }}–{{ shortDate(upcoming.nextWeekEnd) }}
        </p>

        <table v-if="upcoming && upcoming.absences.length" class="weeks">
          <thead>
            <tr>
              <th scope="col">Empleado</th>
              <th scope="col">Tipo</th>
              <th scope="col">Periodo</th>
              <th scope="col" class="num">Esta semana</th>
              <th scope="col" class="num">Próxima</th>
              <th scope="col">Estado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in upcoming.absences" :key="a.employeeId + '-' + a.startDate">
              <td>
                <div class="emp">
                  <UserAvatar :name="a.employeeName" :size="26" />
                  <span>
                    <span class="emp-name">{{ a.employeeName }}</span>
                    <small>{{ a.departmentName }}</small>
                  </span>
                </div>
              </td>
              <td>
                <span class="type">
                  <i class="dot" :style="{ background: a.colorHex ?? SERIES.aqua }" aria-hidden="true"></i>
                  {{ a.absenceTypeName }}
                </span>
              </td>
              <td class="nowrap">{{ shortDate(a.startDate) }} – {{ shortDate(a.endDate) }}</td>
              <td class="num">
                <span :class="{ zero: a.daysThisWeek === 0 }">{{ a.daysThisWeek }}</span>
              </td>
              <td class="num">
                <span :class="{ zero: a.daysNextWeek === 0 }">{{ a.daysNextWeek }}</span>
              </td>
              <td>
                <Tag :severity="statusTag(a.status).severity" :value="statusTag(a.status).text" />
              </td>
            </tr>
          </tbody>
        </table>

        <p v-else class="empty">Nadie falta esta semana ni la próxima.</p>
      </WidgetCard>

      <div class="charts">
        <WidgetCard title="Estado de la plantilla" icon="pi pi-users">
          <div class="chart-wrap">
            <!-- Sin la guarda, Chart.js se inicializa con el lienzo aún sin
                 datos y falla con "can't acquire context". -->
            <Chart v-if="staffSlices.length" type="doughnut" :data="staffData" :options="doughnutOptions" />
            <div class="centre" aria-hidden="true">
              <strong>{{ summary?.activeEmployees ?? 0 }}</strong>
              <small>activos</small>
            </div>
          </div>
          <ul class="legend">
            <li v-for="s in staffSlices" :key="s.label">
              <i class="dot" :style="{ background: s.color }" aria-hidden="true"></i>
              <span>{{ s.label }}</span>
              <b>{{ s.value }}</b>
            </li>
          </ul>
        </WidgetCard>

        <WidgetCard title="Actividad del mes" icon="pi pi-calendar-plus">
          <p class="lead-sub">Días de {{ monthLabel }}</p>
          <div class="bars-wrap">
            <Chart v-if="month" type="bar" :data="monthData" :options="monthOptions" />
          </div>
        </WidgetCard>

        <WidgetCard title="Fichajes en horario" icon="pi pi-stopwatch">
          <template v-if="hasPunctualityData">
            <p class="lead">
              <strong>{{ punctuality!.onSchedulePercent }}%</strong> dentro de horario
              <small>margen de ±{{ punctuality!.toleranceMinutes }} min · {{ monthLabel }}</small>
            </p>
            <div class="bar-wrap">
              <Chart type="bar" :data="punctualityData" :options="punctualityOptions" />
            </div>
            <ul class="legend row">
              <li><i class="dot" :style="{ background: SERIES.aqua }" aria-hidden="true"></i><span>Dentro</span><b>{{ punctuality!.onScheduleCount }}</b></li>
              <li><i class="dot" :style="{ background: SERIES.orange }" aria-hidden="true"></i><span>Fuera</span><b>{{ punctuality!.offScheduleCount }}</b></li>
            </ul>
            <p class="detail">
              {{ punctuality!.lateInCount }} entradas tarde · {{ punctuality!.earlyOutCount }} salidas anticipadas
            </p>
          </template>
          <p v-else class="empty">
            Sin jornadas comparables este mes: hacen falta fichajes cerrados de empleados con horario asignado.
          </p>
        </WidgetCard>
      </div>
    </template>
  </section>
</template>

<style scoped>
.planning { display: flex; flex-direction: column; gap: 1.25rem; }
.loading { display: grid; place-items: center; padding: 2rem; }

.charts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 1.25rem;
  align-items: stretch;   /* las tres tarjetas a la misma altura */
}
.charts > * { display: flex; flex-direction: column; }

.chart-wrap { position: relative; height: 160px; }
.centre {
  position: absolute; inset: 0;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  pointer-events: none;
}
.centre strong { font-size: 1.6rem; font-weight: 700; color: var(--hria-heading); line-height: 1; }
.centre small { font-size: 0.72rem; color: var(--hria-muted); }

.bar-wrap { height: 40px; }
.bars-wrap { height: 150px; }

.lead { margin: 0 0 0.5rem; font-size: 0.85rem; color: var(--hria-muted); }
.lead strong { font-size: 1.5rem; font-weight: 700; color: var(--hria-heading); }
.lead small { display: block; font-size: 0.75rem; }
.lead-sub { margin: 0 0 0.6rem; font-size: 0.78rem; color: var(--hria-muted); }
.detail { margin: 0.5rem 0 0; font-size: 0.75rem; color: var(--hria-muted-2); }

.legend { list-style: none; margin: 0.6rem 0 0; padding: 0; display: flex; flex-direction: column; gap: 0.3rem; }
.legend.row { flex-direction: row; flex-wrap: wrap; gap: 0.9rem; }
.legend li { display: flex; align-items: center; gap: 0.4rem; font-size: 0.78rem; color: var(--hria-muted); }
.legend li span { flex: 1; }
.legend li b { font-weight: 600; color: var(--hria-strong); }
.dot { width: 9px; height: 9px; border-radius: 2px; flex: none; display: inline-block; }

.weeks { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
.weeks th {
  text-align: left; font-weight: 600; font-size: 0.75rem;
  color: var(--hria-muted); padding: 0.35rem 0.5rem;
  border-bottom: 1px solid var(--hria-border-strong);
}
.weeks td { padding: 0.45rem 0.5rem; border-bottom: 1px solid var(--hria-divider); }
.weeks tr:last-child td { border-bottom: none; }
.weeks .num { text-align: right; font-variant-numeric: tabular-nums; }
.weeks .num .zero { color: var(--hria-muted-2); }
.nowrap { white-space: nowrap; }
.emp { display: flex; align-items: center; gap: 0.5rem; }
.emp-name { display: block; color: var(--hria-strong); }
.emp small { color: var(--hria-muted-2); font-size: 0.72rem; }
.type { display: inline-flex; align-items: center; gap: 0.4rem; }

.empty { padding: 1.25rem; text-align: center; color: var(--hria-muted-2); font-size: 0.88rem; margin: 0; }
</style>
