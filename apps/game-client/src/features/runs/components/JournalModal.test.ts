// @vitest-environment jsdom
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import JournalModal from './JournalModal.vue';
import type { RunJournalEntryDto } from '../types/runTypes';

const mockStore: { currentRun: { journalEntries?: RunJournalEntryDto[] | null } | null } = {
  currentRun: null,
};

vi.mock('../stores/runStore', () => ({
  useRunStore: vi.fn(() => mockStore),
}));

function mountModal(journalEntries: RunJournalEntryDto[] | null = null) {
  mockStore.currentRun = { journalEntries };
  return mount(JournalModal);
}

describe('JournalModal', () => {
  beforeEach(() => {
    mockStore.currentRun = null;
  });

  it('renders without crashing', () => {
    const wrapper = mountModal([]);
    expect(wrapper.exists()).toBe(true);
  });

  it('shows the empty-state quote when there are no entries', () => {
    const wrapper = mountModal([]);
    expect(wrapper.text()).toContain('Les pages sont encore vierges');
  });

  it('shows a room section header with the room number for a single-room journal', () => {
    const wrapper = mountModal([
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: "J'ai trouvé un objet." },
    ]);
    expect(wrapper.text()).toContain('Salle 1');
    expect(wrapper.text()).not.toContain('Les pages sont encore vierges');
  });

  it('includes the room display name in the section header when present', () => {
    const wrapper = mountModal([
      { roomIndex: 2, roomNumber: 3, roomDisplayName: 'La Forêt', text: "J'ai combattu un ennemi." },
    ]);
    expect(wrapper.text()).toContain('Salle 3');
    expect(wrapper.text()).toContain('La Forêt');
  });

  it('renders every entry text for the current page', () => {
    const wrapper = mountModal([
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: 'Première ligne du carnet.' },
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: 'Deuxième ligne du carnet.' },
    ]);
    expect(wrapper.text()).toContain('Première ligne du carnet.');
    expect(wrapper.text()).toContain('Deuxième ligne du carnet.');
  });

  it('groups entries into one page per room and defaults to the latest room', () => {
    const wrapper = mountModal([
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: 'Salle 1, événement.' },
      { roomIndex: 1, roomNumber: 2, roomDisplayName: null, text: 'Salle 2, événement.' },
    ]);
    expect(wrapper.text()).toContain('Salle 2, événement.');
    expect(wrapper.text()).not.toContain('Salle 1, événement.');
    expect(wrapper.text()).toContain('Page 2 / 2');
  });

  it('navigates to the previous page when the pager button is clicked', async () => {
    const wrapper = mountModal([
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: 'Salle 1, événement.' },
      { roomIndex: 1, roomNumber: 2, roomDisplayName: null, text: 'Salle 2, événement.' },
    ]);

    const prevBtn = wrapper.findAll('button').find((b) => b.text().includes('Salle précédente'));
    await prevBtn!.trigger('click');

    expect(wrapper.text()).toContain('Salle 1, événement.');
    expect(wrapper.text()).not.toContain('Salle 2, événement.');
  });

  it('does not show pagination controls for a single-room journal', () => {
    const wrapper = mountModal([
      { roomIndex: 0, roomNumber: 1, roomDisplayName: null, text: 'Seule salle.' },
    ]);
    expect(wrapper.text()).not.toContain('Salle précédente');
    expect(wrapper.text()).not.toContain('Salle suivante');
  });

  it('emits close when the close button is clicked', async () => {
    const wrapper = mountModal([]);
    await wrapper.find('.jm-close').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('emits close when the backdrop is clicked', async () => {
    const wrapper = mountModal([]);
    await wrapper.find('.jm-backdrop').trigger('click');
    expect(wrapper.emitted('close')).toHaveLength(1);
  });

  it('does not emit close when the panel itself is clicked', async () => {
    const wrapper = mountModal([]);
    await wrapper.find('.jm-panel').trigger('click');
    expect(wrapper.emitted('close')).toBeUndefined();
  });
});
