// Objeto const (en vez de enum) por 'erasableSyntaxOnly' de TS. Coincide con el backend.
export const Role = {
  Admin: 1,
  Employee: 2,
} as const

export type Role = (typeof Role)[keyof typeof Role]

export interface CurrentUser {
  userId: number
  employeeId: number
  email: string
  role: Role
  fullName: string
  department: string | null
  /** Imagen de perfil. Si es nula se genera un avatar con las iniciales. */
  avatarUrl: string | null
}

export interface LoginResponse {
  token: string
  expiresAt: string
  user: CurrentUser
}
