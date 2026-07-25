import { http } from '@/shared/http/client'
import type {
  Department,
  EmployeeDetail,
  EmployeeFormData,
  EmployeeListItem,
  EmployeeListParams,
  PagedResult,
} from './types'

export const employeesApi = {
  async list(params: EmployeeListParams): Promise<PagedResult<EmployeeListItem>> {
    const { data } = await http.get<PagedResult<EmployeeListItem>>('/employees', {
      params: {
        search: params.search || undefined,
        departmentId: params.departmentId ?? undefined,
        isActive: params.isActive ?? undefined,
        page: params.page,
        pageSize: params.pageSize,
      },
    })
    return data
  },

  async get(id: number): Promise<EmployeeDetail> {
    const { data } = await http.get<EmployeeDetail>(`/employees/${id}`)
    return data
  },

  async create(payload: EmployeeFormData): Promise<EmployeeDetail> {
    const { data } = await http.post<EmployeeDetail>('/employees', payload)
    return data
  },

  async update(id: number, payload: EmployeeFormData): Promise<EmployeeDetail> {
    const { data } = await http.put<EmployeeDetail>(`/employees/${id}`, payload)
    return data
  },

  async deactivate(id: number): Promise<void> {
    await http.post(`/employees/${id}/deactivate`)
  },

  async departments(): Promise<Department[]> {
    const { data } = await http.get<Department[]>('/departments')
    return data
  },
}
