export interface ScheduleSlot {
  id: number
  dayOfWeek: number
  startTime: string
  endTime: string
  durationMinutes: number
}

export interface ScheduleSlotInput {
  dayOfWeek: number
  startTime: string
  endTime: string
}

export interface ScheduleListItem {
  id: number
  name: string
  description: string | null
  isActive: boolean
  weeklyMinutes: number
  slotCount: number
  assignedEmployees: number
}

export interface ScheduleDetail {
  id: number
  name: string
  description: string | null
  isActive: boolean
  weeklyMinutes: number
  slots: ScheduleSlot[]
  createdAt: string
  updatedAt: string
}

export interface ScheduleAssignment {
  id: number
  scheduleId: number
  scheduleName: string
  employeeId: number
  employeeName: string
  startDate: string
  endDate: string | null
  isCurrent: boolean
}

/** Lunes primero: es el orden natural de una semana laboral en España. */
export const WEEKDAYS: { value: number; label: string; short: string }[] = [
  { value: 1, label: 'Lunes', short: 'L' },
  { value: 2, label: 'Martes', short: 'M' },
  { value: 3, label: 'Miércoles', short: 'X' },
  { value: 4, label: 'Jueves', short: 'J' },
  { value: 5, label: 'Viernes', short: 'V' },
  { value: 6, label: 'Sábado', short: 'S' },
  { value: 0, label: 'Domingo', short: 'D' },
]

export function weekdayLabel(value: number): string {
  return WEEKDAYS.find((d) => d.value === value)?.label ?? '—'
}
