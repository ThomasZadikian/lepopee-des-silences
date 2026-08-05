// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import EventChoiceResultPanel from './EventChoiceResultPanel.vue';
import type { CurrentEventChoiceResultDto } from '../types/eventTypes';

function mountPanel(result: CurrentEventChoiceResultDto, isLoading = false) {
  return mount(EventChoiceResultPanel, {
    props: { result, isLoading },
  });
}

describe('EventChoiceResultPanel', () => {
  const baseResult: CurrentEventChoiceResultDto = {
    id: 'result-1',
    title: 'Choix accompli',
    description: 'Ton geste a été retenu par le Palais.',
    outcomeKind: 'Success',
    state: 'Resolved',
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseResult);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the outcome title', () => {
    const wrapper = mountPanel(baseResult);
    expect(wrapper.text()).toContain('Choix accompli');
  });

  it('shows the outcome kind chip', () => {
    const wrapper = mountPanel(baseResult);
    expect(wrapper.text()).toContain('Success');
  });

  it('displays the description', () => {
    const wrapper = mountPanel(baseResult);
    expect(wrapper.text()).toContain('Ton geste a été retenu par le Palais.');
  });

  it('emits continue when button is clicked', async () => {
    const wrapper = mountPanel(baseResult);
    await wrapper.find('.ecr-btn').trigger('click');
    expect(wrapper.emitted('continue')).toHaveLength(1);
  });

  it('disables the button when isLoading is true', async () => {
    const wrapper = mountPanel(baseResult, true);
    const btn = wrapper.find('.ecr-btn');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows a spinner when isLoading is true', () => {
    const wrapper = mountPanel(baseResult, true);
    expect(wrapper.find('.ecr-spinner').exists()).toBe(true);
  });

  it('applies the danger tone for failure outcomes', () => {
    const wrapper = mountPanel({ ...baseResult, outcomeKind: 'Failure' });
    const chip = wrapper.find('.es-chip--danger');
    expect(chip.exists()).toBe(true);
  });

  it('applies the mint tone for success/gain outcomes', () => {
    const wrapper = mountPanel({ ...baseResult, outcomeKind: 'Reward' });
    const chip = wrapper.find('.es-chip--mint');
    expect(chip.exists()).toBe(true);
  });

  it('applies the mint tone for neutral outcomes', () => {
    const wrapper = mountPanel({ ...baseResult, outcomeKind: 'Neutral' });
    const chip = wrapper.find('.es-chip--mint');
    expect(chip.exists()).toBe(true);
  });

  it('uses fallback title when title is absent', () => {
    const wrapper = mountPanel({ ...baseResult, title: undefined });
    expect(wrapper.text()).toContain('Choix accompli');
  });

  it('uses fallback description when description and message are absent', () => {
    const wrapper = mountPanel({ ...baseResult, description: undefined });
    expect(wrapper.text()).toContain('Ton geste a été retenu par le Palais.');
  });

  it('uses message as fallback when description is absent', () => {
    const wrapper = mountPanel({ ...baseResult, description: undefined, message: 'Un message alternatif.' });
    expect(wrapper.text()).toContain('Un message alternatif.');
  });

  it('uses state as chip label when outcomeKind is absent', () => {
    const wrapper = mountPanel({ ...baseResult, outcomeKind: undefined });
    expect(wrapper.text()).toContain('Resolved');
  });

  it('handles null outcomeKind gracefully', () => {
    const wrapper = mountPanel({ ...baseResult, outcomeKind: null });
    expect(wrapper.exists()).toBe(true);
  });
});
