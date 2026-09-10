// @vitest-environment jsdom
import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';

import DeveloperIslandPage from './DeveloperIslandPage.vue';

const router = { replace: vi.fn() };
const runStore = {
  startDeveloperSandbox: vi.fn(),
  currentRun: { id: 'sandbox-1' },
  runActionError: null,
  error: null,
};

vi.mock('vue-router', () => ({ useRouter: () => router }));
vi.mock('../features/runs/stores/runStore', () => ({ useRunStore: () => runStore }));

describe('DeveloperIslandPage', () => {
  it('resets the sandbox then opens its run', async () => {
    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    expect(runStore.startDeveloperSandbox).toHaveBeenCalledOnce();
    expect(router.replace).toHaveBeenCalledWith('/run/sandbox-1');
    expect(wrapper.text()).toContain('Île des développeurs');
  });
});
