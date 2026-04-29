import api from './auth';

export interface GameSave 
{
    id:  number; 
    playedId: number; 
    saveName: string; 
    currentZone: string; 
    playerLevel: number; 
    createdAt: string; 
}

export interface CreateGameSaveDto 
{
    playerId: number;
    saveName: string; 
    currentZone: string; 
    playerLevel: number; 
}
    export const gameSavesApi = 
    {
        getById: (id:number) => api.get<GameSave>(`/gamesaves/${id}`), 
        create: (data:CreateGameSaveDto) => api.post<GameSave>('/gamesaves', data),
        delete: (id:number) => api.delete(`/gamesaves/${id}`),
}