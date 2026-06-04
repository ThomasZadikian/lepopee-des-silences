import api from './auth'
import type { Enemy } from '@/interfaces/bestiary'

export const enemiesApi = {
  getAll:    ()             => api.get<{ items: Enemy[] }>('/enemies'),
  getById:   (id: number)   => api.get<{ enemy: Enemy }>(`/enemies/${id}`),
  getByType: (type: string) => api.get<{ items: Enemy[] }>(`/enemies/type/${type}`),
}