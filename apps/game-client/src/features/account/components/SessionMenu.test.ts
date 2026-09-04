// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import SessionMenu from './SessionMenu.vue';

const router = { replace: vi.fn() };
const api = vi.hoisted(() => ({ logout: vi.fn() }));
const auth = vi.hoisted(() => ({
  getAccessToken: vi.fn(() => 'access-token'),
  clearAuthenticatedSession: vi.fn(),
}));
const runStore = { clearForLogout: vi.fn() };

vi.mock('vue-router', () => ({ useRouter: () => router }));
vi.mock('../../../shared/api/playerApi', () => ({ playerApi: api }));
vi.mock('../authSession', () => auth);
vi.mock('../../runs/stores/runStore', () => ({ useRunStore: () => runStore }));

describe('SessionMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.getAccessToken.mockReturnValue('access-token');
    api.logout.mockResolvedValue(undefined);
  });

  it('revokes the server session, clears local gameplay state and returns to login', async () => {
    const wrapper = mount(SessionMenu);
    await wrapper.get('[aria-label="Ouvrir le menu de session"]').trigger('click');
    await wrapper.get('.session-menu__logout').trigger('click');
    await flushPromises();

    expect(api.logout).toHaveBeenCalledWith('access-token');
    expect(auth.clearAuthenticatedSession).toHaveBeenCalled();
    expect(runStore.clearForLogout).toHaveBeenCalled();
    expect(router.replace).toHaveBeenCalledWith({ name: 'login' });
  });

  it('still clears the local session when no access token remains', async () => {
    auth.getAccessToken.mockReturnValueOnce(null);
    const wrapper = mount(SessionMenu);
    await wrapper.get('[aria-label="Ouvrir le menu de session"]').trigger('click');
    await wrapper.get('.session-menu__logout').trigger('click');
    await flushPromises();

    expect(api.logout).not.toHaveBeenCalled();
    expect(auth.clearAuthenticatedSession).toHaveBeenCalled();
    expect(runStore.clearForLogout).toHaveBeenCalled();
    expect(router.replace).toHaveBeenCalledWith({ name: 'login' });
  });

  it('keeps the session available for retry and displays server logout errors', async () => {
    api.logout.mockRejectedValueOnce(new Error('Service indisponible'));
    const wrapper = mount(SessionMenu);
    await wrapper.get('[aria-label="Ouvrir le menu de session"]').trigger('click');
    await wrapper.get('.session-menu__logout').trigger('click');
    await flushPromises();

    expect(wrapper.get('[role="alert"]').text()).toContain('Service indisponible');
    expect(auth.clearAuthenticatedSession).not.toHaveBeenCalled();
    expect(runStore.clearForLogout).not.toHaveBeenCalled();
    expect(wrapper.get('.session-menu__logout').attributes('disabled')).toBeUndefined();
  });

  it('uses the fallback message for non-Error logout failures', async () => {
    api.logout.mockRejectedValueOnce('network failure');
    const wrapper = mount(SessionMenu);
    await wrapper.get('[aria-label="Ouvrir le menu de session"]').trigger('click');
    await wrapper.get('.session-menu__logout').trigger('click');
    await flushPromises();

    expect(wrapper.get('[role="alert"]').text()).toContain('La déconnexion a échoué');
  });
});
