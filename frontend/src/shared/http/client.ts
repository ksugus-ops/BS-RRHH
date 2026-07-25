import axios, { type AxiosInstance, type AxiosError } from 'axios'

/**
 * Cliente HTTP centralizado.
 * - baseURL configurable por variable de entorno (VITE_API_BASE_URL); por defecto usa el proxy de Vite.
 * - Interceptor de request: añade el token JWT (se conecta con el store de auth en la Fase 3).
 * - Interceptor de response: normaliza errores y gestiona 401 (se completará en la Fase 3).
 */
const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const http: AxiosInstance = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

// Proveedor del token; se sustituye por el store de auth en la Fase 3.
let tokenProvider: () => string | null = () => null
export function setTokenProvider(provider: () => string | null) {
  tokenProvider = provider
}

// Handler de sesión expirada; se conecta con el store de auth en la Fase 3.
let onUnauthorized: () => void = () => {}
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

http.interceptors.request.use((config) => {
  const token = tokenProvider()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export interface ApiError {
  status: number
  title: string
  errors?: Record<string, string[]>
}

/** Forma del cuerpo de error que devuelve la API (ProblemDetails simplificado). */
interface ApiErrorBody {
  title?: string
  errors?: Record<string, string[]>
}

http.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiErrorBody>) => {
    if (error.response?.status === 401) {
      onUnauthorized()
    }
    const apiError: ApiError = {
      status: error.response?.status ?? 0,
      title: error.response?.data?.title ?? error.message ?? 'Error de red',
      errors: error.response?.data?.errors,
    }
    return Promise.reject(apiError)
  },
)
