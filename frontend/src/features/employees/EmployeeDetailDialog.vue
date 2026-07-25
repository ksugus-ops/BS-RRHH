<script setup lang="ts">
import { ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import Tag from 'primevue/tag'
import ProgressSpinner from 'primevue/progressspinner'
import Message from 'primevue/message'
import { employeesApi } from './api'
import type { EmployeeDetail } from './types'
import { Role } from '@/features/auth/types'
import type { ApiError } from '@/shared/http/client'

const props = defineProps<{ visible: boolean; employeeId: number | null }>()
const emit = defineEmits<{ (e: 'update:visible', value: boolean): void }>()

const employee = ref<EmployeeDetail | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

watch(
  () => props.visible,
  async (open) => {
    if (!open || !props.employeeId) return
    loading.value = true
    error.value = null
    employee.value = null
    try {
      employee.value = await employeesApi.get(props.employeeId)
    } catch (e) {
      error.value = (e as ApiError).title ?? 'No se pudo cargar el empleado.'
    } finally {
      loading.value = false
    }
  },
)

function roleLabel(role: Role | null) {
  if (role === Role.Admin) return 'Administrador'
  if (role === Role.Employee) return 'Empleado'
  return '—'
}

function fmtDate(value?: string) {
  if (!value) return '—'
  return new Date(value).toLocaleDateString()
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="(v: boolean) => emit('update:visible', v)"
    modal
    header="Detalle del empleado"
    :style="{ width: '30rem' }"
  >
    <div v-if="loading" class="center"><ProgressSpinner style="width: 40px; height: 40px" /></div>
    <Message v-else-if="error" severity="error" :closable="false">{{ error }}</Message>
    <dl v-else-if="employee" class="detail">
      <dt>Nombre</dt><dd>{{ employee.firstName }} {{ employee.lastName }}</dd>
      <dt>Correo</dt><dd>{{ employee.email }}</dd>
      <dt>Departamento</dt><dd>{{ employee.departmentName }}</dd>
      <dt>Puesto</dt><dd>{{ employee.position }}</dd>
      <dt>Incorporación</dt><dd>{{ fmtDate(employee.hireDate) }}</dd>
      <dt>Rol</dt><dd>{{ roleLabel(employee.role) }}</dd>
      <dt>Estado</dt>
      <dd>
        <Tag :value="employee.isActive ? 'Activo' : 'Inactivo'"
          :severity="employee.isActive ? 'success' : 'secondary'" />
      </dd>
    </dl>
  </Dialog>
</template>

<style scoped>
.center { display: grid; place-items: center; padding: 2rem; }
.detail { display: grid; grid-template-columns: auto 1fr; gap: 0.6rem 1rem; margin: 0; }
.detail dt { font-weight: 600; color: var(--p-text-muted-color, #6b7280); }
.detail dd { margin: 0; }
</style>
