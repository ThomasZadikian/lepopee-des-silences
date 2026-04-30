import type { PlayerInventory } from "@/interfaces/playerInventory";
import api from "./auth";

export const inventoryApi = {
  getAll: () => api.get<{ items: PlayerInventory[] }>("/playerinventories"),
  getById: (id: number) =>
    api.get<{ playerInventory: PlayerInventory }>(`/playerinventories/${id}`),
  delete: (id: number) => api.delete(`/playerinventories/${id}`),
};
