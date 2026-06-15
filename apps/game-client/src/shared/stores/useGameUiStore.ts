import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useGameUiStore = defineStore('gameUi', () => {
  const isBesaceOpen = ref(false);
  const isJournalOpen = ref(false);
  const isLawsOpen = ref(false);
  const activeDrawer = ref<'node' | 'besace' | 'journal' | null>(null);

  function openDrawer(name: 'node' | 'besace' | 'journal') {
    activeDrawer.value = name;
  }

  function closeDrawer() {
    activeDrawer.value = null;
  }

  function toggleBesace() {
    isLawsOpen.value = false;
    if (activeDrawer.value === 'besace') closeDrawer();
    else openDrawer('besace');
  }

  function toggleJournal() {
    if (activeDrawer.value === 'journal') closeDrawer();
    else openDrawer('journal');
  }

  function toggleLaws() {
    activeDrawer.value = null;
    isLawsOpen.value = !isLawsOpen.value;
  }

  return {
    isBesaceOpen,
    isJournalOpen,
    isLawsOpen,
    activeDrawer,
    openDrawer,
    closeDrawer,
    toggleBesace,
    toggleJournal,
    toggleLaws,
  };
});
