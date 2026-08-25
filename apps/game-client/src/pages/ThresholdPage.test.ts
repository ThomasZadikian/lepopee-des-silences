// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import ThresholdPage from './ThresholdPage.vue';

const router = { push: vi.fn() };
const runStore: {
  resumableRun: { id: string; seed: string; currentRoomNumber: number; status: string } | null;
  currentRun: { id: string } | null;
  isLoading: boolean;
  isLoadingResumableRun: boolean;
  error: string | null;
  runActionError: string | null;
  loadResumableRun: ReturnType<typeof vi.fn>;
  loadRun: ReturnType<typeof vi.fn>;
  startRun: ReturnType<typeof vi.fn>;
  abandonResumableRun: ReturnType<typeof vi.fn>;
} = {
  resumableRun: null,
  currentRun: null,
  isLoading: false,
  isLoadingResumableRun: false,
  error: null,
  runActionError: null,
  loadResumableRun: vi.fn(),
  loadRun: vi.fn(),
  startRun: vi.fn(),
  abandonResumableRun: vi.fn(),
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
    runStore.resumableRun = null;
    runStore.currentRun = null;
    runStore.runActionError = null;
    runStore.abandonResumableRun.mockResolvedValue(true);
    runStore.startRun.mockImplementation(async () => {
      runStore.currentRun = { id: 'run-tactical' };
    });
  });

  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('starts a run directly (no resumable run) without an ATB choice', async () => {
    const wrapper = mount(ThresholdPage, { attachTo: document.body });
    await wrapper.findAll('.threshold-link')[1].trigger('click');
    await flushPromises();

    expect(document.body.textContent).not.toContain('Tempo');
    expect(document.body.querySelector('input[value="Atb"]')).toBeNull();
    expect(runStore.startRun).toHaveBeenCalledWith();
    expect(router.push).toHaveBeenCalledWith('/run/run-tactical');
  });

  it('resumes an active run recovered from the backend', async () => {
    runStore.resumableRun = {
      id: 'run-recovered',
      seed: 'old-seed',
      currentRoomNumber: 3,
      status: 'Active',
    };
    runStore.loadRun.mockImplementationOnce(async () => {
      runStore.currentRun = { id: 'run-recovered' };
    });
    const wrapper = mount(ThresholdPage, { attachTo: document.body });

    await wrapper.findAll('.threshold-link')[0].trigger('click');
    await flushPromises();

    expect(runStore.loadRun).toHaveBeenCalledWith('run-recovered');
    expect(router.push).toHaveBeenCalledWith('/run/run-recovered');
  });

  it('abandons the discovered open run before starting a new traversal', async () => {
    runStore.resumableRun = {
      id: 'run-existing',
      seed: 'old-seed',
      currentRoomNumber: 3,
      status: 'Active',
    };
    const wrapper = mount(ThresholdPage, { attachTo: document.body });

    await wrapper.findAll('.threshold-link')[1].trigger('click');
    await document.body.querySelector<HTMLButtonElement>('.confirm-btn--danger')!.click();
    await flushPromises();

    expect(runStore.abandonResumableRun).toHaveBeenCalledWith();
    expect(runStore.startRun).toHaveBeenCalledWith();
    expect(runStore.abandonResumableRun.mock.invocationCallOrder[0])
      .toBeLessThan(runStore.startRun.mock.invocationCallOrder[0]);
  });
});
