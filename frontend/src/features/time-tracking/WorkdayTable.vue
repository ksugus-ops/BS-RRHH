<script setup lang="ts">
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import { WorkdayStatus, type Workday } from './types'
import { formatDate, formatTime, formatMinutes } from '@/shared/utils/format'

defineProps<{ workdays: Workday[]; loading?: boolean }>()

/** Una desviación de pocos minutos es ruido: solo se destaca a partir de 15. */
const TOLERANCIA_MINUTOS = 15

function formatDeviation(minutes: number): string {
  if (minutes === 0) return '0 min'
  const signo = minutes > 0 ? '+' : '−'
  return `${signo}${formatMinutes(Math.abs(minutes))}`
}

function deviationClass(minutes: number): string {
  if (Math.abs(minutes) <= TOLERANCIA_MINUTOS) return 'dev-ok'
  return minutes < 0 ? 'dev-under' : 'dev-over'
}

function statusTag(status: WorkdayStatus) {
  switch (status) {
    case WorkdayStatus.Completed: return { text: 'Completa', severity: 'success' as const }
    case WorkdayStatus.Open: return { text: 'Abierta', severity: 'info' as const }
    case WorkdayStatus.Incomplete: return { text: 'Incompleta', severity: 'danger' as const }
    default: return { text: '—', severity: 'secondary' as const }
  }
}
</script>

<template>
  <DataTable :value="workdays" :loading="loading" paginator :rows="10"
    dataKey="id" responsiveLayout="scroll" sortField="checkIn" :sortOrder="-1">
    <template #empty>
      <div class="empty">No hay jornadas para el rango seleccionado.</div>
    </template>

    <Column header="Fecha" sortable field="date">
      <template #body="{ data }">{{ formatDate(data.date) }}</template>
    </Column>
    <Column header="Entrada">
      <template #body="{ data }">{{ formatTime(data.checkIn) }}</template>
    </Column>
    <Column header="Salida">
      <template #body="{ data }">{{ data.checkOut ? formatTime(data.checkOut) : '—' }}</template>
    </Column>
    <Column header="Descansos">
      <template #body="{ data }">{{ data.breaks.length }}</template>
    </Column>
    <Column header="Trabajado">
      <template #body="{ data }">{{ formatMinutes(data.workedMinutes) }}</template>
    </Column>
    <Column header="Previsto">
      <template #body="{ data }">
        <span v-if="data.expectedMinutes === null" class="muted" title="El empleado no tiene horario asignado">
          —
        </span>
        <span v-else>{{ formatMinutes(data.expectedMinutes) }}</span>
      </template>
    </Column>
    <Column header="Desviación">
      <template #body="{ data }">
        <span v-if="data.deviationMinutes === null" class="muted">—</span>
        <span v-else :class="deviationClass(data.deviationMinutes)">
          {{ formatDeviation(data.deviationMinutes) }}
        </span>
      </template>
    </Column>
    <Column header="Estado">
      <template #body="{ data }">
        <Tag :value="statusTag(data.status).text" :severity="statusTag(data.status).severity" />
      </template>
    </Column>
  </DataTable>
</template>

<style scoped>
.empty { padding: 2rem; text-align: center; color: var(--p-text-muted-color, #6b7280); }
.muted { color: var(--hria-muted-2); }
.dev-ok { color: var(--hria-muted); }
.dev-under { color: #f43f5e; font-weight: 600; }
.dev-over { color: var(--hria-accent-600); font-weight: 600; }
</style>
