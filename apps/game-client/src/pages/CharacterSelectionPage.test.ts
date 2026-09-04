// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import CharacterSelectionPage from './CharacterSelectionPage.vue';

const router = { push: vi.fn(), replace: vi.fn() };
const api = vi.hoisted(() => ({ createCharacter: vi.fn(), getAccount: vi.fn() }));
const auth = vi.hoisted(() => ({ getAccessToken: vi.fn(() => 'access-token') }));

vi.mock('vue-router', () => ({ useRouter: () => router }));
vi.mock('../shared/api/playerApi', () => ({ playerApi: api }));
vi.mock('../features/account/authSession', () => auth);

async function mountReadyPage() {
  const wrapper = mount(CharacterSelectionPage, {
    global: { stubs: { LivingWalls: true } },
  });
  await flushPromises();
  return wrapper;
}

describe('CharacterSelectionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.getAccessToken.mockReturnValue('access-token');
    api.createCharacter.mockResolvedValue({ id: 'character-id' });
    api.getAccount.mockResolvedValue({ characters: [] });
  });

  it('shows the current archetype and future locked slots', async () => {
    const wrapper = await mountReadyPage();

    expect(wrapper.text()).toContain('Porteur');
    expect(wrapper.findAll('.archetype-card--locked')).toHaveLength(2);
    expect(wrapper.text()).toContain('archétype immuable');
  });

  it('requires a character name before entering the Palace', async () => {
    const wrapper = await mountReadyPage();

    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('Donnez un nom');
    expect(api.createCharacter).not.toHaveBeenCalled();
    expect(router.push).not.toHaveBeenCalled();
  });

  it('creates the character with the authenticated account before continuing', async () => {
    const wrapper = await mountReadyPage();

    await wrapper.find('input').setValue('Aster');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.createCharacter).toHaveBeenCalledWith(
      'access-token',
      { displayName: 'Aster', archetypeKey: 'archetype.porteur' },
    );
    expect(router.push).toHaveBeenCalledWith({ name: 'threshold' });
  });

  it('continues with the existing character instead of proposing another creation', async () => {
    api.getAccount.mockResolvedValueOnce({
      characters: [{ id: 'existing-character', displayName: 'Aster' }],
    });

    const wrapper = await mountReadyPage();

    expect(api.getAccount).toHaveBeenCalledWith('access-token');
    expect(api.createCharacter).not.toHaveBeenCalled();
    expect(router.replace).toHaveBeenCalledWith({ name: 'threshold' });
    expect(wrapper.find('form').exists()).toBe(false);
  });

  it('requires an authenticated session and surfaces server failures', async () => {
    auth.getAccessToken.mockReturnValueOnce(null);
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });
    await flushPromises();
    expect(wrapper.text()).toContain('session a expiré');
    expect(router.replace).toHaveBeenCalledWith({ name: 'login' });
    wrapper.unmount();

    auth.getAccessToken.mockReturnValue('access-token');
    api.createCharacter.mockRejectedValueOnce(new Error('Archétype indisponible'));
    const authenticatedWrapper = await mountReadyPage();
    await authenticatedWrapper.find('input').setValue('Aster');
    await authenticatedWrapper.find('form').trigger('submit');
    await flushPromises();
    expect(authenticatedWrapper.text()).toContain('Archétype indisponible');
    expect(router.push).not.toHaveBeenCalled();
  });
});
