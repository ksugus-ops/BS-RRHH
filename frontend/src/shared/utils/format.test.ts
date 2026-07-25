import { describe, it, expect } from 'vitest'
import { formatMinutes } from './format'

describe('formatMinutes', () => {
  it('formatea horas y minutos', () => {
    expect(formatMinutes(450)).toBe('7h 30m')
  })

  it('muestra solo minutos si es menos de una hora', () => {
    expect(formatMinutes(45)).toBe('45m')
  })

  it('rellena los minutos a dos dígitos con horas', () => {
    expect(formatMinutes(65)).toBe('1h 05m')
  })

  it('devuelve guion para valores nulos o negativos', () => {
    expect(formatMinutes(null)).toBe('—')
    expect(formatMinutes(-5)).toBe('—')
  })
})
