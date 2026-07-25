<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import Textarea from 'primevue/textarea'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { useToast } from 'primevue/usetoast'
import { absencesApi, vacationsApi } from './api'
import {
  ABSENCE_STATUS_LABEL,
  ABSENCE_STATUS_SEVERITY,
  AbsenceStatus,
  type AbsenceRequest,
  type AbsenceType,
  type VacationBalance,
} from './types'
import { useAuthStore } from '@/stores/auth'
import type { ApiError } from '@/shared/http/client'

const toast = useToast()
const auth = useAuthStore()

const requests = ref<AbsenceRequest[]>([])
const types = ref<AbsenceType[]>([])
const balance = ref<VacationBalance | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

const statusFilter = ref<AbsenceStatus | null>(null)
const statusOptions = [
  { label: 'Todas', value: null },
  { label: 'Pendientes', value: AbsenceStatus.Pending },
  { label: 'Aprobadas', value: AbsenceStatus.Approved },
  { label: 'Rechazadas', value: AbsenceStatus.Rejected },
  { label: 'Retiradas', value: AbsenceStatus.Cancelled },
]

// --- Formulario de solicitud ---
const formVisible = ref(false)
const formTypeId = ref<number | null>(null)
const formStart = ref('')
const formEnd = ref('')
const formReason = ref('')
const formError = ref<string | null>(null)
const saving = ref(false)

const typeOptions = computed(() => types.value.map((t) => ({ label: t.name, value: t.id })))

const selectedType = computed(() => types.value.find((t) => t.id === formTypeId.value) ?? null)

async function load() {
  loading.value = true
  error.value = null
  try {
    const [list, catalogue] = await Promise.all([
      absencesApi.list({ status: statusFilter.value, pageSize: 100 }),
      absencesApi.types(),
    ])
    requests.value = list.items
    types.value = catalogue

    if (auth.user?.employeeId) {
      balance.value = await vacationsApi.balance(auth.user.employeeId)
    }
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar las ausencias.'
  } finally {
    loading.value = false
  }
}

function openForm() {
  const today = new Date().toISOString().slice(0, 10)
  formTypeId.value = types.value[0]?.id ?? null
  formStart.value = today
  formEnd.value = today
  formReason.value = ''
  formError.value = null
  formVisible.value = true
}

async function submit() {
  formError.value = null
  if (!formTypeId.value) {
    formError.value = 'Selecciona el tipo de ausencia.'
    return
  }
  if (formEnd.value < formStart.value) {
    formError.value = 'La fecha de fin no puede ser anterior a la de inicio.'
    return
  }
  saving.value = true
  try {
    await absencesApi.create({
      absenceTypeId: formTypeId.value,
      startDate: formStart.value,
      endDate: formEnd.value,
      reason: formReason.value.trim() || null,
    })
    toast.add({ severity: 'success', summary: 'Solicitud enviada', life: 3000 })
    formVisible.value = false
    await load()
  } catch (e) {
    formError.value = (e as ApiError).title ?? 'No se pudo crear la solicitud.'
  } finally {
    saving.value = false
  }
}

async function decide(row: AbsenceRequest, approve: boolean) {
  try {
    if (approve) await absencesApi.approve(row.id, null)
    else await absencesApi.reject(row.id, null)
    toast.add({
      severity: 'success',
      summary: approve ? 'Solicitud aprobada' : 'Solicitud rechazada',
      life: 3000,
    })
    await load()
  } catch (e) {
    toast.add({ severity: 'warn', summary: 'No se pudo resolver', detail: (e as ApiError).title, life: 6000 })
  }
}

async function cancel(row: AbsenceRequest) {
  try {
    await absencesApi.cancel(row.id)
    toast.add({ severity: 'success', summary: 'Solicitud retirada', life: 3000 })
    await load()
  } catch (e) {
    toast.add({ severity: 'warn', summary: 'No se pudo retirar', detail: (e as ApiError).title, life: 6000 })
  }
}

function canCancel(row: AbsenceRequest): boolean {
  return row.status === AbsenceStatus.Pending && row.employeeId === auth.user?.employeeId
}

onMounted(load)
</script>

<template>
  <section class="absences">
    <header class="head">
      <div>
        <h1>Ausencias y vacaciones</h1>
        <p v-if="auth.isAdmin">Solicitudes de toda la plantilla. Puedes aprobarlas o rechazarlas.</p>
        <p v-else>Tus solicitudes de ausencia y el saldo de vacaciones del año.</p>
      </div>
      <div class="actions">
        <Select
          v-model="statusFilter"
          :options="statusOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Todas"
          aria-label="Filtrar por estado"
          @change="load"
        />
        <Button label="Solicitar" icon="pi pi-plus" @click="openForm" />
      </div>
    </header>

    <div v-if="balance" class="balance">
      <div><span>Concedidos</span><strong>{{ balance.allowanceDays }}</strong></div>
      <div><span>Aprobados</span><strong>{{ balance.approvedDays }}</strong></div>
      <div><span>Pendientes</span><strong>{{ balance.pendingDays }}</strong></div>
      <div class="highlight"><span>Disponibles</span><strong>{{ balance.availableDays }}</strong></div>
    </div>

    <Message v-if="error" severity="error" :closable="false">
      {{ error }}
      <Button label="Reintentar" text size="small" @click="load" />
    </Message>

    <div v-if="loading" class="loading"><ProgressSpinner style="width: 44px; height: 44px" /></div>

    <DataTable v-else :value="requests" dataKey="id" responsiveLayout="scroll" class="card-table" paginator :rows="10">
      <template #empty>
        <p class="empty">No hay solicitudes que mostrar.</p>
      </template>
      <Column v-if="auth.isAdmin" field="employeeName" header="Empleado" />
      <Column field="absenceTypeName" header="Tipo" />
      <Column header="Desde">
        <template #body="{ data }">{{ data.startDate.slice(0, 10) }}</template>
      </Column>
      <Column header="Hasta">
        <template #body="{ data }">{{ data.endDate.slice(0, 10) }}</template>
      </Column>
      <Column header="Días">
        <template #body="{ data }">{{ data.workingDays }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :severity="ABSENCE_STATUS_SEVERITY[data.status]" :value="ABSENCE_STATUS_LABEL[data.status]" />
        </template>
      </Column>
      <Column header="Resuelta por">
        <template #body="{ data }">{{ data.decidedBy ?? '—' }}</template>
      </Column>
      <Column header="" style="width: 10rem">
        <template #body="{ data }">
          <template v-if="auth.isAdmin && data.status === AbsenceStatus.Pending">
            <Button
              icon="pi pi-check"
              text
              rounded
              severity="success"
              aria-label="Aprobar"
              @click="decide(data, true)"
            />
            <Button
              icon="pi pi-times"
              text
              rounded
              severity="danger"
              aria-label="Rechazar"
              @click="decide(data, false)"
            />
          </template>
          <Button
            v-if="canCancel(data)"
            icon="pi pi-undo"
            text
            rounded
            aria-label="Retirar solicitud"
            @click="cancel(data)"
          />
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="formVisible" modal header="Nueva solicitud" :style="{ width: '28rem' }">
      <div class="form">
        <div class="field">
          <label for="a-type">Tipo</label>
          <Select
            id="a-type"
            v-model="formTypeId"
            :options="typeOptions"
            optionLabel="label"
            optionValue="value"
            fluid
          />
          <small v-if="selectedType?.consumesVacationBalance">Descuenta del saldo de vacaciones.</small>
          <small v-else-if="selectedType && !selectedType.requiresApproval">
            No requiere aprobación: quedará registrada directamente.
          </small>
        </div>
        <div class="field">
          <label for="a-start">Desde</label>
          <input id="a-start" type="date" v-model="formStart" class="date" />
        </div>
        <div class="field">
          <label for="a-end">Hasta</label>
          <input id="a-end" type="date" v-model="formEnd" class="date" />
        </div>
        <div class="field">
          <label for="a-reason">Motivo (opcional)</label>
          <Textarea id="a-reason" v-model="formReason" rows="3" fluid />
        </div>
        <Message v-if="formError" severity="error" :closable="false">{{ formError }}</Message>
      </div>
      <template #footer>
        <Button label="Cancelar" text @click="formVisible = false" />
        <Button label="Enviar" icon="pi pi-send" :loading="saving" @click="submit" />
      </template>
    </Dialog>
  </section>
</template>

<style scoped>
.absences { display: flex; flex-direction: column; gap: 1.25rem; }
.head { display: flex; align-items: start; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
.head h1 { margin: 0; font-size: 1.5rem; }
.head p { margin: 0.25rem 0 0; color: var(--hria-muted); font-size: 0.9rem; }
.actions { display: flex; gap: 0.5rem; align-items: center; }

.balance { display: flex; gap: 1rem; flex-wrap: wrap; }
.balance div {
  flex: 1; min-width: 120px; text-align: center;
  background: var(--hria-surface); border: 1px solid var(--hria-border);
  border-radius: 14px; padding: 0.85rem;
}
.balance span { display: block; font-size: 0.78rem; color: var(--hria-muted); }
.balance strong { font-size: 1.5rem; color: var(--hria-heading); }
.balance .highlight { border-color: var(--hria-accent); }
.balance .highlight strong { color: var(--hria-accent-600); }

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
