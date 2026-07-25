<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import ScheduleFormDialog from './ScheduleFormDialog.vue'
import { schedulesApi } from './api'
import type { ScheduleAssignment, ScheduleDetail, ScheduleListItem } from './types'
import { employeesApi } from '@/features/employees/api'
import type { EmployeeListItem } from '@/features/employees/types'
import type { ApiError } from '@/shared/http/client'

const toast = useToast()

const schedules = ref<ScheduleListItem[]>([])
const assignments = ref<ScheduleAssignment[]>([])
const employees = ref<EmployeeListItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

const formVisible = ref(false)
const editing = ref<ScheduleDetail | null>(null)

const assignVisible = ref(false)
const assignScheduleId = ref<number | null>(null)
const assignEmployeeId = ref<number | null>(null)
const assignStart = ref(new Date().toISOString().slice(0, 10))
const assignEnd = ref('')
const assignError = ref<string | null>(null)
const assigning = ref(false)

const employeeOptions = computed(() =>
  employees.value.map((e) => ({ label: e.fullName, value: e.id })),
)
const scheduleOptions = computed(() =>
  schedules.value.filter((s) => s.isActive).map((s) => ({ label: s.name, value: s.id })),
)

function formatHours(minutes: number): string {
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return m === 0 ? `${h} h` : `${h} h ${m} min`
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [list, assigns, emps] = await Promise.all([
      schedulesApi.list(true),
      schedulesApi.assignments(),
      employeesApi.list({ page: 1, pageSize: 100, isActive: true }),
    ])
    schedules.value = list
    assignments.value = assigns
    employees.value = emps.items
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar los horarios.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  formVisible.value = true
}

async function openEdit(row: ScheduleListItem) {
  try {
    editing.value = await schedulesApi.get(row.id)
    formVisible.value = true
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 4000 })
  }
}

async function deactivate(row: ScheduleListItem) {
  try {
    await schedulesApi.deactivate(row.id)
    toast.add({ severity: 'success', summary: 'Horario desactivado', life: 3000 })
    await load()
  } catch (e) {
    toast.add({
      severity: 'warn',
      summary: 'No se pudo desactivar',
      detail: (e as ApiError).title,
      life: 6000,
    })
  }
}

function openAssign(scheduleId?: number) {
  assignScheduleId.value = scheduleId ?? null
  assignEmployeeId.value = null
  assignStart.value = new Date().toISOString().slice(0, 10)
  assignEnd.value = ''
  assignError.value = null
  assignVisible.value = true
}

async function saveAssignment() {
  assignError.value = null
  if (!assignScheduleId.value || !assignEmployeeId.value) {
    assignError.value = 'Selecciona el horario y el empleado.'
    return
  }
  assigning.value = true
  try {
    await schedulesApi.assign({
      scheduleId: assignScheduleId.value,
      employeeId: assignEmployeeId.value,
      startDate: assignStart.value,
      endDate: assignEnd.value || null,
    })
    toast.add({ severity: 'success', summary: 'Horario asignado', life: 3000 })
    assignVisible.value = false
    await load()
  } catch (e) {
    assignError.value = (e as ApiError).title ?? 'No se pudo asignar el horario.'
  } finally {
    assigning.value = false
  }
}

async function removeAssignment(row: ScheduleAssignment) {
  try {
    await schedulesApi.removeAssignment(row.id)
    toast.add({ severity: 'success', summary: 'Asignación eliminada', life: 3000 })
    await load()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 5000 })
  }
}

async function onSaved() {
  toast.add({ severity: 'success', summary: 'Horario guardado', life: 3000 })
  await load()
}

onMounted(load)
</script>

<template>
  <section class="schedules">
    <header class="head">
      <div>
        <h1>Horarios</h1>
        <p>Plantillas de jornada que se asignan a los empleados durante un periodo.</p>
      </div>
      <div class="actions">
        <Button label="Asignar" icon="pi pi-user-plus" severity="secondary" outlined @click="openAssign()" />
        <Button label="Nuevo horario" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="false">
      {{ error }}
      <Button label="Reintentar" text size="small" @click="load" />
    </Message>

    <div v-if="loading" class="loading"><ProgressSpinner style="width: 44px; height: 44px" /></div>

    <template v-else>
      <DataTable :value="schedules" dataKey="id" responsiveLayout="scroll" class="card-table">
        <template #empty>
          <p class="empty">Todavía no hay horarios. Crea el primero con «Nuevo horario».</p>
        </template>
        <Column field="name" header="Nombre" />
        <Column header="Jornada semanal">
          <template #body="{ data }">{{ formatHours(data.weeklyMinutes) }}</template>
        </Column>
        <Column header="Tramos">
          <template #body="{ data }">{{ data.slotCount }}</template>
        </Column>
        <Column header="Asignados">
          <template #body="{ data }">{{ data.assignedEmployees }}</template>
        </Column>
        <Column header="Estado">
          <template #body="{ data }">
            <Tag :severity="data.isActive ? 'success' : 'secondary'" :value="data.isActive ? 'Activo' : 'Inactivo'" />
          </template>
        </Column>
        <Column header="" style="width: 8rem">
          <template #body="{ data }">
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              v-if="data.isActive"
              icon="pi pi-ban"
              text
              rounded
              severity="danger"
              aria-label="Desactivar"
              @click="deactivate(data)"
            />
          </template>
        </Column>
      </DataTable>

      <h2>Asignaciones</h2>
      <DataTable :value="assignments" dataKey="id" responsiveLayout="scroll" class="card-table">
        <template #empty>
          <p class="empty">Ningún empleado tiene horario asignado todavía.</p>
        </template>
        <Column field="employeeName" header="Empleado" />
        <Column field="scheduleName" header="Horario" />
        <Column header="Desde">
          <template #body="{ data }">{{ data.startDate }}</template>
        </Column>
        <Column header="Hasta">
          <template #body="{ data }">{{ data.endDate ?? 'Indefinido' }}</template>
        </Column>
        <Column header="Vigente">
          <template #body="{ data }">
            <Tag :severity="data.isCurrent ? 'success' : 'secondary'" :value="data.isCurrent ? 'Sí' : 'No'" />
          </template>
        </Column>
        <Column header="" style="width: 5rem">
          <template #body="{ data }">
            <Button
              icon="pi pi-trash"
              text
              rounded
              severity="danger"
              aria-label="Eliminar asignación"
              @click="removeAssignment(data)"
            />
          </template>
        </Column>
      </DataTable>
    </template>

    <ScheduleFormDialog v-model:visible="formVisible" :schedule="editing" @saved="onSaved" />

    <Dialog v-model:visible="assignVisible" modal header="Asignar horario" :style="{ width: '28rem' }">
      <div class="form">
        <div class="field">
          <label for="as-sched">Horario</label>
          <Select
            id="as-sched"
            v-model="assignScheduleId"
            :options="scheduleOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecciona un horario"
            fluid
          />
        </div>
        <div class="field">
          <label for="as-emp">Empleado</label>
          <Select
            id="as-emp"
            v-model="assignEmployeeId"
            :options="employeeOptions"
            optionLabel="label"
            optionValue="value"
            placeholder="Selecciona un empleado"
            filter
            fluid
          />
        </div>
        <div class="field">
          <label for="as-start">Desde</label>
          <input id="as-start" type="date" v-model="assignStart" class="date" />
        </div>
        <div class="field">
          <label for="as-end">Hasta (opcional)</label>
          <input id="as-end" type="date" v-model="assignEnd" class="date" />
          <small>Déjalo vacío para una asignación indefinida.</small>
        </div>
        <Message v-if="assignError" severity="error" :closable="false">{{ assignError }}</Message>
      </div>
      <template #footer>
        <Button label="Cancelar" text @click="assignVisible = false" />
        <Button label="Asignar" icon="pi pi-check" :loading="assigning" @click="saveAssignment" />
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.schedules { display: flex; flex-direction: column; gap: 1.25rem; }
.head { display: flex; align-items: start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.head h1 { margin: 0; font-size: 1.5rem; }
.head p { margin: 0.25rem 0 0; color: var(--hria-muted); font-size: 0.9rem; }
.actions { display: flex; gap: 0.5rem; }
h2 { margin: 0.5rem 0 0; font-size: 1.1rem; }
.loading { display: grid; place-items: center; padding: 3rem; }
.empty { padding: 1.5rem; text-align: center; color: var(--hria-muted-2); }
.card-table { background: var(--hria-surface); border: 1px solid var(--hria-border); border-radius: 16px; overflow: hidden; }
.form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.4rem; }
.field label { font-weight: 600; font-size: 0.9rem; }
.field small { color: var(--hria-muted); font-size: 0.78rem; }
.date {
  padding: 0.55rem 0.7rem; border-radius: 8px; font: inherit;
  border: 1px solid var(--hria-border-strong);
  background: var(--hria-surface); color: var(--hria-text);
}
</style>
