import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { demoPlayerId } from '../../runs/stores/runStore';
import { playerApi } from '../api/playerApi';
import type { PlayerProfileView, PlayerStatKind } from '../types/playerTypes';

export const usePlayerStore = defineStore('player', () => {
  const profile = ref<PlayerProfileView | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const mainCharacter = computed(() => profile.value?.characters[0] ?? null);
  const equippedCount = computed(
    () => mainCharacter.value?.skills.filter((skill) => skill.isEquipped).length ?? 0,
  );
  const isLoadoutFull = computed(
    () => equippedCount.value >= (mainCharacter.value?.maxEquippedSkills ?? 0),
  );
  const unspentStatPoints = computed(() => profile.value?.progression.unspentStatPoints ?? 0);

  async function execute(action: () => Promise<void>) {
    isLoading.value = true;
    error.value = null;
    try {
      await action();
    } catch (caught) {
      error.value = caught instanceof Error
        ? caught.message
        : 'Une erreur inconnue est survenue.';
    } finally {
      isLoading.value = false;
    }
  }

  async function loadProfile(playerId: string = demoPlayerId) {
    await execute(async () => {
      profile.value = await playerApi.getProfile(playerId);
    });
  }

  async function equipSkill(characterId: string, skillKey: string) {
    await execute(async () => {
      profile.value = await playerApi.equipSkill(demoPlayerId, characterId, skillKey);
    });
  }

  async function unequipSkill(characterId: string, skillKey: string) {
    await execute(async () => {
      profile.value = await playerApi.unequipSkill(demoPlayerId, characterId, skillKey);
    });
  }

  async function spendStatPoint(characterId: string, stat: PlayerStatKind) {
    await execute(async () => {
      profile.value = await playerApi.spendStatPoint(demoPlayerId, characterId, stat);
    });
  }

  return {
    profile,
    isLoading,
    error,
    mainCharacter,
    equippedCount,
    isLoadoutFull,
    unspentStatPoints,
    loadProfile,
    equipSkill,
    unequipSkill,
    spendStatPoint,
  };
});
