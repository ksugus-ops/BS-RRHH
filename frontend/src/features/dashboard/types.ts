export interface RecentPunch {
  employeeName: string
  department: string
  action: string
  timeUtc: string
}

export interface DashboardSummary {
  activeEmployees: number
  working: number
  onBreak: number
  incompleteWorkdays: number
  hoursTodayMinutes: number
  recentPunches: RecentPunch[]
  /** Minutos previstos hoy por los horarios asignados. */
  expectedTodayMinutes: number
  /** Empleados con jornada prevista hoy. */
  employeesScheduledToday: number
  /** Empleados con ausencia aprobada hoy. */
  onLeaveToday: number
  /** Solicitudes de ausencia pendientes de resolver. */
  pendingAbsenceRequests: number
}

export interface HoursByDayPoint {
  date: string
  hours: number
}

export interface AbsenceByType {
  code: string
  name: string
  colorHex: string | null
  days: number
  requests: number
}

export interface VacationSummary {
  allowanceDays: number
  approvedDays: number
  pendingDays: number
  availableDays: number
}

export interface UpcomingAbsence {
  employeeId: number
  employeeName: string
  departmentName: string
  absenceTypeName: string
  absenceTypeCode: string
  colorHex: string | null
  startDate: string
  endDate: string
  daysThisWeek: number
  daysNextWeek: number
  status: number
}

export interface MonthActivity {
  year: number
  month: number
  workedDays: number
  vacationDays: number
  otherAbsenceDays: number
}

export interface Punctuality {
  year: number
  month: number
  toleranceMinutes: number
  onScheduleCount: number
  offScheduleCount: number
  lateInCount: number
  earlyOutCount: number
  onSchedulePercent: number
}

export interface UpcomingAbsences {
  thisWeekStart: string
  thisWeekEnd: string
  nextWeekStart: string
  nextWeekEnd: string
  absences: UpcomingAbsence[]
}
