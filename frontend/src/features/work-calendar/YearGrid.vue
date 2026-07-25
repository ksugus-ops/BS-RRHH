<script setup lang="ts">
import { computed } from 'vue'
import { MONTH_NAMES, type CalendarDay } from './types'

const props = defineProps<{
  year: number
  days: CalendarDay[]
  /** Si es cierto, los días son pulsables (para marcar festivos). */
  interactive?: boolean
}>()

const emit = defineEmits<{ dayClick: [CalendarDay] }>()

const byDate = computed(() => {
  const map = new Map<string, CalendarDay>()
  for (const d of props.days) map.set(d.date.slice(0, 10), d)
  return map
})

/**
 * Cada mes como una rejilla de 7 columnas empezando en lunes.
 * Los huecos iniciales se rellenan con null para que los días caigan en su
 * columna correcta.
 */
const months = computed(() =>
  MONTH_NAMES.map((name, monthIndex) => {
    const first = new Date(Date.UTC(props.year, monthIndex, 1))
    const daysInMonth = new Date(Date.UTC(props.year, monthIndex + 1, 0)).getUTCDate()

    // getUTCDay(): 0 = domingo. Se convierte a 0 = lunes.
    const leading = (first.getUTCDay() + 6) % 7

    const cells: (CalendarDay | null)[] = Array(leading).fill(null)
    for (let day = 1; day <= daysInMonth; day++) {
      const iso = `${props.year}-${String(monthIndex + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
      cells.push(byDate.value.get(iso) ?? null)
    }
    return { name, cells }
  }),
)

function dayNumber(day: CalendarDay): number {
  return Number(day.date.slice(8, 10))
}

function title(day: CalendarDay): string {
  if (day.holidayName) return `${day.date.slice(0, 10)} — ${day.holidayName}`
  if (day.isWeekend) return `${day.date.slice(0, 10)} — no laborable`
  return day.date.slice(0, 10)
}

function onClick(day: CalendarDay | null) {
  if (day && props.interactive) emit('dayClick', day)
}
</script>

<template>
  <div class="year-grid">
    <div v-for="month in months" :key="month.name" class="month">
      <h3>{{ month.name }}</h3>
      <div class="weekdays" aria-hidden="true">
        <span>L</span><span>M</span><span>X</span><span>J</span><span>V</span><span>S</span><span>D</span>
      </div>
      <div class="days">
        <template v-for="(cell, i) in month.cells" :key="i">
          <span v-if="!cell" class="cell empty" />
          <component
            v-else
            :is="interactive ? 'button' : 'span'"
            :type="interactive ? 'button' : undefined"
            class="cell"
            :class="{
              weekend: cell.isWeekend,
              holiday: !!cell.holidayName,
              clickable: interactive,
            }"
            :title="title(cell)"
            :aria-label="title(cell)"
            @click="onClick(cell)"
          >
            {{ dayNumber(cell) }}
          </component>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.year-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
  gap: 1rem;
}
.month {
  background: var(--hria-surface);
  border: 1px solid var(--hria-border);
  border-radius: 14px;
  padding: 0.75rem;
}
.month h3 { margin: 0 0 0.5rem; font-size: 0.95rem; text-align: center; }
.weekdays, .days {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 2px;
}
.weekdays span {
  text-align: center;
  font-size: 0.7rem;
  font-weight: 600;
  color: var(--hria-muted-2);
  padding-bottom: 0.25rem;
}
.cell {
  aspect-ratio: 1;
  display: grid;
  place-items: center;
  font-size: 0.75rem;
  border-radius: 6px;
  border: none;
  background: transparent;
  color: var(--hria-text);
  font-family: inherit;
  padding: 0;
}
.cell.empty { visibility: hidden; }
.cell.clickable { cursor: pointer; }
.cell.clickable:hover { outline: 2px solid var(--hria-accent); outline-offset: -2px; }
.cell.weekend { background: var(--hria-surface-3); color: var(--hria-muted-2); }
.cell.holiday { background: var(--hria-accent); color: #fff; font-weight: 700; }
</style>
