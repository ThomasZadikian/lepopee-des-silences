// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { flushPromises, mount } from '@vue/test-utils';

import CharacterSelectionPage from './CharacterSelectionPage.vue';

const router = { push: vi.fn() };

vi.mock('vue-router', () => ({ useRouter: () => router }));

describe('CharacterSelectionPage', () => {
  beforeEach(() => vi.clearAllMocks());

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
    expect(router.push).not.toHaveBeenCalled();
  });

  it('continues with the selected available archetype', async () => {
    const wrapper = mount(CharacterSelectionPage, {
      global: { stubs: { LivingWalls: true } },
    });

    await wrapper.find('input').setValue('Aster');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(router.push).toHaveBeenCalledWith({ name: 'threshold' });
  });
});
