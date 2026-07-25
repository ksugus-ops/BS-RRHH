/** Formatea minutos como "Xh Ym" (o "Ym" si < 1h). */
export function formatMinutes(minutes: number | null | undefined): string {
  if (minutes == null || minutes < 0) return '—'
  const h = Math.floor(minutes / 60)
  const m = Math.round(minutes % 60)
  if (h === 0) return `${m}m`
  return `${h}h ${m.toString().padStart(2, '0')}m`
}

/** Fecha y hora en la zona horaria local del navegador. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleString([], {
    dateStyle: 'short',
    timeStyle: 'short',
  })
}

/** Solo la hora (HH:mm) en local. */
export function formatTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

/** Fecha (sin hora). Acepta ISO o "yyyy-MM-dd". */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'
  const d = value.length <= 10 ? new Date(value + 'T00:00:00') : new Date(value)
  return d.toLocaleDateString()
}
