export const AbsenceStatus = {
  Pending: 1,
  Approved: 2,
  Rejected: 3,
  Cancelled: 4,
} as const
export type AbsenceStatus = (typeof AbsenceStatus)[keyof typeof AbsenceStatus]

export const ABSENCE_STATUS_LABEL: Record<number, string> = {
  1: 'Pendiente',
  2: 'Aprobada',
  3: 'Rechazada',
  4: 'Retirada',
}

/** Severidad de PrimeVue para cada estado. */
export const ABSENCE_STATUS_SEVERITY: Record<number, string> = {
  1: 'warn',
  2: 'success',
  3: 'danger',
  4: 'secondary',
}

export interface AbsenceType {
  id: number
  code: string
  name: string
  consumesVacationBalance: boolean
  requiresApproval: boolean
  colorHex: string | null
}

export interface AbsenceRequest {
  id: number
  employeeId: number
  employeeName: string
  absenceTypeId: number
  absenceTypeName: string
  absenceTypeCode: string
  colorHex: string | null
  startDate: string
  endDate: string
  workingDays: number
  status: AbsenceStatus
  reason: string | null
  requestedAt: string
  decidedAt: string | null
  decidedBy: string | null
  decisionComment: string | null
}

export interface VacationBalance {
  employeeId: number
  employeeName: string
  year: number
  allowanceDays: number
  approvedDays: number
  pendingDays: number
  availableDays: number
}

export interface CalendarAbsence {
  id: number
  startDate: string
  endDate: string
  absenceTypeName: string
  absenceTypeCode: string
  colorHex: string | null
  status: AbsenceStatus
  workingDays: number
}

export interface EmployeeYearAbsences {
  employeeId: number
  employeeName: string
  departmentName: string
  absences: CalendarAbsence[]
}

export interface VacationCalendar {
  year: number
  employees: EmployeeYearAbsences[]
}
