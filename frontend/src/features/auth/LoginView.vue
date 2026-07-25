<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { ApiError } from '@/shared/http/client'
import Card from 'primevue/card'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ThemeToggle from '@/shared/components/ThemeToggle.vue'
import BrandLogo from '@/shared/components/BrandLogo.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)

// Credencial de demostración PÚBLICA del empleado. Es la misma que figura en el
// README y en la documentación: exponerla aquí no añade riesgo.
// La del administrador NO se incluye a propósito: la aplicación es accesible
// desde Internet y su contraseña es secreta (ver security.md, riesgos R11/R13).
const DEMO_EMPLOYEE = { email: 'empleado@hria.local', password: 'Demo1234!' }
const ADMIN_EMAIL = 'admin@hria.local'

async function onSubmit() {
  error.value = null
  try {
    await auth.login(email.value.trim(), password.value)
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
  } catch (e) {
    error.value = (e as ApiError).title ?? 'No se pudo iniciar sesión.'
  }
}

/** Acceso rápido como empleado: rellena la credencial pública y entra. */
async function loginAsEmployee() {
  email.value = DEMO_EMPLOYEE.email
  password.value = DEMO_EMPLOYEE.password
  await onSubmit()
}

/** El administrador solo se prerrellena el correo; la contraseña la teclea quien
 *  la tenga (se facilita en la entrega), nunca se muestra en pantalla. */
function fillAdminEmail() {
  email.value = ADMIN_EMAIL
  password.value = ''
  document.getElementById('password')?.focus()
}
</script>

<template>
  <main class="login" role="main">
    <div class="login-toolbar">
      <ThemeToggle />
    </div>
    <Card class="login-card">
      <template #header>
        <BrandLogo class="login-logo" />
      </template>
      <template #title>Iniciar sesión</template>
      <template #subtitle>BinsaRRHH — ERP de Recursos Humanos</template>
      <template #content>
        <form @submit.prevent="onSubmit" class="form" aria-label="Formulario de inicio de sesión">
          <div class="field">
            <label for="email">Correo electrónico</label>
            <InputText
              id="email"
              v-model="email"
              type="email"
              autocomplete="username"
              required
              aria-required="true"
              fluid
            />
          </div>

          <div class="field">
            <label for="password">Contraseña</label>
            <Password
              inputId="password"
              v-model="password"
              :feedback="false"
              toggleMask
              :inputProps="{ autocomplete: 'current-password' }"
              required
              fluid
            />
          </div>

          <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

          <Button type="submit" label="Entrar" :loading="auth.loading" fluid />
        </form>

        <!-- Usuarios de demostración: se explica cada rol y se ofrece acceso
             directo solo con la credencial pública del empleado. -->
        <div class="demo" aria-label="Usuarios de demostración">
          <p class="demo-title">Usuarios de demostración</p>

          <div class="role">
            <div class="role-head">
              <span class="badge employee">Empleado</span>
              <code>empleado@hria.local</code>
            </div>
            <p class="role-desc">
              Ficha su jornada (entrada, descansos y salida), consulta sus fichajes y horas,
              solicita ausencias y vacaciones, y pregunta al asistente por <strong>sus propios</strong>
              datos. No ve información de otras personas.
            </p>
            <Button
              label="Entrar como Empleado"
              icon="pi pi-sign-in"
              severity="success"
              outlined
              size="small"
              :loading="auth.loading"
              @click="loginAsEmployee"
            />
          </div>

          <div class="role">
            <div class="role-head">
              <span class="badge admin">Administrador</span>
              <code>admin@hria.local</code>
            </div>
            <p class="role-desc">
              Gestiona la plantilla y la planificación: empleados, horarios, ausencias y vacaciones,
              calendario laboral, dashboard con indicadores y gráficos, auditoría, y el asistente
              con datos <strong>de toda la organización</strong>.
            </p>
            <div class="role-actions">
              <Button
                label="Usar este correo"
                icon="pi pi-user"
                severity="secondary"
                text
                size="small"
                @click="fillAdminEmail"
              />
              <small class="note">
                <i class="pi pi-lock" aria-hidden="true" />
                Contraseña facilitada en la entrega — no se publica.
              </small>
            </div>
          </div>
        </div>
      </template>
    </Card>
  </main>
</template>

<style scoped>
.login {
  display: grid;
  place-items: center;
  min-height: 100vh;
  padding: 1rem;
  position: relative;
}
.login-toolbar {
  position: absolute;
  top: 1rem;
  right: 1rem;
}
.login-card {
  width: 100%;
  max-width: 440px;
}
.login-logo {
  display: block;
  height: 52px;
  width: auto;
  margin: 1.5rem auto 0.25rem;
}
.form {
  display: flex;
  flex-direction: column;
  gap: 1.1rem;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.field label {
  font-weight: 600;
  font-size: 0.9rem;
}

/* Panel de usuarios de demostración */
.demo {
  margin-top: 1.5rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--hria-divider);
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.demo-title {
  margin: 0;
  font-size: 0.78rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.4px;
  color: var(--hria-muted-2);
}
.role {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.85rem;
  border: 1px solid var(--hria-border);
  border-radius: 12px;
  background: var(--hria-surface-2);
}
.role-head {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.role-head code {
  font-size: 0.8rem;
  color: var(--hria-muted);
}
.badge {
  font-size: 0.72rem;
  font-weight: 700;
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  color: #fff;
}
.badge.employee {
  background: #16b98a;
}
.badge.admin {
  background: #7c6cf0;
}
.role-desc {
  margin: 0;
  font-size: 0.82rem;
  line-height: 1.45;
  color: var(--hria-text);
}
.role-actions {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
}
.note {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.72rem;
  color: var(--hria-muted-2);
}
</style>
