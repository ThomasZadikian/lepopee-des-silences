// @vitest-environment jsdom
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import DeveloperIslandPage from './DeveloperIslandPage.vue';

const router = { replace: vi.fn() };
const runStore = {
  findDeveloperSandbox: vi.fn(),
  resetDeveloperSandbox: vi.fn(),
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

  it('offers to resume or reset an existing sandbox without mutating it on arrival', async () => {
    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    expect(runStore.findDeveloperSandbox).toHaveBeenCalledOnce();
    expect(runStore.resetDeveloperSandbox).not.toHaveBeenCalled();
    expect(router.replace).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Reprendre');
    expect(wrapper.text()).toContain('Réinitialiser');

    await wrapper.get('[data-testid="resume-sandbox"]').trigger('click');
    expect(router.replace).toHaveBeenCalledWith('/run/sandbox-1');
  });

  it('offers to create a sandbox when none exists', async () => {
    runStore.currentRun = null;

    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    expect(wrapper.text()).toContain('Créer l’environnement');

    runStore.resetDeveloperSandbox.mockImplementationOnce(async () => {
      runStore.currentRun = { id: 'sandbox-2' };
    });
    await wrapper.get('[data-testid="reset-sandbox"]').trigger('click');
    await flushPromises();

    expect(router.replace).toHaveBeenCalledWith('/run/sandbox-2');
  });

  it('requires confirmation before replacing an existing sandbox', async () => {
    const wrapper = mount(DeveloperIslandPage, {
      global: { stubs: { LivingWalls: true, SessionMenu: true } },
    });
    await flushPromises();

    const reset = wrapper.get('[data-testid="reset-sandbox"]');
    await reset.trigger('click');
    expect(runStore.resetDeveloperSandbox).not.toHaveBeenCalled();
    expect(reset.text()).toContain('Confirmer');

    await reset.trigger('click');
    await flushPromises();
    expect(runStore.resetDeveloperSandbox).toHaveBeenCalledOnce();
  });

  it('keeps the entry page available when sandbox lookup fails', async () => {
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
