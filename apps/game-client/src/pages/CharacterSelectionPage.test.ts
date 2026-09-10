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
    sessionStorage.clear();
    auth.getAccessToken.mockReturnValue('access-token');
    api.createCharacter.mockResolvedValue({
      characters: [{
        id: 'character-id',
        displayName: 'Aster',
        characterType: 'Player',
        archetypeKey: 'archetype.porteur',
      }],
    });
    api.getAccount.mockResolvedValue({ role: 'Player', characters: [] });
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

  it('lists every existing character and lets the user select one before playing', async () => {
    api.getAccount.mockResolvedValueOnce({
      characters: [
        { id: 'character-aster', displayName: 'Aster', archetypeKey: 'archetype.porteur' },
        { id: 'character-nox', displayName: 'Nox', archetypeKey: 'archetype.porteur' },
      ],
    });

    const wrapper = await mountReadyPage();

    expect(api.getAccount).toHaveBeenCalledWith('access-token');
    expect(api.createCharacter).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain('Aster');
    expect(wrapper.text()).toContain('Nox');
    expect(wrapper.text()).toContain('Créer un nouveau personnage');
    expect(wrapper.find('form').exists()).toBe(false);

    await wrapper.get('[data-character-id="character-nox"]').trigger('click');

    expect(sessionStorage.getItem('leds.selected-character-id')).toBe('character-nox');
    expect(router.push).toHaveBeenCalledWith({ name: 'threshold' });
  });

  it.each(['Developer', 'Administrator'])('sends a %s account to the developer island', async (role) => {
    api.getAccount.mockResolvedValueOnce({
      role,
      characters: [
        { id: 'character-dev', displayName: 'Aster', archetypeKey: 'archetype.porteur' },
      ],
    });
    const wrapper = await mountReadyPage();

    await wrapper.get('[data-character-id="character-dev"]').trigger('click');

    expect(router.push).toHaveBeenCalledWith({ name: 'developer-island' });
  });

  it('allows another character to be created without selecting an existing companion', async () => {
    api.getAccount.mockResolvedValueOnce({
      characters: [
        { id: 'character-aster', displayName: 'Aster', archetypeKey: 'archetype.porteur' },
      ],
    });
    api.createCharacter.mockResolvedValueOnce({
      characters: [
        { id: 'companion-mane', displayName: 'Mané', characterType: 'Companion' },
        { id: 'character-aster', displayName: 'Aster', characterType: 'Player' },
        { id: 'character-nox', displayName: 'Nox', characterType: 'Player' },
      ],
    });
    const wrapper = await mountReadyPage();

    await wrapper.get('.character-list__create').trigger('click');
    await wrapper.get('input').setValue('Nox');
    await wrapper.get('form').trigger('submit');
    await flushPromises();

    expect(sessionStorage.getItem('leds.selected-character-id')).toBe('character-nox');
    expect(router.push).toHaveBeenCalledWith({ name: 'threshold' });
  });

  it('can cancel character creation and return to the existing list', async () => {
    api.getAccount.mockResolvedValueOnce({
      characters: [{ id: 'character-aster', displayName: 'Aster', archetypeKey: 'archetype.inconnu' }],
    });
    const wrapper = await mountReadyPage();

    expect(wrapper.text()).toContain('archetype.inconnu');
    await wrapper.get('.character-list__create').trigger('click');
    expect(wrapper.find('form').exists()).toBe(true);
    await wrapper.get('.character-selection__cancel').trigger('click');

    expect(wrapper.find('form').exists()).toBe(false);
    expect(wrapper.text()).toContain('Aster');
  });

  it('keeps the user on creation when the returned profile contains no new playable character', async () => {
    api.createCharacter.mockResolvedValueOnce({ characters: [] });
    const wrapper = await mountReadyPage();

    await wrapper.get('input').setValue('Nox');
    await wrapper.get('form').trigger('submit');
    await flushPromises();

    expect(wrapper.text()).toContain('n’a pas pu être sélectionné');
    expect(router.push).not.toHaveBeenCalled();
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
