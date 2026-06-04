import api from './auth'

export interface CompanionStateResponse {
    currentState:   string
    lastUpdated:    string
    victoriesToday: number
    defeatesToday:  number
}

export const companionApi = {
    getState: () => api.get<CompanionStateResponse>('/companion/state')
}