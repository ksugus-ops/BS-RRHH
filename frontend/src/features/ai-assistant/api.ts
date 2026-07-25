import { http } from '@/shared/http/client'

export interface AiAskResponse {
  answer: string
  toolsUsed: string[]
  mode: string
  status: string
}

export const aiApi = {
  async ask(question: string): Promise<AiAskResponse> {
    const { data } = await http.post<AiAskResponse>('/ai/ask', { question })
    return data
  },
}
