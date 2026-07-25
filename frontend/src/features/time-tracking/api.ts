import { http } from '@/shared/http/client'
import type { TimeStatus, Workday } from './types'

export interface WorkdayFilter {
  employeeId?: number | null
  from?: string | null
  to?: string | null
}

export const timeApi = {
  async status(): Promise<TimeStatus> {
    const { data } = await http.get<TimeStatus>('/time/status')
    return data
  },
  async checkIn(): Promise<TimeStatus> {
    const { data } = await http.post<TimeStatus>('/time/check-in')
    return data
  },
  async startBreak(): Promise<TimeStatus> {
    const { data } = await http.post<TimeStatus>('/time/break/start')
    return data
  },
  async endBreak(): Promise<TimeStatus> {
    const { data } = await http.post<TimeStatus>('/time/break/end')
    return data
  },
  async checkOut(): Promise<Workday> {
    const { data } = await http.post<Workday>('/time/check-out')
    return data
  },
  async workdays(filter: WorkdayFilter = {}): Promise<Workday[]> {
    const { data } = await http.get<Workday[]>('/time/workdays', {
      params: {
        employeeId: filter.employeeId ?? undefined,
        from: filter.from || undefined,
        to: filter.to || undefined,
      },
    })
    return data
  },
}
