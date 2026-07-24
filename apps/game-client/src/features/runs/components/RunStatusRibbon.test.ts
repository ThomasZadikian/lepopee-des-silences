// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import RunStatusRibbon from './RunStatusRibbon.vue';
import type { RunDto } from '../types/runTypes';

function mountRibbon(run: RunDto, isSafePoint = true) {
  return mount(RunStatusRibbon, {
    props: { run, isSafePoint },
    global: {
      stubs: {
        RoomClimateBadge: {
          template: '<button class="rcb-stub" @click="$emit(\'open\')" />',
          props: ['climate'],
          emits: ['open'],
        },
      },
    },
  });
}

// The ribbon is collapsed to a small tab by default — most content assertions need
// it expanded first.
async function mountExpandedRibbon(run: RunDto, isSafePoint = true) {
  const wrapper = mountRibbon(run, isSafePoint);
  await wrapper.find('.status-ribbon-tab').trigger('click');
  return wrapper;
}

describe('RunStatusRibbon', () => {
  const baseRun: RunDto = {
    id: 'run-1',
    seed: 'abc',
    status: 'Active',
    currentRoomIndex: 0,
    currentRoomNumber: 1,
    currentRoom: {
      id: 'room-1',
      roomType: 'Combat',
      currentNodeDepth: 0,
      maxNodeDepth: 3,
      nodes: [],
      availableNodes: [],
      bossPreview: { name: 'Boss', dangerHint: 'High' },
    },
  };

  it('renders without crashing', () => {
    const wrapper = mountRibbon(baseRun);
    expect(wrapper.exists()).toBe(true);
  });

  it('collapses to a small tab by default', () => {
    const wrapper = mountRibbon(baseRun);
    expect(wrapper.find('.status-ribbon-tab').exists()).toBe(true);
    expect(wrapper.find('.status-ribbon').exists()).toBe(false);
  });

  it('expands and re-collapses the ribbon on toggle click', async () => {
    const wrapper = mountRibbon(baseRun);

    await wrapper.find('.status-ribbon-tab').trigger('click');
    expect(wrapper.find('.status-ribbon').exists()).toBe(true);
    expect(wrapper.text()).toContain('Salle 1');

    const collapseBtn = wrapper.findAll('button').find((b) => b.classes().includes('status-ribbon__collapse'));
    await collapseBtn!.trigger('click');
    expect(wrapper.find('.status-ribbon-tab').exists()).toBe(true);
  });

  it('displays the room number chip', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    expect(wrapper.text()).toContain('Salle 1');
  });

  it('displays the room type chip', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    expect(wrapper.text()).toContain('Combat');
  });

  it('shows laws button when laws exist', async () => {
    const run = { ...baseRun, activePalaceLaws: [{ key: 'law-1', displayName: 'Law 1', description: '', rarity: 'Commun', polarity: 'Neutre', domains: ['Combat'], version: 'v1' }] };
    const wrapper = await mountExpandedRibbon(run);
    expect(wrapper.text()).toContain('1 loi');
  });

  it('shows plural for multiple laws', async () => {
    const run = {
      ...baseRun,
      activePalaceLaws: [
        { key: 'law-1', displayName: 'Law 1', description: '', rarity: 'Commun', polarity: 'Neutre', domains: ['Combat'], version: 'v1' },
        { key: 'law-2', displayName: 'Law 2', description: '', rarity: 'Commun', polarity: 'Neutre', domains: ['Combat'], version: 'v1' },
      ],
    };
    const wrapper = await mountExpandedRibbon(run);
    expect(wrapper.text()).toContain('2 lois');
  });

  it('shows curses button when curses exist', async () => {
    const run = { ...baseRun, activeCurses: [{ id: 'c1', curseDefinitionKey: 'curse-1', displayName: 'Curse 1' }] };
    const wrapper = await mountExpandedRibbon(run);
    expect(wrapper.text()).toContain('1 malédiction');
  });

  it('shows modifiers button when modifiers exist', async () => {
    const run = { ...baseRun, activeModifiers: [{ id: 'm1', type: 'AddStartingGuard', value: 5, duration: 'UntilRunEnds', sourceType: 'Item' }] };
    const wrapper = await mountExpandedRibbon(run);
    expect(wrapper.text()).toContain('1 mod.');
  });

  it('shows quit room button when not at safe point', async () => {
    const wrapper = await mountExpandedRibbon(baseRun, false);
    expect(wrapper.text()).toContain('Quitter la salle');
  });

  it('hides quit room button at safe point', async () => {
    const wrapper = await mountExpandedRibbon(baseRun, true);
    expect(wrapper.text()).not.toContain('Quitter la salle');
  });

  it('emits exitMidRoom when quit room button is clicked', async () => {
    const wrapper = await mountExpandedRibbon(baseRun, false);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Quitter la salle'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('exitMidRoom')).toHaveLength(1);
  });

  it('emits saveAndExit when save button is clicked', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('saveAndExit')).toHaveLength(1);
  });

  it('emits abandon when abandon button is clicked', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Abandonner'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('abandon')).toHaveLength(1);
  });

  it('emits openParty when party button is clicked', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Équipe'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('openParty')).toHaveLength(1);
  });

  it('emits openBesace when besace button is clicked', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('La Besace'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('openBesace')).toHaveLength(1);
  });

  it('emits openInfluences when laws button is clicked', async () => {
    const run = { ...baseRun, activePalaceLaws: [{ key: 'law-1', displayName: 'Law 1', description: '', rarity: 'Commun', polarity: 'Neutre', domains: ['Combat'], version: 'v1' }] };
    const wrapper = await mountExpandedRibbon(run);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('loi'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('openInfluences')).toHaveLength(1);
  });

  it('disables save button when not at safe point', async () => {
    const wrapper = await mountExpandedRibbon(baseRun, false);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Sauvegarder'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables abandon button when not at safe point', async () => {
    const wrapper = await mountExpandedRibbon(baseRun, false);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Abandonner'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('falls back to dash when room type is missing', async () => {
    const run = { ...baseRun, currentRoom: { ...baseRun.currentRoom, roomType: undefined as any } };
    const wrapper = await mountExpandedRibbon(run);
    expect(wrapper.text()).toContain('—');
  });

  it('hides laws button when no laws', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    expect(wrapper.text()).not.toContain('loi');
  });

  it('hides curses button when no curses', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    expect(wrapper.text()).not.toContain('malédiction');
  });

  it('hides modifiers button when no modifiers', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    expect(wrapper.text()).not.toContain('mod.');
  });

  it('disables the journal button when journalEnabled is falsy', async () => {
    const wrapper = await mountExpandedRibbon(baseRun);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Carnet de bord'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('enables the journal button when journalEnabled is true', async () => {
    const run = { ...baseRun, journalEnabled: true };
    const wrapper = await mountExpandedRibbon(run);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Carnet de bord'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits openJournal when the journal button is clicked', async () => {
    const run = { ...baseRun, journalEnabled: true };
    const wrapper = await mountExpandedRibbon(run);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Carnet de bord'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('openJournal')).toHaveLength(1);
  });
});
