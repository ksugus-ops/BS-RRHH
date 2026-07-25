<script setup lang="ts">
import { onMounted, ref } from 'vue'
import DataTable, { type DataTablePageEvent } from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import { useConfirm } from 'primevue/useconfirm'
import ConfirmDialog from 'primevue/confirmdialog'
import { useToast } from 'primevue/usetoast'
import { employeesApi } from './api'
import type { Department, EmployeeDetail, EmployeeListItem } from './types'
import { Role } from '@/features/auth/types'
import EmployeeFormDialog from './EmployeeFormDialog.vue'
import EmployeeDetailDialog from './EmployeeDetailDialog.vue'
import type { ApiError } from '@/shared/http/client'

const confirm = useConfirm()
const toast = useToast()

const items = ref<EmployeeListItem[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const error = ref<string | null>(null)

const search = ref('')
const departmentId = ref<number | null>(null)
const isActive = ref<boolean | null>(null)
const departments = ref<Department[]>([])

const statusOptions = [
  { label: 'Todos', value: null },
  { label: 'Activos', value: true },
  { label: 'Inactivos', value: false },
]

const showForm = ref(false)
const editing = ref<EmployeeDetail | null>(null)
const showDetail = ref(false)
const detailId = ref<number | null>(null)

let searchTimer: ReturnType<typeof setTimeout> | undefined

async function load() {
  loading.value = true
  error.value = null
  try {
    const res = await employeesApi.list({
      search: search.value,
      departmentId: departmentId.value,
      isActive: isActive.value,
      page: page.value,
      pageSize: pageSize.value,
    })
    items.value = res.items
    total.value = res.total
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudieron cargar los empleados.'
  } finally {
    loading.value = false
  }
}

function onPage(e: DataTablePageEvent) {
  page.value = e.page + 1
  pageSize.value = e.rows
  load()
}

function onFilterChange() {
  page.value = 1
  load()
}

function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(onFilterChange, 350)
}

function openCreate() {
  editing.value = null
  showForm.value = true
}

async function openEdit(row: EmployeeListItem) {
  editing.value = await employeesApi.get(row.id)
  showForm.value = true
}

function openDetail(row: EmployeeListItem) {
  detailId.value = row.id
  showDetail.value = true
}

function onSaved() {
  toast.add({ severity: 'success', summary: 'Guardado', detail: 'Empleado guardado correctamente.', life: 3000 })
  load()
}

function confirmDeactivate(row: EmployeeListItem) {
  confirm.require({
    header: 'Confirmar baja',
    message: `¿Dar de baja a ${row.fullName}? Su acceso quedará desactivado.`,
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Cancelar',
    acceptLabel: 'Dar de baja',
    acceptProps: { severity: 'danger' },
    accept: async () => {
      try {
        await employeesApi.deactivate(row.id)
        toast.add({ severity: 'success', summary: 'Baja realizada', detail: `${row.fullName} desactivado/a.`, life: 3000 })
        load()
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Error', detail: (e as ApiError).title, life: 4000 })
      }
    },
  })
}

function roleLabel(role: Role | null) {
  if (role === Role.Admin) return 'Administrador'
  if (role === Role.Employee) return 'Empleado'
  return '—'
}

onMounted(async () => {
  departments.value = await employeesApi.departments()
  await load()
})
</script>

<template>
  <section class="employees">
    <ConfirmDialog />

    <header class="head">
      <h1>Empleados</h1>
      <Button label="Nuevo empleado" icon="pi pi-plus" @click="openCreate" />
    </header>

    <div class="filters">
      <span class="p-input-icon-left search">
        <!-- type=search + autocomplete off: que el navegador no lo confunda con
             un campo de usuario al abrir un diálogo de contraseña. -->
        <InputText v-model="search" type="search" name="employee-search" autocomplete="off"
          placeholder="Buscar por nombre o correo…"
          aria-label="Buscar empleados" @input="onSearchInput" fluid />
      </span>
      <Select v-model="departmentId" :options="departments" optionLabel="name" optionValue="id"
        placeholder="Departamento" showClear @change="onFilterChange" aria-label="Filtrar por departamento" />
      <Select v-model="isActive" :options="statusOptions" optionLabel="label" optionValue="value"
        placeholder="Estado" @change="onFilterChange" aria-label="Filtrar por estado" />
    </div>

    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

    <DataTable
      :value="items"
      :loading="loading"
      lazy
      paginator
      :rows="pageSize"
      :rowsPerPageOptions="[10, 20, 50]"
      :totalRecords="total"
      :first="(page - 1) * pageSize"
      @page="onPage"
      dataKey="id"
      responsiveLayout="scroll"
    >
      <template #empty>
        <div class="empty">No hay empleados que coincidan con los filtros.</div>
      </template>

      <Column field="fullName" header="Nombre" />
      <Column field="email" header="Correo" />
      <Column field="departmentName" header="Departamento" />
      <Column field="position" header="Puesto" />
      <Column header="Rol">
        <template #body="{ data }">{{ roleLabel(data.role) }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.isActive ? 'Activo' : 'Inactivo'" :severity="data.isActive ? 'success' : 'secondary'" />
        </template>
      </Column>
      <Column header="Acciones" :exportable="false" style="width: 10rem">
        <template #body="{ data }">
          <div class="actions">
            <Button icon="pi pi-eye" text rounded aria-label="Ver detalle" @click="openDetail(data)" />
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button v-if="data.isActive" icon="pi pi-ban" text rounded severity="danger"
              aria-label="Dar de baja" @click="confirmDeactivate(data)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <EmployeeFormDialog
      v-model:visible="showForm"
      :employee="editing"
      :departments="departments"
      @saved="onSaved"
    />
    <EmployeeDetailDialog v-model:visible="showDetail" :employee-id="detailId" />
  </section>
</template>

<style scoped>
.employees { display: flex; flex-direction: column; gap: 1.25rem; }
.head { display: flex; align-items: center; justify-content: space-between; }
.head h1 { margin: 0; font-size: 1.5rem; }
.filters { display: flex; gap: 0.75rem; flex-wrap: wrap; }
.search { flex: 1; min-width: 220px; }
.actions { display: flex; gap: 0.25rem; }
.empty { padding: 2rem; text-align: center; color: var(--p-text-muted-color, #6b7280); }
</style>
