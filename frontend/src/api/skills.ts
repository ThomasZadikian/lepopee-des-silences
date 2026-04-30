import type { PlayerSkill, Skill } from "@/interfaces/playerSkill";
import api from "./auth";

export const playerSkillsApi = {
  getAll: () => api.get<{ items: PlayerSkill[] }>("/playerskills"),
  getById: (id: number) =>
    api.get<{ playerSkill: PlayerSkill }>(`/playerskills/${id}`),
};

export const skillsApi = {
  getAll: () => api.get<{ items: Skill[] }>("/skills"),
};
