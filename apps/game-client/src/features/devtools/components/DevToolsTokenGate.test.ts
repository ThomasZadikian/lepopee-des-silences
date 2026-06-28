// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import DevToolsTokenGate from './DevToolsTokenGate.vue';

function mountComponent(
  hasToken = false,
  status: 'available' | 'unavailable' | 'unknown' = 'unknown',
  environment: string | null = null,
  isLoading = false,
) {
  return mount(DevToolsTokenGate, {
    props: { hasToken, status, environment, isLoading },
  });
}

describe('DevToolsTokenGate', () => {
  it('renders without crashing', () => {
    const wrapper = mountComponent();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays DEV ONLY kicker', () => {
    const wrapper = mountComponent();
    expect(wrapper.text()).toContain('DEV ONLY');
  });

  it('shows status as indisponible when unknown', () => {
    const wrapper = mountComponent();
    expect(wrapper.text()).toContain('inconnu');
  });

  it('shows status as disponible when available', () => {
    const wrapper = mountComponent(false, 'available');
    expect(wrapper.text()).toContain('disponible');
  });

  it('shows status as indisponible when unavailable', () => {
    const wrapper = mountComponent(false, 'unavailable');
    expect(wrapper.text()).toContain('indisponible');
  });

  it('shows token as absent when no token', () => {
    const wrapper = mountComponent(false);
    expect(wrapper.text()).toContain('absent');
  });

  it('shows token as defini when token exists', () => {
    const wrapper = mountComponent(true);
    expect(wrapper.text()).toContain('defini');
  });

  it('shows environment when provided', () => {
    const wrapper = mountComponent(false, 'unknown', 'local');
    expect(wrapper.text()).toContain('local');
  });

  it('hides environment when not provided', () => {
    const wrapper = mountComponent();
    expect(wrapper.text()).not.toContain('Environment');
  });

  it('emits saveToken with token value', async () => {
    const wrapper = mountComponent();
    const input = wrapper.find('input[type="password"]');
    await input.setValue('my-token');
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Enregistrer'));
    if (saveBtn) await saveBtn.trigger('click');
    expect(wrapper.emitted('saveToken')).toBeDefined();
    expect(wrapper.emitted('saveToken')![0][0]).toBe('my-token');
  });

  it('disables save button when input is empty', () => {
    const wrapper = mountComponent();
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Enregistrer'));
    expect((saveBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('enables save button when input has value', async () => {
    const wrapper = mountComponent();
    const input = wrapper.find('input[type="password"]');
    await input.setValue('my-token');
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Enregistrer'));
    expect((saveBtn!.element as HTMLButtonElement).disabled).toBe(false);
  });

  it('emits checkStatus when tester button is clicked', async () => {
    const wrapper = mountComponent(true);
    const testBtn = wrapper.findAll('button').find((b) => b.text().includes('Tester'));
    if (testBtn) await testBtn.trigger('click');
    expect(wrapper.emitted('checkStatus')).toHaveLength(1);
  });

  it('disables tester button when no token', () => {
    const wrapper = mountComponent(false);
    const testBtn = wrapper.findAll('button').find((b) => b.text().includes('Tester'));
    expect((testBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('emits clearToken when clear button is clicked', async () => {
    const wrapper = mountComponent(true);
    const clearBtn = wrapper.findAll('button').find((b) => b.text().includes('Effacer'));
    if (clearBtn) await clearBtn.trigger('click');
    expect(wrapper.emitted('clearToken')).toHaveLength(1);
  });

  it('disables clear button when no token', () => {
    const wrapper = mountComponent(false);
    const clearBtn = wrapper.findAll('button').find((b) => b.text().includes('Effacer'));
    expect((clearBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('disables tester button when loading', () => {
    const wrapper = mountComponent(true, 'unknown', null, true);
    const testBtn = wrapper.findAll('button').find((b) => b.text().includes('Tester'));
    expect((testBtn!.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('clears input after save', async () => {
    const wrapper = mountComponent();
    const input = wrapper.find('input[type="password"]');
    await input.setValue('my-token');
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Enregistrer'));
    if (saveBtn) await saveBtn.trigger('click');
    expect((input.element as HTMLInputElement).value).toBe('');
  });
});
