<script setup lang="ts">
import { onMounted, ref } from 'vue'
import Select from 'primevue/select'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import WorkdayTable from './WorkdayTable.vue'
import { timeApi } from './api'
import type { Workday } from './types'
import { employeesApi } from '@/features/employees/api'
import type { EmployeeListItem } from '@/features/employees/types'
import type { ApiError } from '@/shared/http/client'

function isoDaysAgo(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString().slice(0, 10)
}

const employees = ref<EmployeeListItem[]>([])
const employeeId = ref<number | null>(null)
const from = ref(isoDaysAgo(30))
const to = ref(isoDaysAgo(0))
const workdays = ref<Workday[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    workdays.value = await timeApi.workdays({
      employeeId: employeeId.value,
      from: from.value,
      to: to.value,
    })
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar las jornadas.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  const page = await employeesApi.list({ page: 1, pageSize: 100 })
  employees.value = page.items
  await load()
})
</script>

<template>
  <section class="admin">
    <h1>Registros horarios</h1>

    <header class="filters">
      <label>Empleado
        <Select v-model="employeeId" :options="employees" optionLabel="fullName" optionValue="id"
          placeholder="Todos" showClear @change="load" style="min-width: 220px" />
      </label>
      <label>Desde <InputText v-model="from" type="date" /></label>
      <label>Hasta <InputText v-model="to" type="date" /></label>
      <Button label="Filtrar" icon="pi pi-filter" @click="load" />
    </header>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <WorkdayTable :workdays="workdays" :loading="loading" />
  </section>
</template>

<style scoped>
.admin { display: flex; flex-direction: column; gap: 1.25rem; }
.admin h1 { margin: 0; font-size: 1.5rem; }
.filters { display: flex; align-items: end; gap: 0.75rem; flex-wrap: wrap; }
.filters label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.85rem; font-weight: 600; }
</style>
