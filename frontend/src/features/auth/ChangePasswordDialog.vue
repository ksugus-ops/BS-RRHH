<script setup lang="ts">
import { ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { useToast } from 'primevue/usetoast'
import { authApi } from './api'
import { useAuthStore } from '@/stores/auth'
import type { ApiError } from '@/shared/http/client'

const MIN_LENGTH = 8

const auth = useAuthStore()

const props = defineProps<{ visible: boolean }>()
const emit = defineEmits<{ 'update:visible': [boolean] }>()

const toast = useToast()

const current = ref('')
const next = ref('')
const repeat = ref('')
const saving = ref(false)
const error = ref<string | null>(null)

watch(
  () => props.visible,
  (open) => {
    if (!open) return
    current.value = ''
    next.value = ''
    repeat.value = ''
    error.value = null
  },
)

async function submit() {
  error.value = null

  if (!current.value || !next.value) {
    error.value = 'Rellena todos los campos.'
    return
  }
  if (next.value.length < MIN_LENGTH) {
    error.value = `La nueva contraseña debe tener al menos ${MIN_LENGTH} caracteres.`
    return
  }
  // Se comprueba aquí porque el backend no recibe la repetición: es solo una
  // salvaguarda contra erratas al teclear.
  if (next.value !== repeat.value) {
    error.value = 'La nueva contraseña y su repetición no coinciden.'
    return
  }
  if (next.value === current.value) {
    error.value = 'La nueva contraseña debe ser distinta de la actual.'
    return
  }

  saving.value = true
  try {
    await authApi.changePassword(current.value, next.value)
    toast.add({ severity: 'success', summary: 'Contraseña actualizada', life: 4000 })
    emit('update:visible', false)
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo cambiar la contraseña.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    modal
    header="Cambiar mi contraseña"
    :style="{ width: '26rem' }"
    :breakpoints="{ '640px': '95vw' }"
  >
    <form class="form" @submit.prevent="submit">
      <!--
        El gestor de contraseñas del navegador necesita un campo de usuario junto a
        los de contraseña. Sin él buscaba uno por su cuenta y escribía el correo en
        el primer cuadro de texto de la página: el buscador de la lista de empleados.
      -->
      <div class="field">
        <label for="cp-user">Cuenta</label>
        <input id="cp-user" class="account" type="text" autocomplete="username"
          :value="auth.user?.email ?? ''" readonly tabindex="-1" />
      </div>

      <div class="field">
        <label for="cp-current">Contraseña actual</label>
        <!-- inputId/inputProps y no id/autocomplete: PrimeVue los pone en el
             envoltorio, no en el <input>, y el navegador no llegaba a verlos. -->
        <Password inputId="cp-current" v-model="current" :feedback="false" toggleMask fluid
          :inputProps="{ autocomplete: 'current-password' }" />
      </div>
      <div class="field">
        <label for="cp-new">Nueva contraseña</label>
        <Password inputId="cp-new" v-model="next" toggleMask fluid
          :inputProps="{ autocomplete: 'new-password' }" />
        <small>Mínimo {{ MIN_LENGTH }} caracteres.</small>
      </div>
      <div class="field">
        <label for="cp-repeat">Repite la nueva contraseña</label>
        <Password inputId="cp-repeat" v-model="repeat" :feedback="false" toggleMask fluid
          :inputProps="{ autocomplete: 'new-password' }" />
      </div>

      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

      <!-- Permite enviar con Intro; el botón visible vive en el pie del diálogo. -->
      <button type="submit" class="submit-on-enter" tabindex="-1" aria-hidden="true"></button>
    </form>

    <template #footer>
      <Button label="Cancelar" text @click="emit('update:visible', false)" />
      <Button label="Cambiar" icon="pi pi-check" :loading="saving" @click="submit" />
    </template>
  </Dialog>
</template>

<style scoped>
.form { display: flex; flex-direction: column; gap: 1rem; }
.field { display: flex; flex-direction: column; gap: 0.4rem; }
.field label { font-weight: 600; font-size: 0.9rem; }
.field small { color: var(--hria-muted); font-size: 0.78rem; }

/* De solo lectura: identifica la cuenta y da al navegador dónde autorrellenar. */
.account {
  width: 100%;
  padding: 0.55rem 0.7rem;
  border: 1px solid var(--hria-border);
  border-radius: 6px;
  background: var(--hria-surface-3);
  color: var(--hria-muted);
  font-size: 0.9rem;
  font-family: inherit;
  cursor: default;
}
.submit-on-enter { position: absolute; width: 0; height: 0; padding: 0; border: 0; opacity: 0; }
</style>
