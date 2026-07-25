<script setup lang="ts">
import { reactive, ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { Role } from '@/features/auth/types'
import type { Department, EmployeeDetail, EmployeeFormData } from './types'
import { employeesApi } from './api'
import type { ApiError } from '@/shared/http/client'

const props = defineProps<{
  visible: boolean
  employee: EmployeeDetail | null
  departments: Department[]
}>()

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void
  (e: 'saved'): void
}>()

const roleOptions = [
  { label: 'Administrador', value: Role.Admin },
  { label: 'Empleado', value: Role.Employee },
]

const form = reactive<EmployeeFormData>({
  firstName: '', lastName: '', email: '', departmentId: null,
  position: '', hireDate: '', role: Role.Employee, initialPassword: '',
})

const saving = ref(false)
const error = ref<string | null>(null)
const fieldErrors = ref<Record<string, string[]>>({})

const isEdit = ref(false)

watch(
  () => props.visible,
  (open) => {
    if (!open) return
    error.value = null
    fieldErrors.value = {}
    if (props.employee) {
      isEdit.value = true
      Object.assign(form, {
        firstName: props.employee.firstName,
        lastName: props.employee.lastName,
        email: props.employee.email,
        departmentId: props.employee.departmentId,
        position: props.employee.position,
        hireDate: props.employee.hireDate?.substring(0, 10) ?? '',
        role: props.employee.role ?? Role.Employee,
        initialPassword: '',
      })
    } else {
      isEdit.value = false
      Object.assign(form, {
        firstName: '', lastName: '', email: '', departmentId: null,
        position: '', hireDate: '', role: Role.Employee, initialPassword: '',
      })
    }
  },
)

function close() {
  emit('update:visible', false)
}

async function submit() {
  saving.value = true
  error.value = null
  fieldErrors.value = {}
  try {
    const payload: EmployeeFormData = { ...form }
    if (isEdit.value && props.employee) {
      await employeesApi.update(props.employee.id, payload)
    } else {
      await employeesApi.create(payload)
    }
    emit('saved')
    close()
  } catch (e) {
    const err = e as ApiError
    error.value = err.title ?? 'No se pudo guardar el empleado.'
    if (err.errors) fieldErrors.value = err.errors
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="close"
    modal
    :header="isEdit ? 'Editar empleado' : 'Nuevo empleado'"
    :style="{ width: '32rem' }"
  >
    <form class="form" @submit.prevent="submit">
      <div class="grid">
        <div class="field">
          <label for="firstName">Nombre</label>
          <InputText id="firstName" v-model="form.firstName" required fluid />
        </div>
        <div class="field">
          <label for="lastName">Apellidos</label>
          <InputText id="lastName" v-model="form.lastName" required fluid />
        </div>
      </div>

      <div class="field">
        <label for="email">Correo electrónico</label>
        <InputText id="email" v-model="form.email" type="email" required fluid />
        <small v-if="fieldErrors.Email" class="err">{{ fieldErrors.Email[0] }}</small>
      </div>

      <div class="grid">
        <div class="field">
          <label for="department">Departamento</label>
          <Select
            id="department"
            v-model="form.departmentId"
            :options="departments"
            optionLabel="name"
            optionValue="id"
            placeholder="Selecciona…"
            fluid
          />
        </div>
        <div class="field">
          <label for="position">Puesto</label>
          <InputText id="position" v-model="form.position" required fluid />
        </div>
      </div>

      <div class="grid">
        <div class="field">
          <label for="hireDate">Fecha de incorporación</label>
          <InputText id="hireDate" v-model="form.hireDate" type="date" required fluid />
        </div>
        <div class="field">
          <label for="role">Rol de acceso</label>
          <Select
            id="role"
            v-model="form.role"
            :options="roleOptions"
            optionLabel="label"
            optionValue="value"
            fluid
          />
        </div>
      </div>

      <div v-if="!isEdit" class="field">
        <label for="password">Contraseña inicial</label>
        <Password inputId="password" v-model="form.initialPassword" :feedback="false" toggleMask fluid />
        <small v-if="fieldErrors.InitialPassword" class="err">{{ fieldErrors.InitialPassword[0] }}</small>
      </div>

      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    </form>

    <template #footer>
      <Button label="Cancelar" text @click="close" :disabled="saving" />
      <Button label="Guardar" icon="pi pi-check" :loading="saving" @click="submit" />
    </template>
  </Dialog>
</template>

<style scoped>
.form { display: flex; flex-direction: column; gap: 1rem; padding-top: 0.5rem; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.35rem; }
.field label { font-weight: 600; font-size: 0.85rem; }
.err { color: var(--p-red-500, #ef4444); font-size: 0.8rem; }
@media (max-width: 520px) { .grid { grid-template-columns: 1fr; } }
</style>
