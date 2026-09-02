// @vitest-environment jsdom

import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ManifestationsPage from './ManifestationsPage.vue';
import { enemyCodexApi } from '../features/emotional-registers/enemyCodexApi';

const back = vi.fn();
const definitionOf = vi.fn();

vi.mock('vue-router', () => ({
  useRouter: () => ({ back }),
}));

vi.mock('../features/emotional-registers/enemyCodexApi', () => ({
  enemyCodexApi: {
    listBosses: vi.fn(),
  },
}));

vi.mock('../features/emotional-registers/store', () => ({
  useEmotionalRegisterCatalog: () => ({ definitionOf }),
}));

const affinitySet = [
  { incomingRegister: 'Effroi', outcome: 'Weak' },
  { incomingRegister: 'Deni', outcome: 'Resistant' },
  { incomingRegister: 'Memoire', outcome: 'Immune' },
  { incomingRegister: 'Rupture', outcome: 'Neutral' },
];

function boss(key: string, name: string, threat: number, emotionalRegister = 'Silence') {
  return {
    key,
    displayName: name,
    description: `${name} description`,
    threat,
    emotionalRegister,
    compatibleRoomTypes: ['Boss', 'FinalBoss'],
  } as any;
}

function mountPage(embedded = false) {
  return mount(ManifestationsPage, {
    props: { embedded },
    global: {
      stubs: {
        LivingWalls: { template: '<div data-test="living-walls" />' },
        EmotionalTypeBadge: {
          props: ['type'],
          template: '<span class="type-badge">{{ type }}</span>',
        },
      },
    },
  });
}

describe('ManifestationsPage coverage margin', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    definitionOf.mockImplementation((register?: string) => register
      ? { incomingAffinities: affinitySet }
      : undefined);
  });

  it('renders lore, affinities and every threat band while selecting bosses', async () => {
    vi.mocked(enemyCodexApi.listBosses).mockResolvedValue({
      bosses: [
        boss('canon.enemy.himlit', "Him'Lit", 5),
        boss('boss.four', 'Quatre', 4),
        boss('boss.three', 'Trois', 3),
        boss('boss.two', 'Deux', 2),
      ],
    } as any);

    const wrapper = mountPage(false);
    await flushPromises();

    expect(wrapper.find('[data-test="living-walls"]').exists()).toBe(true);
    expect(wrapper.text()).toContain("Him'Lit");
    expect(wrapper.text()).toContain('Brume');
    expect(wrapper.text()).toContain('Lettre après lettre');
    expect(wrapper.text()).toContain('faible à');
    expect(wrapper.text()).toContain('résiste');
    expect(wrapper.text()).toContain('immunisé à');
    expect(wrapper.find('.manif-detail__threat').attributes('style')).toContain('danger-dim');

    const buttons = wrapper.findAll('.manif-list__item');
    await buttons[1]!.trigger('click');
    expect(wrapper.find('.manif-detail__threat').attributes('style')).toContain('mauve');
    expect(wrapper.find('.manif-detail__quote').exists()).toBe(false);
    expect(wrapper.findAll('.manif-detail__mech-line')).toHaveLength(0);

    await buttons[2]!.trigger('click');
    expect(wrapper.find('.manif-detail__threat').attributes('style')).toContain('mauve-dim');

    await buttons[3]!.trigger('click');
    expect(wrapper.find('.manif-detail__threat').attributes('style')).toContain('ink-3');

    await wrapper.find('.manif-page__back').trigger('click');
    expect(back).toHaveBeenCalledOnce();
  });

  it('hides shell-only UI in embedded mode and handles missing register metadata', async () => {
    definitionOf.mockReturnValue(undefined);
    vi.mocked(enemyCodexApi.listBosses).mockResolvedValue({
      bosses: [boss('unknown', 'Inconnu', 1, 'Unknown')],
    } as any);

    const wrapper = mountPage(true);
    await flushPromises();

    expect(wrapper.classes()).toContain('manif-page--embedded');
    expect(wrapper.find('[data-test="living-walls"]').exists()).toBe(false);
    expect(wrapper.find('.manif-page__back').exists()).toBe(false);
    expect(wrapper.findAll('.manif-detail__affinity .type-badge')).toHaveLength(0);
  });

  it('renders an Error message from a failed codex request', async () => {
    vi.mocked(enemyCodexApi.listBosses).mockRejectedValue(new Error('Codex hors ligne'));

    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).toContain('Codex hors ligne');
    expect(wrapper.find('.manif-layout').exists()).toBe(false);
  });

  it('renders the generic message for non-Error failures', async () => {
    vi.mocked(enemyCodexApi.listBosses).mockRejectedValue('offline');

    const wrapper = mountPage();
    await flushPromises();

    expect(wrapper.text()).toContain('Bestiaire indisponible.');
  });
});
