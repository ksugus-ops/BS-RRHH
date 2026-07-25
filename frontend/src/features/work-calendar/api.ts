import { http } from '@/shared/http/client'
import type { CalendarDay, Holiday, HolidayKind, WorkCalendarDetail, WorkCalendarListItem } from './types'

export const workCalendarApi = {
  async list(): Promise<WorkCalendarListItem[]> {
    const { data } = await http.get<WorkCalendarListItem[]>('/work-calendars')
    return data
  },

  /** Detalle del año. Devuelve null si aún no existe calendario (404). */
  async byYear(year: number): Promise<WorkCalendarDetail | null> {
    try {
      const { data } = await http.get<WorkCalendarDetail>(`/work-calendars/${year}`)
      return data
    } catch (e) {
      if ((e as { status?: number }).status === 404) return null
      throw e
    }
  },

  /** Los días del año. No falla aunque el año no tenga calendario definido. */
  async days(year: number): Promise<CalendarDay[]> {
    const { data } = await http.get<CalendarDay[]>(`/work-calendars/${year}/days`)
    return data
  },

  async create(input: { year: number; name: string; nonWorkingWeekDays: number[] }): Promise<WorkCalendarDetail> {
    const { data } = await http.post<WorkCalendarDetail>('/work-calendars', input)
    return data
  },

  async update(
    id: number,
    input: { name: string; isActive: boolean; nonWorkingWeekDays: number[] },
  ): Promise<WorkCalendarDetail> {
    const { data } = await http.put<WorkCalendarDetail>(`/work-calendars/${id}`, input)
    return data
  },

  async addHoliday(calendarId: number, input: { date: string; name: string; kind: HolidayKind }): Promise<Holiday> {
    const { data } = await http.post<Holiday>(`/work-calendars/${calendarId}/holidays`, input)
    return data
  },

  async removeHoliday(calendarId: number, holidayId: number): Promise<void> {
    await http.delete(`/work-calendars/${calendarId}/holidays/${holidayId}`)
  },
}
