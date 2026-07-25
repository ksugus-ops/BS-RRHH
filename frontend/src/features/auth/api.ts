import { http } from '@/shared/http/client'
import type { CurrentUser, LoginResponse } from './types'

export const authApi = {
  async login(email: string, password: string): Promise<LoginResponse> {
    const { data } = await http.post<LoginResponse>('/auth/login', { email, password })
    return data
  },

  async me(): Promise<CurrentUser> {
    const { data } = await http.get<CurrentUser>('/auth/me')
    return data
  },

  /** Cambia la contraseña del usuario autenticado. */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await http.post('/auth/change-password', { currentPassword, newPassword })
  },
}
