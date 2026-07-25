<script setup lang="ts">
import { RouterLink } from 'vue-router'
</script>

<template>
  <main class="not-found" role="main">
    <h1>404</h1>
    <p>La página que buscas no existe.</p>
    <RouterLink to="/">Volver al inicio</RouterLink>
  </main>
</template>

<style scoped>
.not-found {
  text-align: center;
  margin: 5rem auto;
}
</style>
