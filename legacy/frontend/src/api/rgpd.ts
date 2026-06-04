import api from './auth'

export const rgpdApi = {
    exportData: () => api.get('/rgpd/export'),
    exportJson: () => api.get('/rgpd/export/json', { responseType: 'blob' }),
    deleteAccount: (reason?: string) => api.delete('/rgpd/me', { data: reason }),
}