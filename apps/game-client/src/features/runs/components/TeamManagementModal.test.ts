// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import TeamManagementModal from './TeamManagementModal.vue';
import { usePlayerStore } from '../../party/stores/playerStore';
import { skillsApi } from '../../party/api/skillsApi';
import type { PlayerProfileView } from '../../party/types/playerTypes';

vi.mock('../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
    equipItem: vi.fn(),
    unequipItem: vi.fn(),
  },
}));

vi.mock('../../party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn(),
  },
}));

function baseProfile(): PlayerProfileView {
  return {
    id: 'player-1',
    displayName: 'Test Player',
    characters: [
      {
        id: 'char-1',
        definitionKey: 'character.player.self',
        displayName: 'Le Porteur',
        maxEquippedSkills: 4,
        items: [],
        maxEquippedItems: 3,
        skills: [
          { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
        ],
        stats: {
          maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
          speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
        },
      },
    ],
    progression: { unspentStatPoints: 2, totalStatPointsEarned: 3 },
    permanentItems: [],
  };
}

function mountModal() {
  return mount(TeamManagementModal, {
    global: { stubs: { Transition: { template: '<slot />' } } },
  });
}

describe('TeamManagementModal', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: [] });
    usePlayerStore().profile = baseProfile();
  });

  it('renders the four tabs', async () => {
    const wrapper = mountModal();
    await flushPromises();
    const tabs = wrapper.findAll('.tmm-tab');
    expect(tabs.map((t) => t.text())).toEqual(['Équipe', 'Statistiques', 'Compétences', 'Équipement']);
  });

  it('shows the overview tab by default', async () => {
    const wrapper = mountModal();
    await flushPromises();
    expect(wrapper.text()).toContain('Le Porteur');
  });

  it('switches to the stats tab on click', async () => {
    const wrapper = mountModal();
    await flushPromises();
    const tabs = wrapper.findAll('.tmm-tab');
    await tabs[1].trigger('click');
    expect(wrapper.text()).toContain('Points disponibles');
  });

  it('switches to the skills tab on click', async () => {
    const wrapper = mountModal();
    await flushPromises();
    const tabs = wrapper.findAll('.tmm-tab');
    await tabs[2].trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Sorts équipés');
  });

  it('switches to the items tab on click', async () => {
    const wrapper = mountModal();
    await flushPromises();
    const tabs = wrapper.findAll('.tmm-tab');
    await tabs[3].trigger('click');
    await flushPromises();
    expect(wrapper.text()).toContain('Objets équipés');
  });

  it('does not show a character picker with only one character', async () => {
    const wrapper = mountModal();
    await flushPromises();
    expect(wrapper.find('.tmm-character-picker').exists()).toBe(false);
  });

  it('emits close when the backdrop is clicked', async () => {
    const wrapper = mountModal();
    await flushPromises();
    await wrapper.find('.tmm-backdrop').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountModal();
    await flushPromises();
    await wrapper.find('.tmm-close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });
});
