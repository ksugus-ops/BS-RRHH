<script setup lang="ts">
import { computed } from 'vue'
import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()

/**
 * El logotipo corporativo tiene las letras en azul marino, ilegibles sobre
 * fondo oscuro. En modo oscuro se sirve una variante con las letras claras.
 */
const src = computed(() => (theme.isDark ? '/logo-binsa-dark.png' : '/logo-binsa.png'))
</script>

<template>
  <img :src="src" alt="Binsa Software" />
</template>
