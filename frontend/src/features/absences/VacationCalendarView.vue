<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import Button from 'primevue/button'
import Select from 'primevue/select'
import SelectButton from 'primevue/selectbutton'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import { absencesApi } from './api'
import { AbsenceStatus, type CalendarAbsence, type EmployeeYearAbsences, type VacationCalendar } from './types'
import { MONTH_NAMES, type CalendarDay } from '@/features/work-calendar/types'
import { workCalendarApi } from '@/features/work-calendar/api'
import type { ApiError } from '@/shared/http/client'

const today = new Date()
const currentYear = today.getFullYear()
const todayIso = today.toISOString().slice(0, 10)

type ViewMode = 'month' | 'quarter'

const view = ref<ViewMode>('month')
const viewOptions = [
  { label: 'Mes', value: 'month' as ViewMode },
  { label: 'Trimestre', value: 'quarter' as ViewMode },
]

const year = ref(currentYear)
const month = ref(today.getMonth())            // 0-11
const quarter = ref(Math.floor(today.getMonth() / 3)) // 0-3
const search = ref('')

const yearOptions = Array.from({ length: 5 }, (_, i) => currentYear - 1 + i).map((y) => ({
  label: String(y),
  value: y,
}))
const monthOptions = MONTH_NAMES.map((label, value) => ({ label, value }))
const quarterOptions = [
  { label: 'T1 · Ene–Mar', value: 0 },
  { label: 'T2 · Abr–Jun', value: 1 },
  { label: 'T3 · Jul–Sep', value: 2 },
  { label: 'T4 · Oct–Dic', value: 3 },
]

const calendar = ref<VacationCalendar | null>(null)
const nonWorkingDates = ref<Set<string>>(new Set())
const loading = ref(false)
const error = ref<string | null>(null)

// ------------------------------------------------------------------
// Rango visible
// ------------------------------------------------------------------

/** Primer y último mes (0-11) del rango, según la vista activa. */
const monthRange = computed<[number, number]>(() =>
  view.value === 'month' ? [month.value, month.value] : [quarter.value * 3, quarter.value * 3 + 2],
)

interface DayColumn {
  iso: string
  day: number
  month: number
  weekday: string
  isFirstOfMonth: boolean
  isNonWorking: boolean
  isToday: boolean
}

const WEEKDAY_INITIALS = ['D', 'L', 'M', 'X', 'J', 'V', 'S']

/**
 * Las columnas son la única fuente de verdad de la rejilla: las cabeceras, las
 * celdas y las barras se posicionan sobre este mismo array, así no puede haber
 * desajustes entre ellas.
 */
const columns = computed<DayColumn[]>(() => {
  const [fromMonth, toMonth] = monthRange.value
  const out: DayColumn[] = []

  for (let m = fromMonth; m <= toMonth; m++) {
    const days = new Date(Date.UTC(year.value, m + 1, 0)).getUTCDate()
    for (let d = 1; d <= days; d++) {
      const iso = `${year.value}-${String(m + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`
      out.push({
        iso,
        day: d,
        month: m,
        weekday: WEEKDAY_INITIALS[new Date(Date.UTC(year.value, m, d)).getUTCDay()],
        isFirstOfMonth: d === 1,
        isNonWorking: nonWorkingDates.value.has(iso),
        isToday: iso === todayIso,
      })
    }
  }
  return out
})

/** Índice de cada fecha dentro de la rejilla, para colocar las barras. */
const indexByIso = computed(() => {
  const map = new Map<string, number>()
  columns.value.forEach((c, i) => map.set(c.iso, i))
  return map
})

/** Bandas de mes de la cabecera del trimestre. */
const monthBands = computed(() => {
  const bands: { name: string; span: number }[] = []
  for (const col of columns.value) {
    if (col.isFirstOfMonth) bands.push({ name: MONTH_NAMES[col.month], span: 0 })
    bands[bands.length - 1].span++
  }
  return bands
})

/** En trimestre no caben 91 números: se rotulan solo algunos días. */
function showsNumber(col: DayColumn): boolean {
  return view.value === 'month' || col.day === 1 || col.day % 5 === 0
}

const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${columns.value.length}, minmax(${view.value === 'month' ? 30 : 11}px, 1fr))`,
}))

// El periodo visible ya lo indican los desplegables de año y mes/trimestre,
// así que no hace falta rotularlo aparte.

// ------------------------------------------------------------------
// Navegación
// ------------------------------------------------------------------

function shift(delta: number) {
  if (view.value === 'month') {
    const m = month.value + delta
    if (m < 0) { month.value = 11; year.value-- }
    else if (m > 11) { month.value = 0; year.value++ }
    else month.value = m
    quarter.value = Math.floor(month.value / 3)
  } else {
    const q = quarter.value + delta
    if (q < 0) { quarter.value = 3; year.value-- }
    else if (q > 3) { quarter.value = 0; year.value++ }
    else quarter.value = q
    month.value = quarter.value * 3
  }
}

function goToday() {
  year.value = currentYear
  month.value = today.getMonth()
  quarter.value = Math.floor(today.getMonth() / 3)
}

// Al cambiar de vista se mantiene el periodo: el mes elegido decide el
// trimestre y viceversa, para no perder el contexto al alternar.
watch(view, (mode) => {
  if (mode === 'quarter') quarter.value = Math.floor(month.value / 3)
  else month.value = quarter.value * 3
})

// ------------------------------------------------------------------
// Datos
// ------------------------------------------------------------------

const rows = computed(() => {
  const list = calendar.value?.employees ?? []
  const term = search.value.trim().toLowerCase()
  return term
    ? list.filter(
        (e) => e.employeeName.toLowerCase().includes(term) || e.departmentName.toLowerCase().includes(term),
      )
    : list
})

interface Bar extends CalendarAbsence {
  startIndex: number
  span: number
  continuesBefore: boolean
  continuesAfter: boolean
}

function barsFor(row: EmployeeYearAbsences): Bar[] {
  const cols = columns.value
  if (cols.length === 0) return []

  const first = cols[0].iso
  const last = cols[cols.length - 1].iso

  return row.absences
    .filter((a) => a.startDate.slice(0, 10) <= last && first <= a.endDate.slice(0, 10))
    .map((a) => {
      const s = a.startDate.slice(0, 10)
      const e = a.endDate.slice(0, 10)
      const startIndex = indexByIso.value.get(s < first ? first : s) ?? 0
      const endIndex = indexByIso.value.get(e > last ? last : e) ?? cols.length - 1
      return {
        ...a,
        startIndex,
        span: endIndex - startIndex + 1,
        continuesBefore: s < first,
        continuesAfter: e > last,
      }
    })
    .sort((a, b) => a.startIndex - b.startIndex)
}

function barStyle(bar: Bar) {
  const color = bar.colorHex ?? '#16b98a'
  return {
    gridColumn: `${bar.startIndex + 1} / span ${bar.span}`,
    background: color,
    // Las pendientes se atenúan y llevan borde discontinuo: hay que poder
    // distinguir de un vistazo lo que aún está por decidir.
    opacity: bar.status === AbsenceStatus.Approved ? '1' : '0.55',
    borderStyle: bar.status === AbsenceStatus.Approved ? 'solid' : 'dashed',
    borderTopLeftRadius: bar.continuesBefore ? '0' : '999px',
    borderBottomLeftRadius: bar.continuesBefore ? '0' : '999px',
    borderTopRightRadius: bar.continuesAfter ? '0' : '999px',
    borderBottomRightRadius: bar.continuesAfter ? '0' : '999px',
  }
}

function barTitle(row: EmployeeYearAbsences, bar: Bar): string {
  const estado = bar.status === AbsenceStatus.Approved ? 'aprobada' : 'pendiente'
  return `${row.employeeName} · ${bar.absenceTypeName} (${estado})\n${bar.startDate.slice(0, 10)} – ${bar.endDate.slice(0, 10)} · ${bar.workingDays} días`
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [data, days] = await Promise.all([
      absencesApi.vacationCalendar(year.value),
      workCalendarApi.days(year.value),
    ])
    calendar.value = data
    nonWorkingDates.value = new Set(
      days.filter((d: CalendarDay) => !d.isWorkingDay).map((d: CalendarDay) => d.date.slice(0, 10)),
    )
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cargar el calendario de vacaciones.'
  } finally {
    loading.value = false
  }
}

// El año trae todas las ausencias de una vez: cambiar de mes o de trimestre no
// vuelve a consultar al servidor.
watch(year, load)
onMounted(load)
</script>

<template>
  <section class="vc">
    <header class="head">
      <div>
        <h1>Calendario de vacaciones</h1>
        <p>Ausencias de la plantilla por mes o por trimestre.</p>
      </div>
      <div class="actions">
        <SelectButton v-model="view" :options="viewOptions" optionLabel="label" optionValue="value" :allowEmpty="false" aria-label="Vista" />
        <Select v-model="year" :options="yearOptions" optionLabel="label" optionValue="value" aria-label="Año" />
        <Select
          v-if="view === 'month'"
          v-model="month"
          :options="monthOptions"
          optionLabel="label"
          optionValue="value"
          aria-label="Mes"
          class="period"
        />
        <Select
          v-else
          v-model="quarter"
          :options="quarterOptions"
          optionLabel="label"
          optionValue="value"
          aria-label="Trimestre"
          class="period"
        />
        <Button icon="pi pi-chevron-left" text rounded aria-label="Periodo anterior" @click="shift(-1)" />
        <Button icon="pi pi-chevron-right" text rounded aria-label="Periodo siguiente" @click="shift(1)" />
        <Button label="Hoy" size="small" outlined severity="secondary" @click="goToday" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="false">
      {{ error }}
      <Button label="Reintentar" text size="small" @click="load" />
    </Message>

    <div v-if="loading" class="loading"><ProgressSpinner style="width: 44px; height: 44px" /></div>

    <template v-else>
      <div class="legend">
        <span><i class="swatch" style="background: #16b98a" aria-hidden="true"></i> Vacaciones</span>
        <span><i class="swatch" style="background: #f43f5e" aria-hidden="true"></i> Enfermedad</span>
        <span><i class="swatch" style="background: #f59e0b" aria-hidden="true"></i> Asuntos propios</span>
        <span><i class="swatch" style="background: #3b82f6" aria-hidden="true"></i> Permiso</span>
        <span><i class="swatch pending" aria-hidden="true"></i> Pendiente de aprobar</span>
      </div>

      <div class="board" :class="view">
        <!-- Banda de meses: solo aporta en la vista de trimestre -->
        <div v-if="view === 'quarter'" class="row band-row">
          <div class="name-col" aria-hidden="true"></div>
          <div class="days" :style="gridStyle">
            <span v-for="(b, i) in monthBands" :key="i" class="band" :style="{ gridColumn: `span ${b.span}` }">
              {{ b.name }}
            </span>
          </div>
        </div>

        <!-- Cabecera: buscador + días -->
        <div class="row header-row">
          <div class="name-col">
            <InputText v-model="search" placeholder="Buscar empleados" class="search" aria-label="Buscar empleados" />
          </div>
          <div class="days" :style="gridStyle">
            <span
              v-for="col in columns"
              :key="col.iso"
              class="day-head"
              :class="{ nonworking: col.isNonWorking, today: col.isToday, 'month-start': col.isFirstOfMonth }"
              :title="col.iso"
            >
              <small v-if="view === 'month'">{{ col.weekday }}</small>
              <b v-if="showsNumber(col)">{{ col.day }}</b>
            </span>
          </div>
        </div>

        <!-- Una fila por empleado -->
        <div v-for="row in rows" :key="row.employeeId" class="row">
          <div class="name-col">
            <UserAvatar :name="row.employeeName" :size="28" />
            <span class="who">
              <span class="emp-name">{{ row.employeeName }}</span>
              <small>{{ row.departmentName }}</small>
            </span>
          </div>
          <div class="days" :style="gridStyle">
            <!--
              La columna va explícita en cada celda. Sin ella, el autoposicionado
              de CSS Grid las coloca en el primer hueco libre y las barras, que sí
              llevan columna fija, se lo comen: en una fila con una ausencia del 6
              al 17, el día 11 acababa en la columna 23 y la rejilla generaba 43
              columnas en vez de 31. Descuadraba esa fila respecto a las demás.
            -->
            <span
              v-for="(col, i) in columns"
              :key="col.iso"
              class="cell"
              :style="{ gridColumn: i + 1 }"
              :class="{ nonworking: col.isNonWorking, today: col.isToday, 'month-start': col.isFirstOfMonth }"
            />
            <span
              v-for="bar in barsFor(row)"
              :key="bar.id"
              class="bar"
              :style="barStyle(bar)"
              :title="barTitle(row, bar)"
            >
              {{ bar.absenceTypeName }}
            </span>
          </div>
        </div>

        <p v-if="rows.length === 0" class="empty">
          {{ search ? 'Ningún empleado coincide con la búsqueda.' : 'No hay empleados activos.' }}
        </p>
      </div>
    </template>
  </section>
</template>

<style scoped>
.vc { display: flex; flex-direction: column; gap: 1.25rem; }
.head { display: flex; align-items: start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.head h1 { margin: 0; font-size: 1.5rem; }
.head p { margin: 0.25rem 0 0; color: var(--hria-muted); font-size: 0.9rem; }
.actions { display: flex; gap: 0.35rem; align-items: center; flex-wrap: wrap; }
.period { min-width: 10rem; }
.loading { display: grid; place-items: center; padding: 3rem; }

.legend { display: flex; gap: 1.1rem; flex-wrap: wrap; font-size: 0.8rem; color: var(--hria-muted); }
.legend span { display: inline-flex; align-items: center; gap: 0.4rem; }
.swatch { width: 13px; height: 13px; border-radius: 4px; display: inline-block; }
.swatch.pending { background: #16b98a; opacity: 0.55; border: 1px dashed var(--hria-heading); }

.board {
  background: var(--hria-surface);
  border: 1px solid var(--hria-border);
  border-radius: 16px;
  overflow-x: auto;
}
.row { display: flex; align-items: stretch; border-bottom: 1px solid var(--hria-divider); }
.row:last-of-type { border-bottom: none; }
.header-row { position: sticky; top: 0; background: var(--hria-surface-2); z-index: 2; }
.band-row { background: var(--hria-surface-2); }

.name-col {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  width: 210px;
  min-width: 210px;
  padding: 0.45rem 0.75rem;
  position: sticky;
  left: 0;
  background: inherit;
  border-right: 1px solid var(--hria-border);
  z-index: 1;
}
.row:not(.header-row):not(.band-row) .name-col { background: var(--hria-surface); }
.who { display: flex; flex-direction: column; line-height: 1.2; min-width: 0; }
.emp-name {
  font-size: 0.85rem; font-weight: 500; color: var(--hria-strong);
  white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
}
.who small { color: var(--hria-muted-2); font-size: 0.72rem; }
.search { width: 100%; font-size: 0.85rem; }

.days { display: grid; flex: 1; min-width: 0; }

.band {
  grid-row: 1;
  text-align: center;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--hria-muted);
  padding: 0.25rem 0;
  border-left: 1px solid var(--hria-border-strong);
}

.day-head {
  grid-row: 1;
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 0.3rem 0;
  border-left: 1px solid var(--hria-divider);
  font-size: 0.75rem;
  min-height: 34px;
}
.day-head small { color: var(--hria-muted-2); font-size: 0.62rem; text-transform: uppercase; }
.day-head b { font-weight: 600; color: var(--hria-text); }
.day-head.nonworking { background: var(--hria-surface-3); }
.day-head.today b {
  background: var(--hria-accent); color: #fff;
  width: 20px; height: 20px; border-radius: 50%;
  display: grid; place-items: center;
}

.cell {
  grid-row: 1;
  min-height: 38px;
  border-left: 1px solid var(--hria-divider);
}
.cell.nonworking { background: var(--hria-surface-3); }
.cell.today { box-shadow: inset 1px 0 0 var(--hria-accent), inset -1px 0 0 var(--hria-accent); }

/* Separador de mes en la vista de trimestre. */
.quarter .cell.month-start,
.quarter .day-head.month-start { border-left: 1px solid var(--hria-border-strong); }

/* La barra se superpone a las celdas ocupando su rango de días. */
.bar {
  grid-row: 1;
  align-self: center;
  height: 24px;
  margin: 0 2px;
  padding: 0 0.5rem;
  display: flex;
  align-items: center;
  color: #fff;
  font-size: 0.72rem;
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  border-width: 1px;
  border-color: rgba(255, 255, 255, 0.6);
  cursor: default;
}
/* En trimestre las columnas son estrechas: la etiqueta se reduce y las barras
   de un solo día quedan como marca de color, legibles por el título. */
.quarter .bar { height: 20px; font-size: 0.66rem; padding: 0 0.3rem; margin: 0 1px; }

.empty { padding: 2rem; text-align: center; color: var(--hria-muted-2); }

@media (max-width: 720px) {
  .name-col { width: 150px; min-width: 150px; }
}
</style>
