// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import PartyPanel from './PartyPanel.vue';

function mountPanel() {
  return mount(PartyPanel);
}

describe('PartyPanel', () => {
  it('renders without crashing', () => {
    const wrapper = mountPanel();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the header label', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('PARTY_MEMBER');
  });

  it('renders two party members', () => {
    const wrapper = mountPanel();
    expect(wrapper.findAll('.party__member').length).toBe(2);
  });

  it('displays member names', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('NAME');
  });

  it('displays member roles', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('PLAYER');
    expect(wrapper.text()).toContain('COMPANION');
  });

  it('displays curse tag', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('CURSE');
  });

  it('displays buff tag with frost class', () => {
    const wrapper = mountPanel();
    expect(wrapper.find('.party__tag--frost').exists()).toBe(true);
    expect(wrapper.text()).toContain('BUFF');
  });

  it('renders portrait placeholders', () => {
    const wrapper = mountPanel();
    expect(wrapper.findAll('.party__portrait').length).toBe(2);
  });

  it('renders meter elements', () => {
    const wrapper = mountPanel();
    expect(wrapper.findAll('meter').length).toBe(2);
  });

  it('displays vitality and power points', () => {
    const wrapper = mountPanel();
    expect(wrapper.text()).toContain('PV 138 / 190');
    expect(wrapper.text()).toContain('PP 6 / 12');
  });
});
