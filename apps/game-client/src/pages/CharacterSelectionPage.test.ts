// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import CharacterSelectionPage from './CharacterSelectionPage.vue';

const router = { push: vi.fn() };
const api = vi.hoisted(() => ({ createCharacter: vi.fn() }));
const auth = vi.hoisted(() => ({ getAccessToken: vi.fn(() => 'access-token') }));

vi.mock('vue-router', () => ({ useRouter: () => router }));
vi.mock('../shared/api/playerApi', () => ({ playerApi: api }));
vi.mock('../features/account/authSession', () => auth);

describe('CharacterSelectionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    auth.getAccessToken.mockReturnValue('access-token');
    api.createCharacter.mockResolvedValue({ id: 'character-id' });
  });

  it('shows the current archetype and future locked slots', () => {
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });

    expect(wrapper.text()).toContain('Porteur');
    expect(wrapper.findAll('.archetype-card--locked')).toHaveLength(2);
    expect(wrapper.text()).toContain('archétype immuable');
  });

  it('requires a character name before entering the Palace', async () => {
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });

    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('Donnez un nom');
    expect(api.createCharacter).not.toHaveBeenCalled();
    expect(router.push).not.toHaveBeenCalled();
  });

  it('creates the character with the authenticated account before continuing', async () => {
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });

    await wrapper.find('input').setValue('Aster');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(api.createCharacter).toHaveBeenCalledWith(
      'access-token',
      { displayName: 'Aster', archetypeKey: 'archetype.porteur' },
    );
    expect(router.push).toHaveBeenCalledWith({ name: 'threshold' });
  });

  it('requires an authenticated session and surfaces server failures', async () => {
    auth.getAccessToken.mockReturnValueOnce(null);
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });
    await wrapper.find('input').setValue('Aster');
    await wrapper.find('form').trigger('submit');
    expect(wrapper.text()).toContain('session a expiré');

    auth.getAccessToken.mockReturnValue('access-token');
    api.createCharacter.mockRejectedValueOnce(new Error('Archétype indisponible'));
    await wrapper.find('form').trigger('submit');
    await flushPromises();
    expect(wrapper.text()).toContain('Archétype indisponible');
    expect(router.push).not.toHaveBeenCalled();
  });
});
