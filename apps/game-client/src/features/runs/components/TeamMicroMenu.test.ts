// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';

import TeamMicroMenu from './TeamMicroMenu.vue';

const teamHubStub = {
  name: 'team-hub-page',
  props: ['embedded', 'initialTab'],
  template: `<div class="stub-team-hub" :data-tab="initialTab" />`,
};

function mountMenu() {
  return mount(TeamMicroMenu, {
    attachTo: document.body,
    global: {
      stubs: {
        TeamHubPage: teamHubStub,
      },
    },
  });
}

describe('TeamMicroMenu', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  afterEach(() => {
    document.body.innerHTML = '';
  });

  // The live team-status button (État de l'équipe) only shows while not in combat — with no
  // run loaded at all, gameplayPhase resolves to 'Loading' (not 'Combat'), so it renders by
  // default here, ahead of the five destination buttons.

  it('renders the vitals button plus one per destination', () => {
    const wrapper = mountMenu();
    const buttons = wrapper.findAll('.micro-menu__btn');
    expect(buttons).toHaveLength(6);
  });

  it('gives each button a tooltip label', () => {
    const wrapper = mountMenu();
    const titles = wrapper.findAll('.micro-menu__btn').map((btn) => btn.attributes('title'));
    expect(titles).toEqual([
      "État de l'équipe", 'Équipe', 'Statistiques', 'Grimoire', 'Équipement', 'La Besace',
    ]);
  });

  it('shows no modal by default', () => {
    mountMenu();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });

  it('opens the fused Équipe hub on the Équipe tab', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[1].trigger('click');
    expect(document.querySelector('.stub-team-hub')?.getAttribute('data-tab')).toBe('equipe');
  });

  it('opens the fused Équipe hub on the Statistiques tab', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[2].trigger('click');
    expect(document.querySelector('.stub-team-hub')?.getAttribute('data-tab')).toBe('statistiques');
  });

  it('opens the fused Équipe hub on the Grimoire tab', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[3].trigger('click');
    expect(document.querySelector('.stub-team-hub')?.getAttribute('data-tab')).toBe('grimoire');
  });

  it('opens the fused Équipe hub on the Équipement tab', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[4].trigger('click');
    expect(document.querySelector('.stub-team-hub')?.getAttribute('data-tab')).toBe('equipement');
  });

  it('opens the fused Équipe hub on the Besace tab', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[5].trigger('click');
    expect(document.querySelector('.stub-team-hub')?.getAttribute('data-tab')).toBe('besace');
  });

  it('marks the button for the currently open modal as active', async () => {
    const wrapper = mountMenu();
    const buttons = wrapper.findAll('.micro-menu__btn');
    await buttons[1].trigger('click');
    expect(buttons[1].classes()).toContain('micro-menu__btn--active');
    expect(buttons[2].classes()).not.toContain('micro-menu__btn--active');
  });

  it('closes the modal when PageOverlayModal emits close', async () => {
    const wrapper = mountMenu();
    await wrapper.findAll('.micro-menu__btn')[1].trigger('click');
    expect(document.querySelector('.pom-backdrop')).not.toBeNull();

    const closeBtn = document.querySelector('.pom-close') as HTMLButtonElement;
    closeBtn.click();
    await wrapper.vm.$nextTick();
    expect(document.querySelector('.pom-backdrop')).toBeNull();
  });

  it('hides the DevTools entry when devtools are disabled (the default)', () => {
    const wrapper = mountMenu();
    expect(wrapper.find('.micro-menu__btn--devtools').exists()).toBe(false);
  });

  // ── État de l'équipe: opens the run's live PartyDrawer (rendered by RunPage.vue) via the
  // shared gameUi store, instead of the persistent character sheet the other buttons open. ──

  it('toggles the party drawer flag in the shared UI store', async () => {
    const wrapper = mountMenu();
    const { useGameUiStore } = await import('../../../shared/stores/useGameUiStore');
    const uiStore = useGameUiStore();

    const vitalsButton = wrapper.findAll('.micro-menu__btn')[0];
    expect(uiStore.activeDrawer).toBeNull();

    await vitalsButton.trigger('click');
    expect(uiStore.activeDrawer).toBe('party');
    expect(vitalsButton.classes()).toContain('micro-menu__btn--active');

    await vitalsButton.trigger('click');
    expect(uiStore.activeDrawer).toBeNull();
  });
});

// DevToolsPanel is folded into this same menu (see task: move the floating DevTools button) —
// isDevToolsEnabled() is read once at module scope, so flipping it requires a fresh dynamic
// import per the env stub, same pattern as isDevToolsEnabled.test.ts.
describe('TeamMicroMenu — DevTools entry (enabled)', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.stubEnv('DEV', true);
    vi.stubEnv('VITE_GAME_CLIENT_DEVTOOLS_ENABLED', 'true');
  });

  afterEach(() => {
    document.body.innerHTML = '';
    vi.unstubAllEnvs();
    vi.resetModules();
  });

  it('shows a distinctly styled DevTools button in the menu, off by default', async () => {
    const { default: EnabledTeamMicroMenu } = await import('./TeamMicroMenu.vue');
    const wrapper = mount(EnabledTeamMicroMenu, {
      attachTo: document.body,
      global: {
        stubs: {
          TeamHubPage: teamHubStub,
        },
      },
    });

    const devBtn = wrapper.find('.micro-menu__btn--devtools');
    expect(devBtn.exists()).toBe(true);
    expect(devBtn.attributes('aria-expanded')).toBe('false');

    // No active run in a fresh store, so the panel itself never mounts — this only asserts
    // the toggle's own state, not DevToolsPanel's internals.
    await devBtn.trigger('click');
    expect(devBtn.attributes('aria-expanded')).toBe('true');
  });
});
