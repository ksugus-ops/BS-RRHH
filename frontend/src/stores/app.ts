import { defineStore } from 'pinia'
import { ref } from 'vue'
import { http } from '@/shared/http/client'

/**
 * Store de aplicación (andamiaje). En la Fase 2 solo comprueba la conectividad
 * con el backend vía /health. Los stores de auth y de cada feature llegan después.
 */
export const useAppStore = defineStore('app', () => {
  const backendHealthy = ref<boolean | null>(null)
  const checking = ref(false)

  async function checkHealth() {
    checking.value = true
    try {
      // /health está fuera de /api; se consulta con ruta absoluta relativa al host.
      const res = await http.get('/health', { baseURL: '' })
      backendHealthy.value = res.data?.status === 'healthy'
    } catch {
      backendHealthy.value = false
    } finally {
      checking.value = false
    }
  }

  return { backendHealthy, checking, checkHealth }
})
