import { http } from '@/shared/http/client'
import type { PagedResult } from '@/features/employees/types'
import type {
  AbsenceRequest,
  AbsenceStatus,
  AbsenceType,
  VacationBalance,
  VacationCalendar,
} from './types'

export interface AbsenceFilter {
  employeeId?: number | null
  absenceTypeId?: number | null
  status?: AbsenceStatus | null
  from?: string | null
  to?: string | null
  page?: number
  pageSize?: number
}

export const absencesApi = {
  async types(): Promise<AbsenceType[]> {
    const { data } = await http.get<AbsenceType[]>('/absences/types')
    return data
  },

  async list(filter: AbsenceFilter = {}): Promise<PagedResult<AbsenceRequest>> {
    const { data } = await http.get<PagedResult<AbsenceRequest>>('/absences', {
      params: {
        employeeId: filter.employeeId ?? undefined,
        absenceTypeId: filter.absenceTypeId ?? undefined,
        status: filter.status ?? undefined,
        from: filter.from || undefined,
        to: filter.to || undefined,
        page: filter.page ?? 1,
        pageSize: filter.pageSize ?? 20,
      },
    })
    return data
  },

  async create(input: {
    absenceTypeId: number
    startDate: string
    endDate: string
    reason: string | null
    employeeId?: number | null
  }): Promise<AbsenceRequest> {
    const { data } = await http.post<AbsenceRequest>('/absences', input)
    return data
  },

  async approve(id: number, comment: string | null): Promise<AbsenceRequest> {
    const { data } = await http.post<AbsenceRequest>(`/absences/${id}/approve`, { comment })
    return data
  },

  async reject(id: number, comment: string | null): Promise<AbsenceRequest> {
    const { data } = await http.post<AbsenceRequest>(`/absences/${id}/reject`, { comment })
    return data
  },

  async cancel(id: number): Promise<AbsenceRequest> {
    const { data } = await http.post<AbsenceRequest>(`/absences/${id}/cancel`)
    return data
  },

  async vacationCalendar(year: number): Promise<VacationCalendar> {
    const { data } = await http.get<VacationCalendar>(`/absences/calendar/${year}`)
    return data
  },
}

export const vacationsApi = {
  async balance(employeeId: number, year?: number): Promise<VacationBalance> {
    const { data } = await http.get<VacationBalance>(`/vacations/balance/${employeeId}`, {
      params: { year: year ?? undefined },
    })
    return data
  },

  async balances(year?: number): Promise<VacationBalance[]> {
    const { data } = await http.get<VacationBalance[]>('/vacations/balances', {
      params: { year: year ?? undefined },
    })
    return data
  },

  async setAllowance(input: { employeeId: number; year: number; days: number }): Promise<VacationBalance> {
    const { data } = await http.put<VacationBalance>('/vacations/allowance', input)
    return data
  },
}
