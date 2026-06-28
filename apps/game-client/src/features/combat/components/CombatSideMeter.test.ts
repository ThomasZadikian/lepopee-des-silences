// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import CombatSideMeter from './CombatSideMeter.vue';
import type { CombatSideMetrics } from '../composables/useCombatMetrics';

function makeMetrics(overrides: Partial<CombatSideMetrics> = {}): CombatSideMetrics {
  return {
    damageDealt: 50,
    damageTaken: 30,
    healingDone: 20,
    healingReceived: 15,
    guardAbsorbed: 10,
    guardGained: 5,
    netVitalityLoss: 15,
    ...overrides,
  };
}

function mountMeter(title = 'Alliés', side: 'allies' | 'enemies' = 'allies', metrics?: CombatSideMetrics) {
  return mount(CombatSideMeter, { props: { title, side, metrics: metrics ?? makeMetrics() } });
}

describe('CombatSideMeter', () => {
  it('renders without crashing', () => {
    const wrapper = mountMeter();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the title', () => {
    const wrapper = mountMeter('Ennemis', 'enemies');
    expect(wrapper.find('.side-meter__title').text()).toBe('Ennemis');
  });

  it('shows default metric value (damageTaken)', () => {
    const wrapper = mountMeter('Alliés', 'allies', makeMetrics({ damageTaken: 42 }));
    expect(wrapper.find('.side-meter__value').text()).toBe('42');
  });

  it('renders a select element for metric options', () => {
    const wrapper = mountMeter();
    expect(wrapper.find('.side-meter__select').exists()).toBe(true);
  });

  it('has seven metric options', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    expect(options.length).toBe(7);
  });

  it('includes damageDealt option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('damageDealt');
  });

  it('includes damageTaken option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('damageTaken');
  });

  it('includes healingDone option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('healingDone');
  });

  it('includes guardAbsorbed option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('guardAbsorbed');
  });

  it('includes guardGained option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('guardGained');
  });

  it('includes netVitalityLoss option', () => {
    const wrapper = mountMeter();
    const options = wrapper.findAll('option');
    const values = options.map((o) => (o.element as HTMLOptionElement).value);
    expect(values).toContain('netVitalityLoss');
  });

  it('disables guard options when no guard data', () => {
    const metrics = makeMetrics({ guardAbsorbed: 0, guardGained: 0 });
    const wrapper = mountMeter('Alliés', 'allies', metrics);
    const options = wrapper.findAll('option') as any[];
    const guardAbsorbedOpt = options.find((o) => (o.element as HTMLOptionElement).value === 'guardAbsorbed');
    const guardGainedOpt = options.find((o) => (o.element as HTMLOptionElement).value === 'guardGained');
    expect((guardAbsorbedOpt.element as HTMLOptionElement).disabled).toBe(true);
    expect((guardGainedOpt.element as HTMLOptionElement).disabled).toBe(true);
  });

  it('enables guard options when guard data exists', () => {
    const metrics = makeMetrics({ guardAbsorbed: 10, guardGained: 5 });
    const wrapper = mountMeter('Alliés', 'allies', metrics);
    const options = wrapper.findAll('option') as any[];
    const guardAbsorbedOpt = options.find((o) => (o.element as HTMLOptionElement).value === 'guardAbsorbed');
    expect((guardAbsorbedOpt.element as HTMLOptionElement).disabled).toBe(false);
  });

  it('updates displayed value when select changes', async () => {
    const wrapper = mountMeter('Alliés', 'allies', makeMetrics({ healingDone: 99 }));
    const select = wrapper.find('.side-meter__select');
    await select.setValue('healingDone');
    expect(wrapper.find('.side-meter__value').text()).toBe('99');
  });

  it('shows zero when metric value is zero', async () => {
    const wrapper = mountMeter('Alliés', 'allies', makeMetrics({ damageTaken: 0 }));
    const select = wrapper.find('.side-meter__select');
    await select.setValue('damageTaken');
    expect(wrapper.find('.side-meter__value').text()).toBe('0');
  });
});
