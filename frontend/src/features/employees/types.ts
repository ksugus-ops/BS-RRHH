import type { Role } from '@/features/auth/types'

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface EmployeeListItem {
  id: number
  fullName: string
  email: string
  departmentId: number
  departmentName: string
  position: string
  isActive: boolean
  role: Role | null
}

export interface EmployeeDetail {
  id: number
  firstName: string
  lastName: string
  email: string
  departmentId: number
  departmentName: string
  position: string
  hireDate: string
  isActive: boolean
  role: Role | null
  createdAt: string
  updatedAt: string
}

export interface Department {
  id: number
  name: string
  isActive: boolean
}

export interface EmployeeFormData {
  firstName: string
  lastName: string
  email: string
  departmentId: number | null
  position: string
  hireDate: string
  role: Role
  initialPassword?: string
}

export interface EmployeeListParams {
  search?: string
  departmentId?: number | null
  isActive?: boolean | null
  page: number
  pageSize: number
}
