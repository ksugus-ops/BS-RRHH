<script setup lang="ts">
import { onMounted, ref } from 'vue'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ClockPanel from './ClockPanel.vue'
import WorkdayTable from './WorkdayTable.vue'
import { timeApi } from './api'
import type { Workday } from './types'
import type { ApiError } from '@/shared/http/client'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

function isoDaysAgo(days: number) {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString().slice(0, 10)
}

const from = ref(isoDaysAgo(30))
const to = ref(isoDaysAgo(0))
const workdays = ref<Workday[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function load() {
  loading.value = true
  error.value = null
  try {
    // "Mis jornadas": siempre las del usuario logueado (aunque sea administrador).
    workdays.value = await timeApi.workdays({
      employeeId: auth.user?.employeeId,
      from: from.value,
      to: to.value,
    })
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar las jornadas.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="tt">
    <h1>Mi jornada</h1>
    <ClockPanel @changed="load" />

    <div class="history">
      <header class="filters">
        <h2>Mis jornadas</h2>
        <div class="range">
          <label>Desde <InputText v-model="from" type="date" /></label>
          <label>Hasta <InputText v-model="to" type="date" /></label>
          <Button label="Filtrar" icon="pi pi-filter" size="small" @click="load" />
        </div>
      </header>
      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
      <WorkdayTable :workdays="workdays" :loading="loading" />
    </div>
  </section>
</template>

<style scoped>
.tt { display: flex; flex-direction: column; gap: 1.5rem; }
.tt h1 { margin: 0; font-size: 1.5rem; }
.history { display: flex; flex-direction: column; gap: 1rem; }
.filters { display: flex; align-items: end; justify-content: space-between; flex-wrap: wrap; gap: 1rem; }
.filters h2 { margin: 0; font-size: 1.15rem; }
.range { display: flex; align-items: end; gap: 0.75rem; flex-wrap: wrap; }
.range label { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.85rem; font-weight: 600; }
</style>
