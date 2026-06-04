import api from './auth'
import type { BestiaryUnlock } from '@/interfaces/bestiary'

export const bestiaryApi = {
  getMe:  () => api.get<{ items: BestiaryUnlock[] }>('/bestiaryunlocks/me'),
  getAll: () => api.get<{ items: BestiaryUnlock[] }>('/bestiaryunlocks'),
}