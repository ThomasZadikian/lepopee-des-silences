// @vitest-environment jsdom
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import DeveloperIslandPage from './DeveloperIslandPage.vue';

const router = { replace: vi.fn() };
const runStore = {
  startDeveloperSandbox: vi.fn(),
  currentRun: { id: 'sandbox-1' } as { id: string } | null,
  runActionError: null as string | null,
  error: null as string | null,
};

vi.mock('vue-router', () => ({ useRouter: () => router }));
vi.mock('../features/runs/stores/runStore', () => ({ useRunStore: () => runStore }));

describe('DeveloperIslandPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    runStore.currentRun = { id: 'sandbox-1' };
    runStore.runActionError = null;
    runStore.error = null;
  });

  it('resets the sandbox then opens its run', async () => {
    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    expect(runStore.startDeveloperSandbox).toHaveBeenCalledOnce();
    expect(router.replace).toHaveBeenCalledWith('/run/sandbox-1');
    expect(wrapper.text()).toContain('Île des développeurs');
  });

  it('stays on the entry page and displays the initialization error', async () => {
    runStore.currentRun = null;
    runStore.error = 'Sandbox indisponible';

    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    expect(router.replace).not.toHaveBeenCalled();
    expect(wrapper.get('[role="alert"]').text()).toBe('Sandbox indisponible');
  });
});
