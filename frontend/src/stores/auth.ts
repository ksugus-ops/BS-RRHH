import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi } from '@/features/auth/api'
import { Role, type CurrentUser } from '@/features/auth/types'

const TOKEN_KEY = 'hria.token'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY))
  const user = ref<CurrentUser | null>(null)
  const loading = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isAdmin = computed(() => user.value?.role === Role.Admin)

  function setToken(value: string | null) {
    token.value = value
    if (value) localStorage.setItem(TOKEN_KEY, value)
    else localStorage.removeItem(TOKEN_KEY)
  }

  async function login(email: string, password: string) {
    loading.value = true
    try {
      const res = await authApi.login(email, password)
      setToken(res.token)
      user.value = res.user
      return res.user
    } finally {
      loading.value = false
    }
  }

  /** Restaura la sesión al arrancar: si hay token, valida contra /auth/me. */
  async function restore() {
    if (!token.value) return
    try {
      user.value = await authApi.me()
    } catch {
      // Token inválido o caducado.
      logout()
    }
  }

  function logout() {
    setToken(null)
    user.value = null
  }

  return { token, user, loading, isAuthenticated, isAdmin, login, restore, logout }
})
