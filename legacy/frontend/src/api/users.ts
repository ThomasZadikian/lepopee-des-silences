import type { User } from "@/interfaces/user";
import api from "./auth";

export const usersApi = {
  getAll: () => api.get<{ items: User[] }>("/users"),
  delete: (id: number) => api.delete(`/users/${id}`),
};
