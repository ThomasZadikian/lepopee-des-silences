import type { PlayerSkill, Skill } from "@/interfaces/playerSkill";
import api from "./auth";

export const playerSkillsApi = {
  getAll: () => api.get<{ items: PlayerSkill[] }>("/playerskills"),
  getMe: () => api.get<{ items: PlayerSkill[] }>("/playerskills/me"),
};

export const skillsApi = {
  getAll: () => api.get<{ items: Skill[] }>("/skills"),
};
