import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

import { demoPlayerId } from '../../runs/stores/runStore';
import { playerApi } from '../api/playerApi';
import type { PlayerProfileView } from '../types/playerTypes';

export const usePlayerStore = defineStore('player', () => {
  const profile = ref<PlayerProfileView | null>(null);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  const mainCharacter = computed(() => profile.value?.characters[0] ?? null);
  const permanentItems = computed(() => profile.value?.permanentItems ?? []);

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

  async function equipItem(characterId: string, itemKey: string) {
    await execute(async () => {
      profile.value = await playerApi.equipItem(demoPlayerId, characterId, itemKey);
    });
  }

  async function unequipItem(characterId: string, itemKey: string) {
    await execute(async () => {
      profile.value = await playerApi.unequipItem(demoPlayerId, characterId, itemKey);
    });
  }

  return {
    profile,
    isLoading,
    error,
    mainCharacter,
    permanentItems,
    loadProfile,
    equipSkill,
    unequipSkill,
    equipItem,
    unequipItem,
  };
});
