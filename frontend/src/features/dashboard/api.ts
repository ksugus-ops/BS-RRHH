import { http } from '@/shared/http/client'
import type {
  AbsenceByType,
  DashboardSummary,
  HoursByDayPoint,
  MonthActivity,
  Punctuality,
  UpcomingAbsences,
  VacationSummary,
} from './types'

export const dashboardApi = {
  async summary(): Promise<DashboardSummary> {
    const { data } = await http.get<DashboardSummary>('/dashboard/summary')
    return data
  },
  async hoursByDay(from?: string, to?: string): Promise<HoursByDayPoint[]> {
    const { data } = await http.get<HoursByDayPoint[]>('/dashboard/hours-by-day', {
      params: { from: from || undefined, to: to || undefined },
    })
    return data
  },
  async absencesByType(year?: number): Promise<AbsenceByType[]> {
    const { data } = await http.get<AbsenceByType[]>('/dashboard/absences-by-type', {
      params: { year: year ?? undefined },
    })
    return data
  },
  async vacationSummary(year?: number): Promise<VacationSummary> {
    const { data } = await http.get<VacationSummary>('/dashboard/vacation-summary', {
      params: { year: year ?? undefined },
    })
    return data
  },
  async upcomingAbsences(): Promise<UpcomingAbsences> {
    const { data } = await http.get<UpcomingAbsences>('/dashboard/upcoming-absences')
    return data
  },
  async monthActivity(): Promise<MonthActivity> {
    const { data } = await http.get<MonthActivity>('/dashboard/month-activity')
    return data
  },
  async punctuality(toleranceMinutes = 5): Promise<Punctuality> {
    const { data } = await http.get<Punctuality>('/dashboard/punctuality', {
      params: { toleranceMinutes },
    })
    return data
  },
}
