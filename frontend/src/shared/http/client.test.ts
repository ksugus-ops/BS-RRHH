import { describe, it, expect } from 'vitest'
import { http } from './client'

// Test smoke del andamiaje: verifica que el cliente HTTP centralizado
// se crea con la configuración esperada.
describe('http client', () => {
  it('usa una baseURL por defecto', () => {
    expect(http.defaults.baseURL).toBeDefined()
  })

  it('envía JSON por defecto', () => {
    expect(http.defaults.headers['Content-Type']).toBe('application/json')
  })
})
