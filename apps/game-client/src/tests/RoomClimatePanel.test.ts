// @vitest-environment jsdom
import { describe, expect, it } from 'vitest';
import { mount } from '@vue/test-utils';

import RoomClimatePanel from '../features/room-climate/RoomClimatePanel.vue';
import { resolveRoomClimateDisplay } from '../features/room-climate/roomClimateDisplay';

describe('RoomClimatePanel', () => {
  it('shows an empty state when no climate is active', () => {
    const wrapper = mount(RoomClimatePanel, { props: { climate: null } });

    expect(wrapper.text()).toContain('Aucun climat actif dans cette Room.');
  });

  it('displays an active string climate with public fallback labels', () => {
    const wrapper = mount(RoomClimatePanel, { props: { climate: 'Grey' } });

    expect(wrapper.text()).toContain('Grisaille');
    expect(wrapper.text()).toContain('Une grisaille pèse sur les actions dans cette Room.');
    expect(wrapper.text()).toContain('Room actuelle uniquement');
  });

  it('uses DTO display fields when provided by the server', () => {
    const wrapper = mount(RoomClimatePanel, {
      props: {
        climate: {
          key: 'climate.rain.room-1',
          type: 'Rain',
          displayName: 'Pluie fine',
          description: 'Une pluie publique renvoyée par le serveur.',
          source: 'law-tempest-v1',
          expiresWhen: 'Fin de Room',
          roomId: 'room-1',
        },
      },
    });

    expect(wrapper.text()).toContain('Pluie fine');
    expect(wrapper.text()).toContain('Une pluie publique renvoyée par le serveur.');
    expect(wrapper.text()).toContain('law-tempest-v1');
    expect(wrapper.text()).toContain('Fin de Room');
    expect(wrapper.text()).toContain('room-1');
  });

  it('does not break when optional DTO fields are absent', () => {
    const wrapper = mount(RoomClimatePanel, {
      props: { climate: { type: 'Heatwave' } },
    });

    expect(wrapper.text()).toContain('Canicule');
    expect(wrapper.text()).toContain('Room actuelle uniquement');
  });

  it('does not expose numeric effect coefficients from fallback mapping', () => {
    const wrapper = mount(RoomClimatePanel, { props: { climate: 'Hail' } });

    expect(wrapper.text()).not.toContain('25%');
    expect(wrapper.text()).not.toContain('0.25');
  });

  it('resolves display data without calculating climate effects', () => {
    const display = resolveRoomClimateDisplay({ type: 'Rain', displayName: 'Pluie' });

    expect(display).toEqual(expect.objectContaining({ displayName: 'Pluie' }));
    expect(display).not.toHaveProperty('powerMultiplier');
    expect(display).not.toHaveProperty('speedMultiplier');
    expect(display).not.toHaveProperty('healingMultiplier');
    expect(display).not.toHaveProperty('dotMultiplier');
  });
});
