// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import ThresholdPage from './ThresholdPage.vue';

const router = { push: vi.fn() };
const runStore: {
  resumableRun: null;
  currentRun: { id: string } | null;
  isLoading: boolean;
  isLoadingResumableRun: boolean;
  error: string | null;
  loadResumableRun: ReturnType<typeof vi.fn>;
  loadRun: ReturnType<typeof vi.fn>;
  startRun: ReturnType<typeof vi.fn>;
} = {
  resumableRun: null,
  currentRun: null,
  isLoading: false,
  isLoadingResumableRun: false,
  error: null,
  loadResumableRun: vi.fn(),
  loadRun: vi.fn(),
  startRun: vi.fn(),
};

vi.mock('vue-router', () => ({
  useRouter: () => router,
}));

vi.mock('../features/runs/stores/runStore', () => ({
  useRunStore: () => runStore,
}));

describe('ThresholdPage tactical-only flow', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    runStore.currentRun = null;
    runStore.startRun.mockImplementation(async () => {
      runStore.currentRun = { id: 'run-tactical' };
    });
  });

  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('presents tactical combat without an ATB choice', async () => {
    const wrapper = mount(ThresholdPage, { attachTo: document.body });
    await wrapper.findAll('.ribbon__head')[1].trigger('click');
    await wrapper.find('.ribbon__btn').trigger('click');

    expect(document.body.textContent).toContain('Combat tactique');
    expect(document.body.textContent).not.toContain('Tempo');
    expect(document.body.querySelector('input[value="Atb"]')).toBeNull();
  });

  it('starts a run without sending a combat-mode argument', async () => {
    const wrapper = mount(ThresholdPage, { attachTo: document.body });
    await wrapper.findAll('.ribbon__head')[1].trigger('click');
    await wrapper.find('.ribbon__btn').trigger('click');

    const generate = [...document.body.querySelectorAll('.confirm-dialog button')]
      .find((button) => button.textContent?.includes('Générer une run'));
    generate?.click();
    await flushPromises();

    expect(runStore.startRun).toHaveBeenCalledWith();
    expect(router.push).toHaveBeenCalledWith('/run/run-tactical');
  });
});
