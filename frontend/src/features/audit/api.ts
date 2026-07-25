import { http } from '@/shared/http/client'
import type { PagedResult } from '@/features/employees/types'

export interface AuditLog {
  id: number
  userEmail: string
  action: string
  entity: string
  entityId: string | null
  details: string | null
  createdAt: string
}

export interface AiQueryLog {
  id: number
  userEmail: string
  question: string
  toolsUsed: string | null
  responseStatus: string
  durationMs: number
  createdAt: string
}

export const auditApi = {
  async audit(page = 1, pageSize = 20): Promise<PagedResult<AuditLog>> {
    const { data } = await http.get<PagedResult<AuditLog>>('/audit', { params: { page, pageSize } })
    return data
  },
  async aiQueries(page = 1, pageSize = 20): Promise<PagedResult<AiQueryLog>> {
    const { data } = await http.get<PagedResult<AiQueryLog>>('/audit/ai', { params: { page, pageSize } })
    return data
  },
}
