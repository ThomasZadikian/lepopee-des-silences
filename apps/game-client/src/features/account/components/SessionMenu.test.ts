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
});
