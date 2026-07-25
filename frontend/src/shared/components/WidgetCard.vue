<script setup lang="ts">
defineProps<{
  title: string
  icon: string
  accent?: string
}>()
</script>

<template>
  <section class="widget">
    <header class="widget-head">
      <span class="widget-icon" :style="{ background: (accent ?? '#16b98a') + '1a', color: accent ?? '#16b98a' }">
        <i :class="icon" />
      </span>
      <h2>{{ title }}</h2>
    </header>
    <div class="widget-body">
      <slot />
    </div>
  </section>
</template>

<style scoped>
.widget {
  background: var(--hria-surface);
  border: 1px solid var(--hria-border);
  border-radius: 16px;
  padding: 1.25rem;
  box-shadow: var(--hria-card-shadow);
  display: flex;
  flex-direction: column;
}
.widget-head {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  margin-bottom: 1rem;
}
.widget-icon {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  font-size: 1.05rem;
  flex: none;
}
.widget-head h2 {
  margin: 0;
  font-size: 1rem;
  font-weight: 600;
}
.widget-body { flex: 1; }
</style>
