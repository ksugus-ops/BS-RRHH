import { http } from '@/shared/http/client'
import type {
  ScheduleAssignment,
  ScheduleDetail,
  ScheduleListItem,
  ScheduleSlotInput,
} from './types'

export interface ScheduleInput {
  name: string
  description: string | null
  slots: ScheduleSlotInput[]
}

export const schedulesApi = {
  async list(includeInactive = false): Promise<ScheduleListItem[]> {
    const { data } = await http.get<ScheduleListItem[]>('/schedules', {
      params: { includeInactive },
    })
    return data
  },

  async get(id: number): Promise<ScheduleDetail> {
    const { data } = await http.get<ScheduleDetail>(`/schedules/${id}`)
    return data
  },

  async create(input: ScheduleInput): Promise<ScheduleDetail> {
    const { data } = await http.post<ScheduleDetail>('/schedules', input)
    return data
  },

  async update(id: number, input: ScheduleInput & { isActive: boolean }): Promise<ScheduleDetail> {
    const { data } = await http.put<ScheduleDetail>(`/schedules/${id}`, input)
    return data
  },

  async deactivate(id: number): Promise<void> {
    await http.post(`/schedules/${id}/deactivate`)
  },

  // --- Asignaciones ---

  async assignments(params: { employeeId?: number; scheduleId?: number } = {}): Promise<ScheduleAssignment[]> {
    const { data } = await http.get<ScheduleAssignment[]>('/schedules/assignments', {
      params: { employeeId: params.employeeId ?? undefined, scheduleId: params.scheduleId ?? undefined },
    })
    return data
  },

  async assign(input: {
    scheduleId: number
    employeeId: number
    startDate: string
    endDate: string | null
  }): Promise<ScheduleAssignment> {
    const { data } = await http.post<ScheduleAssignment>('/schedules/assignments', input)
    return data
  },

  async removeAssignment(id: number): Promise<void> {
    await http.delete(`/schedules/assignments/${id}`)
  },

  /** Horario vigente de un empleado. Devuelve null si no tiene ninguno (204). */
  async effective(employeeId: number, date?: string): Promise<ScheduleDetail | null> {
    const { data, status } = await http.get<ScheduleDetail | ''>(`/schedules/effective/${employeeId}`, {
      params: { date: date || undefined },
    })
    return status === 204 || !data ? null : (data as ScheduleDetail)
  },
}
