<script setup lang="ts">
import { RouterView, RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Button from 'primevue/button'
import ThemeToggle from '@/shared/components/ThemeToggle.vue'
import BrandLogo from '@/shared/components/BrandLogo.vue'
import UserAvatar from '@/shared/components/UserAvatar.vue'
import ChangePasswordDialog from '@/features/auth/ChangePasswordDialog.vue'
import AiAssistantWidget from '@/features/ai-assistant/AiAssistantWidget.vue'

import { ref } from 'vue'

const auth = useAuthStore()
const router = useRouter()
const passwordVisible = ref(false)

function onLogout() {
  auth.logout()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="layout">
    <header class="topbar">
      <RouterLink class="brand" :to="{ name: 'home' }" aria-label="Ir al inicio">
        <BrandLogo class="logo" />
        <span class="brand-text">BinsaRRHH</span>
      </RouterLink>

      <nav class="nav" aria-label="Navegación principal">
        <RouterLink :to="{ name: 'home' }">Inicio</RouterLink>
        <RouterLink :to="{ name: 'time-tracking' }">Fichaje</RouterLink>
        <RouterLink :to="{ name: 'absences' }">Ausencias</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'employees' }">Empleados</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'time-admin' }">Registros</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'schedules' }">Horarios</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'work-calendar' }">Calendario</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'vacation-calendar' }">Vacaciones</RouterLink>
        <!-- El asistente no ocupa sitio en la barra: se abre desde su botón flotante. -->
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'audit' }">Auditoría</RouterLink>
      </nav>

      <div class="right" v-if="auth.user">
        <ThemeToggle />
        <span class="live"><span class="dot" aria-hidden="true"></span>Online</span>
        <div class="user">
          <UserAvatar :name="auth.user.fullName" :avatar-url="auth.user.avatarUrl" :size="34" />
          <span class="who">
            {{ auth.user.fullName }}
            <small>{{ auth.isAdmin ? 'Administrador' : 'Empleado' }}</small>
          </span>
        </div>
        <Button
          icon="pi pi-key"
          rounded
          text
          aria-label="Cambiar mi contraseña"
          title="Cambiar mi contraseña"
          @click="passwordVisible = true"
        />
        <Button icon="pi pi-sign-out" rounded text aria-label="Cerrar sesión" @click="onLogout" />
      </div>
    </header>

    <main class="content" role="main">
      <RouterView />
    </main>

    <ChangePasswordDialog v-model:visible="passwordVisible" />

    <!-- Vive en el layout, no en una vista: así el hilo del chat sobrevive a la
         navegación entre pantallas. -->
    <AiAssistantWidget />
  </div>
</template>

<style scoped>
.layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}
.topbar {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.7rem 1.5rem;
  border-bottom: 1px solid var(--hria-border-strong);
  background: var(--hria-topbar-bg);
  backdrop-filter: blur(10px);
  position: sticky;
  top: 0;
  z-index: 10;
}
/* La marca es el atajo al inicio, como en cualquier web. */
.brand {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  text-decoration: none;
  padding: 0.25rem 0.4rem;
  margin-left: -0.4rem;
  border-radius: 10px;
  transition: background 0.15s;
}
.brand:hover { background: var(--hria-accent-soft); }
.brand:focus-visible { outline: 2px solid var(--hria-accent-700); outline-offset: 2px; }
.logo {
  height: 30px;
  width: auto;
  align-self: center;
}
.brand-text {
  font-size: 1.2rem;
  font-weight: 700;
  color: var(--hria-accent-600);
  letter-spacing: 0.3px;
}
/* Con nueve secciones el desplazamiento horizontal escondía enlaces sin
   ninguna pista visual. Envolver a una segunda línea los mantiene todos
   accesibles y hace crecer la barra solo cuando hace falta. */
.nav {
  display: flex;
  gap: 0.35rem;
  margin-left: 1.5rem;
  margin-right: auto;
  min-width: 0;
  flex-wrap: wrap;
}
.nav a {
  text-decoration: none;
  color: var(--hria-muted);
  font-weight: 500;
  font-size: 0.92rem;
  padding: 0.4rem 0.8rem;
  border-radius: 8px;
  transition: all 0.15s;
}
.nav a:hover {
  color: var(--hria-accent-600);
  background: var(--hria-accent-soft);
}
/* "exact-active" y no "active": la ruta de Inicio es la padre de todas las
   demás, así que con `router-link-active` quedaba marcada a la vez que la
   sección en la que realmente estás. */
.nav a.router-link-exact-active {
  color: var(--hria-accent-700);
  background: var(--hria-accent-soft-2);
}
.right {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  flex: none;
}
.brand {
  flex: none;
}
.live {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.78rem;
  color: var(--hria-accent-600);
  background: var(--hria-accent-soft-2);
  border: 1px solid rgba(22, 185, 138, 0.3);
  padding: 0.2rem 0.6rem;
  border-radius: 999px;
}
.live .dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #16b98a;
  box-shadow: 0 0 0 0 rgba(22, 185, 138, 0.6);
  animation: pulse 2s infinite;
}
@keyframes pulse {
  0% { box-shadow: 0 0 0 0 rgba(22, 185, 138, 0.5); }
  70% { box-shadow: 0 0 0 6px rgba(22, 185, 138, 0); }
  100% { box-shadow: 0 0 0 0 rgba(22, 185, 138, 0); }
}
.user {
  display: flex;
  align-items: center;
  gap: 0.55rem;
}
.avatar {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 0.8rem;
  font-weight: 700;
  color: #ffffff;
  background: linear-gradient(135deg, #0d9973, #33cc9a);
}
.who {
  display: flex;
  flex-direction: column;
  line-height: 1.2;
  font-size: 0.9rem;
  color: var(--hria-text);
}
.who small {
  color: var(--hria-muted-2);
  font-size: 0.72rem;
}
.content {
  flex: 1;
  padding: 1.75rem;
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
}
/* La barra superior tiene muchos elementos (marca + 6 enlaces + estado +
   usuario + acciones). Se van retirando los accesorios por orden de menor
   importancia para que nunca desborde horizontalmente. */
@media (max-width: 1100px) {
  .live { display: none; }
}
@media (max-width: 900px) {
  .who { display: none; }
}
@media (max-width: 720px) {
  .who { display: none; }
  .nav { margin-left: 0.5rem; gap: 0.1rem; }
  .nav a { padding: 0.4rem 0.5rem; font-size: 0.85rem; }
}
</style>
