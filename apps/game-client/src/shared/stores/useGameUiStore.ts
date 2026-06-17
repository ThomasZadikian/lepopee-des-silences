import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useGameUiStore = defineStore('gameUi', () => {
  const isBesaceOpen = ref(false);
  const isJournalOpen = ref(false);
  const isLawsOpen = ref(false);
  const isPartyOpen = ref(false);
  const activeDrawer = ref<'node' | 'besace' | 'journal' | 'party' | null>(null);

  function openDrawer(name: 'node' | 'besace' | 'journal' | 'party') {
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

  function toggleParty() {
    isLawsOpen.value = false;
    if (activeDrawer.value === 'party') closeDrawer();
    else openDrawer('party');
  }

  function closeAll() {
    activeDrawer.value = null;
    isLawsOpen.value = false;
  }

  return {
    isBesaceOpen,
    isJournalOpen,
    isLawsOpen,
    isPartyOpen,
    activeDrawer,
    openDrawer,
    closeDrawer,
    closeAll,
    toggleBesace,
    toggleJournal,
    toggleLaws,
    toggleParty,
  };
});
