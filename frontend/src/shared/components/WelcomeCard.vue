<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    name: string
    subtitle?: string
    avatarUrl?: string | null
  }>(),
  { avatarUrl: null },
)

const initials = computed(() =>
  props.name
    .split(' ')
    .slice(0, 2)
    .map((p) => p[0])
    .join('')
    .toUpperCase(),
)

const today = computed(() => {
  const d = new Date().toLocaleDateString('es-ES', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
  return d.charAt(0).toUpperCase() + d.slice(1)
})
</script>

<template>
  <div class="welcome">
    <div class="welcome-text">
      <p class="hello">Hola {{ name.split(' ')[0] }},</p>
      <h1>¡Bienvenido/a!</h1>
      <p class="date"><i class="pi pi-calendar" /> {{ today }}</p>
      <p v-if="subtitle" class="sub">{{ subtitle }}</p>
    </div>
    <!-- Sobre la tarjeta verde el círculo blanco con iniciales contrasta mejor
         que el avatar de color, así que solo se sustituye si hay foto real. -->
    <img v-if="avatarUrl" class="avatar photo" :src="avatarUrl" :alt="name" />
    <div v-else class="avatar" :title="name" aria-hidden="true">{{ initials }}</div>
  </div>
</template>

<style scoped>
.welcome {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  height: 100%;
  background: linear-gradient(120deg, #16b98a 0%, #33cc9a 55%, #57d9ac 100%);
  border-radius: 18px;
  padding: 1.6rem 1.8rem;
  color: #fff;
  box-shadow: 0 8px 24px rgba(22, 185, 138, 0.28);
  box-sizing: border-box;
}
.hello { margin: 0; font-size: 0.95rem; opacity: 0.92; }
.welcome-text h1 {
  margin: 0.2rem 0 0.6rem;
  font-size: 1.7rem;
  color: #fff;
  font-weight: 700;
}
.date {
  margin: 0;
  font-size: 0.85rem;
  opacity: 0.92;
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
}
.sub { margin: 0.4rem 0 0; font-size: 0.85rem; opacity: 0.9; }
.avatar {
  width: 66px;
  height: 66px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 1.3rem;
  font-weight: 700;
  color: #0b7d5f;
  background: rgba(255, 255, 255, 0.9);
  flex: none;
  border: 3px solid rgba(255, 255, 255, 0.55);
}
.avatar.photo { object-fit: cover; background: rgba(255, 255, 255, 0.9); }
@media (max-width: 560px) {
  .welcome-text h1 { font-size: 1.35rem; }
  .avatar { width: 52px; height: 52px; font-size: 1.05rem; }
}
</style>
