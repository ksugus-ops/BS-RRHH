<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()

const label = computed(() =>
  theme.isDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro',
)
</script>

<template>
  <Button
    class="theme-toggle"
    :icon="theme.isDark ? 'pi pi-sun' : 'pi pi-moon'"
    :aria-label="label"
    :title="label"
    severity="secondary"
    text
    rounded
    @click="theme.toggle()"
  />
</template>

<style scoped>
.theme-toggle {
  color: var(--hria-muted);
}
.theme-toggle:hover {
  color: var(--hria-accent-600);
}
</style>
