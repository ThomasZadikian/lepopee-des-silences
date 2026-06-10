import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

import { useCombatStore } from './useCombatStore';

describe('useCombatStore visual feedback state', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    setActivePinia(createPinia());
  });

  it('clears damaged feedback after timeout', () => {
    const store = useCombatStore();

    store.markDamaged(['enemy-1']);

    expect(store.recentlyDamagedIds).toContain('enemy-1');

    vi.advanceTimersByTime(700);

    expect(store.recentlyDamagedIds).not.toContain('enemy-1');
  });

  it('clears guarded feedback after timeout', () => {
    const store = useCombatStore();

    store.markGuarded(['ally-1']);

    expect(store.recentlyGuardedIds).toContain('ally-1');

    vi.advanceTimersByTime(800);

    expect(store.recentlyGuardedIds).not.toContain('ally-1');
  });

  it('clears defeated feedback after timeout', () => {
    const store = useCombatStore();

    store.markDefeated(['enemy-1']);

    expect(store.recentlyDefeatedIds).toContain('enemy-1');

    vi.advanceTimersByTime(900);

    expect(store.recentlyDefeatedIds).not.toContain('enemy-1');
  });

  it('reset clears all visual feedback state and timers', () => {
    const store = useCombatStore();

    store.markDamaged(['enemy-1']);
    store.markGuarded(['ally-1']);
    store.markDefeated(['enemy-2']);
    store.markActing('ally-1');

    store.resetAnimationState();
    vi.advanceTimersByTime(1000);

    expect(store.recentlyDamagedIds).toEqual([]);
    expect(store.recentlyGuardedIds).toEqual([]);
    expect(store.recentlyDefeatedIds).toEqual([]);
    expect(store.recentlyActingId).toBeNull();
  });
});
