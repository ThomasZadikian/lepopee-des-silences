import { beforeEach, describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useGameUiStore } from './useGameUiStore';

describe('useGameUiStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with all drawers closed', () => {
    const store = useGameUiStore();
    expect(store.isBesaceOpen).toBe(false);
    expect(store.isJournalOpen).toBe(false);
    expect(store.isLawsOpen).toBe(false);
    expect(store.isPartyOpen).toBe(false);
    expect(store.activeDrawer).toBeNull();
  });

  it('opens a drawer by name', () => {
    const store = useGameUiStore();
    store.openDrawer('besace');
    expect(store.activeDrawer).toBe('besace');
  });

  it('closes active drawer', () => {
    const store = useGameUiStore();
    store.openDrawer('journal');
    store.closeDrawer();
    expect(store.activeDrawer).toBeNull();
  });

  it('toggles besace open', () => {
    const store = useGameUiStore();
    store.toggleBesace();
    expect(store.activeDrawer).toBe('besace');
  });

  it('toggles besace closed', () => {
    const store = useGameUiStore();
    store.activeDrawer = 'besace';
    store.toggleBesace();
    expect(store.activeDrawer).toBeNull();
  });

  it('closes laws when toggling besace', () => {
    const store = useGameUiStore();
    store.isLawsOpen = true;
    store.toggleBesace();
    expect(store.isLawsOpen).toBe(false);
  });

  it('toggles journal open', () => {
    const store = useGameUiStore();
    store.toggleJournal();
    expect(store.activeDrawer).toBe('journal');
  });

  it('toggles journal closed', () => {
    const store = useGameUiStore();
    store.activeDrawer = 'journal';
    store.toggleJournal();
    expect(store.activeDrawer).toBeNull();
  });

  it('toggles laws open', () => {
    const store = useGameUiStore();
    store.toggleLaws();
    expect(store.isLawsOpen).toBe(true);
  });

  it('toggles laws closed', () => {
    const store = useGameUiStore();
    store.isLawsOpen = true;
    store.toggleLaws();
    expect(store.isLawsOpen).toBe(false);
  });

  it('closes active drawer when toggling laws', () => {
    const store = useGameUiStore();
    store.activeDrawer = 'besace';
    store.toggleLaws();
    expect(store.activeDrawer).toBeNull();
  });

  it('toggles party open', () => {
    const store = useGameUiStore();
    store.toggleParty();
    expect(store.activeDrawer).toBe('party');
  });

  it('toggles party closed', () => {
    const store = useGameUiStore();
    store.activeDrawer = 'party';
    store.toggleParty();
    expect(store.activeDrawer).toBeNull();
  });

  it('closes laws when toggling party', () => {
    const store = useGameUiStore();
    store.isLawsOpen = true;
    store.toggleParty();
    expect(store.isLawsOpen).toBe(false);
  });

  it('closes all drawers', () => {
    const store = useGameUiStore();
    store.activeDrawer = 'besace';
    store.isLawsOpen = true;
    store.closeAll();
    expect(store.activeDrawer).toBeNull();
    expect(store.isLawsOpen).toBe(false);
  });

  it('sets isJournalOpen when journal drawer is active', () => {
    const store = useGameUiStore();
    store.openDrawer('journal');
    expect(store.activeDrawer).toBe('journal');
  });

  it('sets isPartyOpen when party drawer is active', () => {
    const store = useGameUiStore();
    store.openDrawer('party');
    expect(store.activeDrawer).toBe('party');
  });

  it('can switch between different drawers', () => {
    const store = useGameUiStore();
    store.openDrawer('besace');
    expect(store.activeDrawer).toBe('besace');
    store.openDrawer('journal');
    expect(store.activeDrawer).toBe('journal');
    store.openDrawer('party');
    expect(store.activeDrawer).toBe('party');
  });
});
