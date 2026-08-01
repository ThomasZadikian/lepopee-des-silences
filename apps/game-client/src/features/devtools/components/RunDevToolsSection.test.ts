// @vitest-environment jsdom
import { describe, expect, it, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import RunDevToolsSection from './RunDevToolsSection.vue';

function mountSection(disabled = false, isLoading = false) {
  return mount(RunDevToolsSection, {
    props: { disabled, isLoading },
  });
}

describe('RunDevToolsSection', () => {
  it('renders without crashing', () => {
    const wrapper = mountSection();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays Run section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Run');
  });

  it('displays Party section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Party');
  });

  it('displays Room section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Room');
  });

  it('displays Laws section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Laws');
  });

  it('displays Curses section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Curses');
  });

  it('displays Items section', () => {
    const wrapper = mountSection();
    expect(wrapper.text()).toContain('Items');
  });

  it('emits advanceRoom when button is clicked', async () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Advance room'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('advanceRoom')).toHaveLength(1);
  });

  it('emits advanceRooms with clamped count', async () => {
    const wrapper = mountSection();
    const input = wrapper.find('input[type="number"]');
    await input.setValue(5);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Advance N rooms'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('advanceRooms')).toBeDefined();
    expect(wrapper.emitted('advanceRooms')![0][0]).toBe(5);
  });

  it('clamps room count to max 10', async () => {
    const wrapper = mountSection();
    const input = wrapper.find('input[type="number"]');
    await input.setValue(20);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Advance N rooms'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('advanceRooms')![0][0]).toBe(10);
  });

  it('clamps room count to min 1', async () => {
    const wrapper = mountSection();
    const input = wrapper.find('input[type="number"]');
    await input.setValue(0);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Advance N rooms'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('advanceRooms')![0][0]).toBe(1);
  });

  it('emits forcePalaceState with selected state', async () => {
    const wrapper = mountSection();
    const selects = wrapper.findAll('select');
    await selects[1].setValue('Enraged');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Force state'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('forcePalaceState')).toBeDefined();
    expect(wrapper.emitted('forcePalaceState')![0][0]).toBe('Enraged');
  });

  it('emits forceClimate with selected climate', async () => {
    const wrapper = mountSection();
    const selects = wrapper.findAll('select');
    await selects[2].setValue('Rain');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Force climate'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('forceClimate')).toBeDefined();
    expect(wrapper.emitted('forceClimate')![0][0]).toBe('Rain');
  });

  it('emits activateLaw with trimmed law key', async () => {
    const wrapper = mountSection();
    const inputs = wrapper.findAll('input:not([type="number"])');
    await inputs[0].setValue('law-aegis-v1');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activate law'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('activateLaw')).toBeDefined();
    expect(wrapper.emitted('activateLaw')![0][0]).toBe('law-aegis-v1');
  });

  it('disables activateLaw when input is empty', () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activate law'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits activateCurse with trimmed curse key', async () => {
    const wrapper = mountSection();
    const inputs = wrapper.findAll('input:not([type="number"])');
    await inputs[1].setValue('curse.old-wound');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activate curse'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('activateCurse')).toBeDefined();
    expect(wrapper.emitted('activateCurse')![0][0]).toBe('curse.old-wound');
  });

  it('disables activateCurse when input is empty', () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Activate curse'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits addAlly with the selected companion key when button is clicked', async () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Recruter le compagnon'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('addAlly')).toHaveLength(1);
    expect(wrapper.emitted('addAlly')![0][0]).toBe('npc.thomas');
  });

  it('emits addAlly with a different companion key once selected', async () => {
    const wrapper = mountSection();
    const select = wrapper.findAll('select')[0];
    await select.setValue('npc.john');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Recruter le compagnon'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('addAlly')![0][0]).toBe('npc.john');
  });

  it('emits removeAlly when button is clicked', async () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Retirer le dernier compagnon'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('removeAlly')).toHaveLength(1);
  });

  it('shows all 5 recruitable companions in the selector', () => {
    const wrapper = mountSection();
    const select = wrapper.findAll('select')[0];
    const options = select.findAll('option');
    expect(options.length).toBe(5);
    expect(options.map((o) => o.text())).toEqual(['Thomas', 'Mané', 'Mina', 'Elise', 'John']);
  });

  it('emits clearLaws on confirm', async () => {
    const wrapper = mountSection();
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Clear laws'));
    if (btn) await btn.trigger('click');

    expect(wrapper.emitted('clearLaws')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });

  it('emits clearCurses on confirm', async () => {
    const wrapper = mountSection();
    const originalConfirm = globalThis.window.confirm;
    globalThis.window.confirm = vi.fn(() => true);

    const btn = wrapper.findAll('button').find((b) => b.text().includes('Clear curses'));
    if (btn) await btn.trigger('click');

    expect(wrapper.emitted('clearCurses')).toHaveLength(1);
    globalThis.window.confirm = originalConfirm;
  });

  it('disables all buttons when disabled prop is true', () => {
    const wrapper = mountSection(true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBe(buttons.length);
  });

  it('disables all buttons when isLoading is true', () => {
    const wrapper = mountSection(false, true);
    const buttons = wrapper.findAll('button');
    const disabledButtons = buttons.filter((b) => (b.element as HTMLButtonElement).disabled);
    expect(disabledButtons.length).toBe(buttons.length);
  });

  it('shows all palace states in selector', () => {
    const wrapper = mountSection();
    const select = wrapper.findAll('select')[1];
    const options = select.findAll('option');
    expect(options.length).toBe(5);
  });

  it('shows all climates in selector', () => {
    const wrapper = mountSection();
    const select = wrapper.findAll('select')[2];
    const options = select.findAll('option');
    expect(options.length).toBe(5);
  });

  it('emits addItem with trimmed key and default quantity', async () => {
    const wrapper = mountSection();
    const inputs = wrapper.findAll('input:not([type="number"])');
    await inputs[2]!.setValue('item.the-seuil');
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Ajouter a la besace'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('addItem')).toBeDefined();
    expect(wrapper.emitted('addItem')![0]).toEqual(['item.the-seuil', 1]);
  });

  it('emits addItem with the chosen quantity, clamped to [1,99]', async () => {
    const wrapper = mountSection();
    const inputs = wrapper.findAll('input:not([type="number"])');
    await inputs[2]!.setValue('item.the-seuil');
    const numberInputs = wrapper.findAll('input[type="number"]');
    await numberInputs[numberInputs.length - 1]!.setValue(150);
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Ajouter a la besace'));
    if (btn) await btn.trigger('click');
    expect(wrapper.emitted('addItem')![0]).toEqual(['item.the-seuil', 99]);
  });

  it('disables addItem when the item key is empty', () => {
    const wrapper = mountSection();
    const btn = wrapper.findAll('button').find((b) => b.text().includes('Ajouter a la besace'));
    expect((btn!.element as HTMLButtonElement).disabled).toBe(true);
  });
});
