import type { CreateGameSaveDto, GameSave } from "@/interfaces/gameSave";
import api from "./auth";

export const gameSavesApi = {
  getAll: () => api.get<{ items: GameSave[] }>("/gamesaves"),
  getMe: () => api.get<{ items: GameSave[] }>("/gamesaves/me"),
  getById: (id: number) => api.get<{ gameSave: GameSave }>(`/gamesaves/${id}`),
  create: (data: CreateGameSaveDto) =>
    api.post<{ id: number; message: string }>("/gamesaves", data),
  delete: (id: number) => api.delete(`/gamesaves/${id}`),
};
