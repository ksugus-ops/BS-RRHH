export const HolidayKind = {
  Nacional: 1,
  Autonomico: 2,
  Local: 3,
  Convenio: 4,
  Empresa: 5,
} as const
export type HolidayKind = (typeof HolidayKind)[keyof typeof HolidayKind]

export const HOLIDAY_KINDS: { value: HolidayKind; label: string }[] = [
  { value: HolidayKind.Nacional, label: 'Nacional' },
  { value: HolidayKind.Autonomico, label: 'Autonómico' },
  { value: HolidayKind.Local, label: 'Local' },
  { value: HolidayKind.Convenio, label: 'Convenio' },
  { value: HolidayKind.Empresa, label: 'Empresa' },
]

export function holidayKindLabel(kind: HolidayKind | null): string {
  return HOLIDAY_KINDS.find((k) => k.value === kind)?.label ?? '—'
}

export interface Holiday {
  id: number
  date: string
  name: string
  kind: HolidayKind
}

export interface WorkCalendarListItem {
  id: number
  year: number
  name: string
  isActive: boolean
  nonWorkingWeekDays: number[]
  holidayCount: number
}

export interface WorkCalendarDetail {
  id: number
  year: number
  name: string
  isActive: boolean
  nonWorkingWeekDays: number[]
  holidays: Holiday[]
  workingDaysInYear: number
  createdAt: string
  updatedAt: string
}

/** Un día de la vista anual. */
export interface CalendarDay {
  date: string
  isWorkingDay: boolean
  isWeekend: boolean
  holidayName: string | null
  holidayKind: HolidayKind | null
}

export const MONTH_NAMES = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
]
