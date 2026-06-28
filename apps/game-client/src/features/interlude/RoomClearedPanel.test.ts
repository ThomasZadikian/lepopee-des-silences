// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';
import RoomClearedPanel from './RoomClearedPanel.vue';
import type { RoomDto } from '../runs/types/runTypes';

function mountPanel(room: RoomDto, currentRoomIndex: number, isLoading = false) {
  return mount(RoomClearedPanel, { props: { room, currentRoomIndex, isLoading } });
}

describe('RoomClearedPanel', () => {
  const baseRoom: RoomDto = {
    id: 'room-1',
    roomType: 'Combat',
    currentNodeDepth: 0,
    maxNodeDepth: 3,
    nodes: [],
    availableNodes: [],
    bossPreview: { name: 'Gardien de la Salle', dangerHint: 'High' },
  };

  it('renders without crashing', () => {
    const wrapper = mountPanel(baseRoom, 0);
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the title', () => {
    const wrapper = mountPanel(baseRoom, 0);
    expect(wrapper.text()).toContain('Salle terminée');
  });

  it('displays the subtitle', () => {
    const wrapper = mountPanel(baseRoom, 0);
    expect(wrapper.text()).toContain('Le Palais se replie autour de vous.');
  });

  it('displays the room number', () => {
    const wrapper = mountPanel(baseRoom, 2);
    expect(wrapper.text()).toContain('Salle 3');
  });

  it('displays the room type label', () => {
    const wrapper = mountPanel(baseRoom, 0);
    expect(wrapper.text()).toContain('Salle de combat');
  });

  it('displays the boss name', () => {
    const wrapper = mountPanel(baseRoom, 0);
    expect(wrapper.text()).toContain('Gardien de la Salle');
  });

  it('shows fallback when boss name is missing', () => {
    const room = { ...baseRoom, bossPreview: { name: undefined as unknown as string, dangerHint: 'High' } };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('—');
  });

  it('emits enterInterlude when CTA is clicked', async () => {
    const wrapper = mountPanel(baseRoom, 0);
    await wrapper.find('.room-cleared__cta').trigger('click');
    expect(wrapper.emitted('enterInterlude')).toHaveLength(1);
  });

  it('disables button when isLoading is true', () => {
    const wrapper = mountPanel(baseRoom, 0, true);
    const btn = wrapper.find('.room-cleared__cta');
    expect((btn.element as HTMLButtonElement).disabled).toBe(true);
  });

  it('shows loading text when isLoading is true', () => {
    const wrapper = mountPanel(baseRoom, 0, true);
    expect(wrapper.text()).toContain('Repli en cours');
  });

  it('handles Rest room type', () => {
    const room = { ...baseRoom, roomType: 'Rest' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Havre');
  });

  it('handles Merchant room type', () => {
    const room = { ...baseRoom, roomType: 'Merchant' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Bazar du Palais');
  });

  it('handles Mystery room type', () => {
    const room = { ...baseRoom, roomType: 'Mystery' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Salle mystère');
  });

  it('handles Elite room type', () => {
    const room = { ...baseRoom, roomType: 'Elite' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Salle d\'élite');
  });

  it('handles Threshold room type', () => {
    const room = { ...baseRoom, roomType: 'Threshold' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Seuil');
  });

  it('falls back to raw roomType for unknown types', () => {
    const room = { ...baseRoom, roomType: 'Unknown' };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('Unknown');
  });

  it('handles missing bossPreview gracefully', () => {
    const room = { ...baseRoom, bossPreview: undefined as any };
    const wrapper = mountPanel(room, 0);
    expect(wrapper.text()).toContain('—');
  });
});
