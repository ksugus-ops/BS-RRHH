export const TimeState = {
  NotStarted: 0,
  Working: 1,
  OnBreak: 2,
} as const
export type TimeState = (typeof TimeState)[keyof typeof TimeState]

export const WorkdayStatus = {
  Open: 1,
  Completed: 2,
  Incomplete: 3,
} as const
export type WorkdayStatus = (typeof WorkdayStatus)[keyof typeof WorkdayStatus]

export interface Break {
  id: number
  startTime: string
  endTime: string | null
  durationMinutes: number
}

export interface Workday {
  id: number
  employeeId: number
  date: string
  checkIn: string
  checkOut: string | null
  status: WorkdayStatus
  workedMinutes: number
  breaks: Break[]
  /** Minutos previstos por el horario. null = el empleado no tiene horario asignado. */
  expectedMinutes: number | null
  /** Trabajados − previstos. Negativo = falta jornada. null si no hay previsión. */
  deviationMinutes: number | null
}

export interface TimeStatus {
  state: TimeState
  workday: Workday | null
}
