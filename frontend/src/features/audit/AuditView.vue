<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import DataTable, { type DataTablePageEvent } from 'primevue/datatable'
import Column from 'primevue/column'
import SelectButton from 'primevue/selectbutton'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { auditApi, type AuditLog, type AiQueryLog } from './api'
import { formatDateTime } from '@/shared/utils/format'
import type { ApiError } from '@/shared/http/client'

const tabs = [
  { label: 'Acciones', value: 'actions' },
  { label: 'Consultas IA', value: 'ai' },
]
const tab = ref<'actions' | 'ai'>('actions')

const actions = ref<AuditLog[]>([])
const aiQueries = ref<AiQueryLog[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const error = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    if (tab.value === 'actions') {
      const res = await auditApi.audit(page.value, pageSize.value)
      actions.value = res.items
      total.value = res.total
    } else {
      const res = await auditApi.aiQueries(page.value, pageSize.value)
      aiQueries.value = res.items
      total.value = res.total
    }
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cargar la auditoría.'
  } finally {
    loading.value = false
  }
}

function onPage(e: DataTablePageEvent) {
  page.value = e.page + 1
  pageSize.value = e.rows
  load()
}

watch(tab, () => {
  page.value = 1
  load()
})

function statusSeverity(s: string) {
  if (s === 'Success' || s === 'Demo') return 'success'
  if (s === 'Denied') return 'warn'
  return 'danger'
}

onMounted(load)
</script>

<template>
  <section class="audit">
    <h1>Auditoría</h1>
    <SelectButton v-model="tab" :options="tabs" optionLabel="label" optionValue="value" :allowEmpty="false" />

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <DataTable v-if="tab === 'actions'" :value="actions" :loading="loading" lazy paginator
      :rows="pageSize" :totalRecords="total" :first="(page - 1) * pageSize" @page="onPage"
      dataKey="id" responsiveLayout="scroll">
      <template #empty><div class="empty">Sin registros de auditoría.</div></template>
      <Column header="Fecha"><template #body="{ data }">{{ formatDateTime(data.createdAt) }}</template></Column>
      <Column field="userEmail" header="Usuario" />
      <Column field="action" header="Acción" />
      <Column field="entity" header="Entidad" />
      <Column field="details" header="Detalle" />
    </DataTable>

    <DataTable v-else :value="aiQueries" :loading="loading" lazy paginator
      :rows="pageSize" :totalRecords="total" :first="(page - 1) * pageSize" @page="onPage"
      dataKey="id" responsiveLayout="scroll">
      <template #empty><div class="empty">Sin consultas al asistente.</div></template>
      <Column header="Fecha"><template #body="{ data }">{{ formatDateTime(data.createdAt) }}</template></Column>
      <Column field="userEmail" header="Usuario" />
      <Column field="question" header="Pregunta" />
      <Column header="Herramientas"><template #body="{ data }">{{ data.toolsUsed || '—' }}</template></Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.responseStatus" :severity="statusSeverity(data.responseStatus)" />
        </template>
      </Column>
      <Column header="Duración"><template #body="{ data }">{{ data.durationMs }} ms</template></Column>
    </DataTable>
  </section>
</template>

<style scoped>
.audit { display: flex; flex-direction: column; gap: 1.25rem; }
.audit h1 { margin: 0; font-size: 1.5rem; }
.empty { padding: 2rem; text-align: center; color: var(--hria-muted-2); }
</style>
