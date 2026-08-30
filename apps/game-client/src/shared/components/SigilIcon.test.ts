// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import SigilIcon from './SigilIcon.vue';

function mountIcon(kind = 'combat', size = 22, strokeWidth = 1.4) {
  return mount(SigilIcon, { props: { kind, size, strokeWidth } });
}

describe('SigilIcon', () => {
  it('renders without crashing', () => {
    const wrapper = mountIcon();
    expect(wrapper.exists()).toBe(true);
  });

  it('renders as SVG element', () => {
    const wrapper = mountIcon();
    expect(wrapper.element.tagName).toBe('svg');
  });

  it('sets correct width and height', () => {
    const wrapper = mountIcon('combat', 32);
    expect(wrapper.attributes('width')).toBe('32');
    expect(wrapper.attributes('height')).toBe('32');
  });

  it('sets correct stroke width', () => {
    const wrapper = mountIcon('combat', 22, 2);
    expect(wrapper.attributes('stroke-width')).toBe('2');
  });

  it('renders combat sigil (triangle)', () => {
    const wrapper = mountIcon('combat');
    expect(wrapper.html()).toContain('path');
  });

  it('renders elite sigil (triangle + circle)', () => {
    const wrapper = mountIcon('elite');
    const paths = wrapper.findAll('path');
    const circles = wrapper.findAll('circle');
    expect(paths.length).toBeGreaterThanOrEqual(1);
    expect(circles.length).toBeGreaterThanOrEqual(1);
  });

  it('renders memoire sigil (double circle)', () => {
    const wrapper = mountIcon('memoire');
    expect(wrapper.findAll('circle').length).toBe(2);
  });

  it('renders repos sigil (crescent)', () => {
    const wrapper = mountIcon('repos');
    expect(wrapper.html()).toContain('path');
  });

  it('renders marchand sigil (diamond)', () => {
    const wrapper = mountIcon('marchand');
    expect(wrapper.html()).toContain('path');
  });

  it('renders loi sigil (filled diamond)', () => {
    const wrapper = mountIcon('loi');
    expect(wrapper.html()).toContain('fill="currentColor"');
  });

  it('renders malediction sigil (dashed circle)', () => {
    const wrapper = mountIcon('malediction');
    expect(wrapper.html()).toContain('stroke-dasharray');
  });

  it('renders pnj sigil (circle + dot)', () => {
    const wrapper = mountIcon('pnj');
    expect(wrapper.findAll('circle').length).toBe(2);
  });

  it('renders objet sigil (filled diamond)', () => {
    const wrapper = mountIcon('objet');
    expect(wrapper.html()).toContain('fill="currentColor"');
  });

  it('renders rare sigil (4-pointed star)', () => {
    const wrapper = mountIcon('rare');
    expect(wrapper.html()).toContain('fill="currentColor"');
  });

  it('renders boss sigil (triple circle + diamond)', () => {
    const wrapper = mountIcon('boss');
    expect(wrapper.findAll('circle').length).toBe(2);
  });

  it('renders seuil sigil (arch)', () => {
    const wrapper = mountIcon('seuil');
    expect(wrapper.html()).toContain('path');
  });

  it('renders generation sigil (cross + circle)', () => {
    const wrapper = mountIcon('generation');
    expect(wrapper.html()).toContain('path');
    expect(wrapper.html()).toContain('circle');
  });

  it('renders run sigil (same as generation)', () => {
    const wrapper = mountIcon('run');
    expect(wrapper.html()).toContain('path');
  });

  it('renders narration sigil (text lines)', () => {
    const wrapper = mountIcon('narration');
    expect(wrapper.html()).toContain('path');
  });

  it('renders recompense sigil (diamond outline)', () => {
    const wrapper = mountIcon('recompense');
    expect(wrapper.html()).toContain('path');
  });

  it('renders sort sigil (3-pointed star)', () => {
    const wrapper = mountIcon('sort');
    expect(wrapper.html()).toContain('path');
  });

  it('renders map sigil (folded map)', () => {
    const wrapper = mountIcon('map');
    expect(wrapper.html()).toContain('path');
  });

  it('renders vitalite sigil (heart + pulse)', () => {
    const wrapper = mountIcon('vitalite');
    expect(wrapper.findAll('path').length).toBe(2);
  });

  it('renders fallback for unknown kind', () => {
    const wrapper = mountIcon('unknown-kind');
    expect(wrapper.html()).toContain('circle');
    expect(wrapper.html()).toContain('stroke-dasharray');
  });

  it('uses default size of 22', () => {
    const wrapper = mount(SigilIcon, { props: { kind: 'combat' } });
    expect(wrapper.attributes('width')).toBe('22');
  });

  it('uses default stroke width of 1.4', () => {
    const wrapper = mount(SigilIcon, { props: { kind: 'combat' } });
    expect(wrapper.attributes('stroke-width')).toBe('1.4');
  });

  it('sets viewBox to 0 0 24 24', () => {
    const wrapper = mountIcon();
    expect(wrapper.attributes('viewBox')).toBe('0 0 24 24');
  });

  it('sets fill to none', () => {
    const wrapper = mountIcon('combat');
    expect(wrapper.attributes('fill')).toBe('none');
  });
});
