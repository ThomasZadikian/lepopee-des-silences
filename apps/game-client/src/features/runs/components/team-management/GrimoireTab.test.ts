// @vitest-environment jsdom
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { mount, flushPromises, type VueWrapper } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import GrimoireTab from './GrimoireTab.vue';
import { usePlayerStore } from '../../../party/stores/playerStore';
import { useRunStore } from '../../stores/runStore';
import { skillsApi } from '../../../party/api/skillsApi';
import { playerApi } from '../../../party/api/playerApi';
import { runApi } from '../../api/runApi';
import { demoPlayerId } from '../../stores/runStore';
import type { PlayerCharacterView } from '../../../party/types/playerTypes';
import type { SkillDefinitionView } from '../../../party/types/skillTypes';

vi.mock('../../../party/api/playerApi', () => ({
  playerApi: {
    getProfile: vi.fn(),
    equipSkill: vi.fn(),
    unequipSkill: vi.fn(),
    spendStatPoint: vi.fn(),
  },
}));

vi.mock('../../../party/api/skillsApi', () => ({
  skillsApi: {
    listActive: vi.fn(),
  },
}));

vi.mock('../../api/runApi', () => ({
  runApi: {
    startRun: vi.fn(),
    getRun: vi.fn(),
    chooseNode: vi.fn(),
    resolveCurrentEvent: vi.fn(),
    progressRun: vi.fn(),
    generateNextNodes: vi.fn(),
    enterInterlude: vi.fn(),
    getInterlude: vi.fn(),
    enterNextRoom: vi.fn(),
    saveAndExitRun: vi.fn(),
    resumeRun: vi.fn(),
    exitMidRoom: vi.fn(),
    abandonRun: vi.fn(),
    getPermanentItemCandidates: vi.fn(),
    confirmPermanentItemSelection: vi.fn(),
    removePalaceLaw: vi.fn(),
    useCaliceInfini: vi.fn(),
    syncPartySkills: vi.fn(),
  },
}));

function baseCharacter(): PlayerCharacterView {
  return {
    id: 'char-1',
    definitionKey: 'character.player.self',
    displayName: 'Le Porteur',
    maxEquippedSkills: 4,
    items: [],
    maxEquippedItems: 3,
    characterType: 'Standard',
    skills: [
      { skillKey: 'skill.a', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: true },
      { skillKey: 'skill.b', unlockedAtUtc: '2026-01-01T00:00:00Z', source: 'default', isEquipped: false },
    ],
    stats: {
      maxVitality: 100, attackPower: 12, defense: 6, startingGuard: 0,
      speed: 10, initiative: 10, recovery: 5, focus: 0, mana: 0, charge: 0,
    },
  };
}

function skill(overrides: Partial<SkillDefinitionView> & { key: string; displayName: string }): SkillDefinitionView {
  return {
    description: 'Un sort.', skillType: 'Damage', targetingType: 'SingleEnemy', effectType: 'Damage',
    manaCost: 0, chargeCost: 0, basePower: 10, category: 'Physical',
    basePowerIsPercentOfMaxVitality: false, effects: [], acquisitionHints: [],
    ...overrides,
  };
}

function baseSkills(): SkillDefinitionView[] {
  return [
    skill({ key: 'skill.a', displayName: 'Frappe', description: 'Un coup.' }),
    skill({ key: 'skill.b', displayName: 'Garde', description: 'Se protège.', skillType: 'Guard', effectType: 'Buff' }),
    skill({
      key: 'skill.locked', displayName: 'Secret', description: 'Un sort verrouillé.', manaCost: 3, basePower: 20,
      category: 'Magic', acquisitionHints: ['Offert par Hitomi'],
    }),
  ];
}

// Equipping/unequipping moves a card's section (Disponibles <-> Équipés), so a stale
// wrapper reference from before the click points at a detached node. Re-query by name.
function findCardByName(wrapper: VueWrapper<any>, name: string) {
  const card = wrapper.findAll('.grimoire-card').find((c) => c.find('.grimoire-card__name').text() === name);
  if (!card) throw new Error(`No grimoire card found for "${name}"`);
  return card;
}

function setPlayerProfile(characters: PlayerCharacterView[]) {
  usePlayerStore().profile = {
    id: 'player-1',
    displayName: 'Test',
    characters,
    progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
    permanentItems: [],
  };
}

describe('GrimoireTab', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
    vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: baseSkills() });
  });

  it('renders owned skills in color and locked skills with an acquisition hint', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    expect(cards).toHaveLength(3);
    expect(cards[2].classes()).toContain('grimoire-card--locked');
    expect(cards[2].text()).toContain('Offert par Hitomi');
    expect(cards[0].classes()).not.toContain('grimoire-card--locked');
  });

  it('separates equipped, available, and locked skills into distinct sections', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const equipped = wrapper.find('.grimoire-section--equipped');
    const available = wrapper.find('.grimoire-section--available');
    const locked = wrapper.find('.grimoire-section--locked');

    expect(equipped.text()).toContain('Frappe');
    expect(equipped.text()).not.toContain('Garde');
    expect(available.text()).toContain('Garde');
    expect(available.text()).not.toContain('Frappe');
    expect(locked.text()).toContain('Secret');
  });

  it('shows the equip toggle only for owned skills', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    const cards = wrapper.findAll('.grimoire-card');
    expect(cards[0].find('.grimoire-toggle').exists()).toBe(true);
    expect(cards[2].find('.grimoire-toggle').exists()).toBe(false);
  });

  it('stages equip/unequip without calling the API until Valider is clicked', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    await findCardByName(wrapper, 'Garde').find('.grimoire-toggle').trigger('click'); // equip skill.b

    expect(playerApi.equipSkill).not.toHaveBeenCalled();
    expect(findCardByName(wrapper, 'Garde').find('.grimoire-toggle').text()).toBe('Équipé');
    expect(wrapper.find('.grimoire-section--equipped').text()).toContain('Garde');
  });

  it('Annuler resets the staged selection without calling the API', async () => {
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    await findCardByName(wrapper, 'Garde').find('.grimoire-toggle').trigger('click');
    await wrapper.find('.grimoire-btn--ghost').trigger('click');

    expect(findCardByName(wrapper, 'Garde').find('.grimoire-toggle').text()).toBe('Équiper');
    expect(wrapper.find('.grimoire-section--available').text()).toContain('Garde');
    expect(playerApi.equipSkill).not.toHaveBeenCalled();
  });

  it('Valider les choix replays only the diff and does not sync party skills without an active run', async () => {
    vi.mocked(playerApi.equipSkill).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    });
    setPlayerProfile([baseCharacter()]);
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    await findCardByName(wrapper, 'Garde').find('.grimoire-toggle').trigger('click'); // equip skill.b
    await wrapper.find('.grimoire-btn--primary').trigger('click');
    await flushPromises();

    expect(playerApi.equipSkill).toHaveBeenCalledWith(demoPlayerId, 'char-1', 'skill.b');
    expect(playerApi.unequipSkill).not.toHaveBeenCalled();
    expect(runApi.syncPartySkills).not.toHaveBeenCalled();
  });

  it('Valider les choix also syncs the active run so the change applies mid-run', async () => {
    vi.mocked(playerApi.equipSkill).mockResolvedValue({
      id: 'player-1',
      displayName: 'Test',
      characters: [baseCharacter()],
      progression: { unspentStatPoints: 0, totalStatPointsEarned: 0, palaceShardCount: 0 },
      permanentItems: [],
    });
    vi.mocked(runApi.syncPartySkills).mockResolvedValue({
      run: { id: 'run-1', status: 'Active', currentRoom: {} },
    } as any);
    setPlayerProfile([baseCharacter()]);
    useRunStore().currentRun = { id: 'run-1', status: 'Active' } as any;

    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    await findCardByName(wrapper, 'Garde').find('.grimoire-toggle').trigger('click');
    await wrapper.find('.grimoire-btn--primary').trigger('click');
    await flushPromises();

    expect(runApi.syncPartySkills).toHaveBeenCalledWith('run-1');
  });

  it('disables further equips once the pending loadout is full', async () => {
    const character = baseCharacter();
    character.maxEquippedSkills = 1; // already 1/1 equipped (skill.a)
    setPlayerProfile([character]);
    const wrapper = mount(GrimoireTab, { props: { character } });
    await flushPromises();

    const unequippedToggle = findCardByName(wrapper, 'Garde').find('.grimoire-toggle'); // skill.b
    expect(unequippedToggle.attributes('disabled')).toBeDefined();
  });

  it('filters skills by name across all sections via the search bar', async () => {
    const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
    await flushPromises();

    await wrapper.find('.grimoire-search').setValue('secr');

    expect(wrapper.findAll('.grimoire-card')).toHaveLength(1);
    expect(wrapper.find('.grimoire-section--locked').text()).toContain('Secret');
    expect(wrapper.find('.grimoire-section--equipped').text()).toContain('Aucun sort équipé ne correspond à la recherche.');
    expect(wrapper.find('.grimoire-section--available').text()).toContain('Aucun sort disponible ne correspond à la recherche.');
  });

  describe('locked section: sort + pagination', () => {
    function manyLockedSkills(): SkillDefinitionView[] {
      // skill.a/skill.b are known (per baseCharacter) and land in Équipés/Disponibles;
      // the three z-prefixed ones stay unknown and land in the Verrouillés section under test.
      return [
        skill({ key: 'skill.a', displayName: 'Frappe' }),
        skill({ key: 'skill.b', displayName: 'Garde', effectType: 'Buff' }),
        skill({ key: 'skill.z1', displayName: 'Zephyr', effectType: 'Damage' }),
        skill({ key: 'skill.z2', displayName: 'Aurore', effectType: 'Buff' }),
        skill({ key: 'skill.z3', displayName: 'Brume', effectType: 'Damage' }),
      ];
    }

    it('sorts alphabetically by default within a section', async () => {
      vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: manyLockedSkills() });
      const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
      await flushPromises();

      const names = wrapper.find('.grimoire-section--locked').findAll('.grimoire-card__name').map((n) => n.text());
      expect(names).toEqual(['Aurore', 'Brume', 'Zephyr']);
    });

    it('sorts by effect category, then alphabetically within each category', async () => {
      vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: manyLockedSkills() });
      const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
      await flushPromises();

      await wrapper.find('.grimoire-sort__select').setValue('category');

      // Damage ("Offensif") sorts before Buff ("Soutien"): Brume/Zephyr, then Aurore.
      const names = wrapper.find('.grimoire-section--locked').findAll('.grimoire-card__name').map((n) => n.text());
      expect(names).toEqual(['Brume', 'Zephyr', 'Aurore']);
    });

    it('paginates the locked section at 6 skills per page and resets to page 1 on sort change', async () => {
      const manySkills: SkillDefinitionView[] = Array.from({ length: 8 }, (_, i) =>
        skill({ key: `skill.locked-${i}`, displayName: `Sort ${String(i).padStart(2, '0')}` }),
      );
      vi.mocked(skillsApi.listActive).mockResolvedValue({ skills: manySkills });

      const wrapper = mount(GrimoireTab, { props: { character: baseCharacter() } });
      await flushPromises();

      const locked = wrapper.find('.grimoire-section--locked');
      expect(locked.findAll('.grimoire-card')).toHaveLength(6);
      expect(locked.find('.grimoire-page-indicator').text()).toBe('Page 1 / 2');

      const buttons = locked.findAll('.grimoire-page-btn');
      await buttons[1].trigger('click'); // Suivant
      expect(wrapper.find('.grimoire-section--locked .grimoire-page-indicator').text()).toBe('Page 2 / 2');
      expect(wrapper.find('.grimoire-section--locked').findAll('.grimoire-card')).toHaveLength(2);

      await wrapper.find('.grimoire-sort__select').setValue('category');
      expect(wrapper.find('.grimoire-section--locked .grimoire-page-indicator').text()).toBe('Page 1 / 2');
    });
  });
});
