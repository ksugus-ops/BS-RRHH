<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import YearGrid from './YearGrid.vue'
import { workCalendarApi } from './api'
import {
  HOLIDAY_KINDS,
  HolidayKind,
  holidayKindLabel,
  type CalendarDay,
  type WorkCalendarDetail,
} from './types'
import { WEEKDAYS } from '@/features/schedules/types'
import type { ApiError } from '@/shared/http/client'

const toast = useToast()

const currentYear = new Date().getFullYear()
const year = ref(currentYear)
const yearOptions = Array.from({ length: 7 }, (_, i) => currentYear - 2 + i).map((y) => ({ label: String(y), value: y }))

const calendar = ref<WorkCalendarDetail | null>(null)
const days = ref<CalendarDay[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)

const holidayVisible = ref(false)
const holidayDate = ref('')
const holidayName = ref('')
const holidayKind = ref<HolidayKind>(HolidayKind.Convenio)
const holidayError = ref<string | null>(null)

const nonWorking = ref<number[]>([])

const holidayCount = computed(() => calendar.value?.holidays.length ?? 0)

async function load() {
  loading.value = true
  error.value = null
  try {
    const [detail, list] = await Promise.all([workCalendarApi.byYear(year.value), workCalendarApi.days(year.value)])
    calendar.value = detail
    days.value = list
    nonWorking.value = detail ? [...detail.nonWorkingWeekDays] : [6, 0]
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cargar el calendario laboral.'
  } finally {
    loading.value = false
  }
}

async function createCalendar() {
  saving.value = true
  try {
    await workCalendarApi.create({
      year: year.value,
      name: `Calendario laboral ${year.value}`,
      nonWorkingWeekDays: nonWorking.value,
    })
    toast.add({ severity: 'success', summary: `Calendario ${year.value} creado`, life: 3000 })
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 5000 })
  } finally {
    saving.value = false
  }
}

function toggleWeekday(value: number) {
  nonWorking.value = nonWorking.value.includes(value)
    ? nonWorking.value.filter((d) => d !== value)
    : [...nonWorking.value, value]
}

async function saveWeekdays() {
  if (!calendar.value) return
  if (nonWorking.value.length === 7) {
    toast.add({ severity: 'warn', summary: 'No puedes marcar los siete días como no laborables', life: 5000 })
    return
  }
  saving.value = true
  try {
    await workCalendarApi.update(calendar.value.id, {
      name: calendar.value.name,
      isActive: calendar.value.isActive,
      nonWorkingWeekDays: nonWorking.value,
    })
    toast.add({ severity: 'success', summary: 'Días no laborables actualizados', life: 3000 })
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 5000 })
  } finally {
    saving.value = false
  }
}

function openHoliday(day?: CalendarDay) {
  holidayDate.value = day ? day.date.slice(0, 10) : `${year.value}-01-01`
  holidayName.value = day?.holidayName ?? ''
  holidayKind.value = HolidayKind.Convenio
  holidayError.value = null
  holidayVisible.value = true
}

/** Al pulsar un día del calendario: si ya es festivo se ofrece quitarlo. */
async function onDayClick(day: CalendarDay) {
  if (!calendar.value) {
    toast.add({ severity: 'info', summary: `Crea primero el calendario de ${year.value}`, life: 4000 })
    return
  }
  const existing = calendar.value.holidays.find((h) => h.date.slice(0, 10) === day.date.slice(0, 10))
  if (existing) {
    await removeHoliday(existing.id, existing.name)
    return
  }
  openHoliday(day)
}

async function saveHoliday() {
  if (!calendar.value) return
  holidayError.value = null
  if (!holidayName.value.trim()) {
    holidayError.value = 'Indica el nombre del festivo.'
    return
  }
  saving.value = true
  try {
    await workCalendarApi.addHoliday(calendar.value.id, {
      date: holidayDate.value,
      name: holidayName.value.trim(),
      kind: holidayKind.value,
    })
    toast.add({ severity: 'success', summary: 'Festivo añadido', life: 3000 })
    holidayVisible.value = false
    await load()
  } catch (e) {
    holidayError.value = (e as ApiError).title ?? 'No se pudo añadir el festivo.'
  } finally {
    saving.value = false
  }
}

async function removeHoliday(id: number, name: string) {
  if (!calendar.value) return
  try {
    await workCalendarApi.removeHoliday(calendar.value.id, id)
    toast.add({ severity: 'success', summary: `Festivo «${name}» eliminado`, life: 3000 })
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 5000 })
  }
}

watch(year, load)
onMounted(load)
</script>

<template>
  <section class="wc">
    <header class="head">
      <div>
        <h1>Calendario laboral</h1>
        <p>Días no laborables de la semana y festivos del convenio, por año natural.</p>
      </div>
      <div class="actions">
        <Select v-model="year" :options="yearOptions" optionLabel="label" optionValue="value" aria-label="Año" />
        <Button
          v-if="calendar"
          label="Añadir festivo"
          icon="pi pi-plus"
          @click="openHoliday()"
        />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="false">
      {{ error }}
      <Button label="Reintentar" text size="small" @click="load" />
    </Message>

    <div v-if="loading" class="loading"><ProgressSpinner style="width: 44px; height: 44px" /></div>

    <template v-else>
      <Message v-if="!calendar" severity="info" :closable="false">
        No hay calendario laboral para {{ year }}. Mientras tanto se muestra el criterio por defecto
        (sábado y domingo no laborables).
        <Button label="Crear calendario" size="small" :loading="saving" @click="createCalendar" />
      </Message>

      <div v-if="calendar" class="panel">
        <div class="weekdays-config">
          <h2>Días no laborables de la semana</h2>
          <div class="day-toggles">
            <button
              v-for="d in WEEKDAYS"
              :key="d.value"
              type="button"
              class="day-toggle"
              :class="{ on: nonWorking.includes(d.value) }"
              :aria-pressed="nonWorking.includes(d.value)"
              :aria-label="d.label"
              @click="toggleWeekday(d.value)"
            >
              {{ d.short }}
            </button>
            <Button label="Guardar" size="small" :loading="saving" @click="saveWeekdays" />
          </div>
        </div>
        <dl class="stats">
          <div><dt>Días laborables</dt><dd>{{ calendar.workingDaysInYear }}</dd></div>
          <div><dt>Festivos</dt><dd>{{ holidayCount }}</dd></div>
        </dl>
      </div>

      <p class="hint">
        Pulsa un día del calendario para marcarlo como festivo, o para quitarlo si ya lo es.
      </p>

      <YearGrid :year="year" :days="days" interactive @dayClick="onDayClick" />

      <template v-if="calendar && calendar.holidays.length">
        <h2>Festivos de {{ year }}</h2>
        <DataTable :value="calendar.holidays" dataKey="id" responsiveLayout="scroll" class="card-table">
          <Column header="Fecha">
            <template #body="{ data }">{{ data.date.slice(0, 10) }}</template>
          </Column>
          <Column field="name" header="Nombre" />
          <Column header="Tipo">
            <template #body="{ data }">
              <Tag :value="holidayKindLabel(data.kind)" severity="secondary" />
            </template>
          </Column>
          <Column header="" style="width: 5rem">
            <template #body="{ data }">
              <Button
                icon="pi pi-trash"
                text
                rounded
                severity="danger"
                :aria-label="`Eliminar ${data.name}`"
                @click="removeHoliday(data.id, data.name)"
              />
            </template>
          </Column>
        </DataTable>
      </template>
    </template>

    <Dialog v-model:visible="holidayVisible" modal header="Nuevo festivo" :style="{ width: '26rem' }">
      <div class="form">
        <div class="field">
          <label for="h-date">Fecha</label>
          <input id="h-date" type="date" v-model="holidayDate" class="date" />
        </div>
        <div class="field">
          <label for="h-name">Nombre</label>
          <InputText id="h-name" v-model="holidayName" placeholder="Puente de convenio" fluid />
        </div>
        <div class="field">
          <label for="h-kind">Tipo</label>
          <Select
            id="h-kind"
            v-model="holidayKind"
            :options="HOLIDAY_KINDS"
            optionLabel="label"
            optionValue="value"
            fluid
          />
        </div>
        <Message v-if="holidayError" severity="error" :closable="false">{{ holidayError }}</Message>
      </div>
      <template #footer>
        <Button label="Cancelar" text @click="holidayVisible = false" />
        <Button label="Añadir" icon="pi pi-check" :loading="saving" @click="saveHoliday" />
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.wc { display: flex; flex-direction: column; gap: 1.25rem; }
.head { display: flex; align-items: start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.head h1 { margin: 0; font-size: 1.5rem; }
.head p { margin: 0.25rem 0 0; color: var(--hria-muted); font-size: 0.9rem; }
.actions { display: flex; gap: 0.5rem; align-items: center; }
h2 { margin: 0.5rem 0 0; font-size: 1.05rem; }
.loading { display: grid; place-items: center; padding: 3rem; }
.hint { margin: 0; color: var(--hria-muted); font-size: 0.85rem; }

.panel {
  display: flex; justify-content: space-between; gap: 1.5rem; flex-wrap: wrap;
  background: var(--hria-surface); border: 1px solid var(--hria-border);
  border-radius: 16px; padding: 1rem 1.25rem;
}
.weekdays-config h2 { margin: 0 0 0.5rem; }
.day-toggles { display: flex; gap: 0.4rem; align-items: center; flex-wrap: wrap; }
.day-toggle {
  width: 38px; height: 38px; border-radius: 50%; cursor: pointer;
  border: 1px solid var(--hria-border-strong); background: transparent;
  color: var(--hria-muted); font-weight: 700; font-size: 0.9rem;
}
.day-toggle.on { background: var(--hria-surface-3); border-color: var(--hria-muted-2); color: var(--hria-muted-2); }

.stats { display: flex; gap: 2rem; margin: 0; }
.stats div { text-align: center; }
.stats dt { font-size: 0.78rem; color: var(--hria-muted); }
.stats dd { margin: 0.15rem 0 0; font-size: 1.5rem; font-weight: 700; color: var(--hria-heading); }

.card-table { background: var(--hria-surface); border: 1px solid var(--hria-border); border-radius: 16px; overflow: hidden; }
.form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.4rem; }
.field label { font-weight: 600; font-size: 0.9rem; }
.date {
  padding: 0.55rem 0.7rem; border-radius: 8px; font: inherit;
  border: 1px solid var(--hria-border-strong);
  background: var(--hria-surface); color: var(--hria-text);
}
</style>
