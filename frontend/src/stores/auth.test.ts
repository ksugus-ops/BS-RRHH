import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { Role } from '@/features/auth/types'

// Mock del API de auth para no depender de la red.
vi.mock('@/features/auth/api', () => ({
  authApi: {
    login: vi.fn(async (email: string) => ({
      token: 'fake-jwt',
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
      user: {
        userId: 1, employeeId: 1, email, role: Role.Admin,
        fullName: 'Ana Admin', department: 'RR. HH.',
      },
    })),
    me: vi.fn(),
  },
}))

import { useAuthStore } from './auth'

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('empieza sin autenticar', () => {
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(false)
    expect(auth.isAdmin).toBe(false)
  })

  it('login guarda token y usuario y persiste el token', async () => {
    const auth = useAuthStore()
    await auth.login('admin@hria.local', 'Demo1234!')

    expect(auth.isAuthenticated).toBe(true)
    expect(auth.isAdmin).toBe(true)
    expect(auth.user?.email).toBe('admin@hria.local')
    expect(localStorage.getItem('hria.token')).toBe('fake-jwt')
  })

  it('logout limpia el estado y el almacenamiento', async () => {
    const auth = useAuthStore()
    await auth.login('admin@hria.local', 'Demo1234!')
    auth.logout()

    expect(auth.isAuthenticated).toBe(false)
    expect(auth.user).toBeNull()
    expect(localStorage.getItem('hria.token')).toBeNull()
  })
})
