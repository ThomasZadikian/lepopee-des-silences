import { describe, expect, it } from 'vitest';
import { resolveRoomClimateDisplay } from './roomClimateDisplay';
import type { RoomClimateStateDto } from '../runs/types/runTypes';

describe('resolveRoomClimateDisplay', () => {
  it('returns null for null input', () => {
    expect(resolveRoomClimateDisplay(null)).toBeNull();
  });

  it('returns null for undefined input', () => {
    expect(resolveRoomClimateDisplay(undefined)).toBeNull();
  });

  it('resolves string climate type', () => {
    const result = resolveRoomClimateDisplay('Grey' as any);
    expect(result).not.toBeNull();
    expect(result!.displayName).toBe('Grisaille');
    expect(result!.description).toBe('Une grisaille pèse sur les actions dans cette Room.');
  });

  it('resolves Rain climate', () => {
    const result = resolveRoomClimateDisplay('Rain' as any);
    expect(result!.displayName).toBe('Pluie');
    expect(result!.description).toBe('La pluie accélère le rythme de cette Room.');
  });

  it('resolves Heatwave climate', () => {
    const result = resolveRoomClimateDisplay('Heatwave' as any);
    expect(result!.displayName).toBe('Canicule');
    expect(result!.description).toBe('La chaleur altère les soins reçus dans cette Room.');
  });

  it('resolves Hail climate', () => {
    const result = resolveRoomClimateDisplay('Hail' as any);
    expect(result!.displayName).toBe('Grêle');
    expect(result!.description).toBe('La grêle rend les effets persistants plus menaçants dans cette Room.');
  });

  it('falls back to raw type for unknown string climate', () => {
    const result = resolveRoomClimateDisplay('Unknown' as any);
    expect(result!.displayName).toBe('Unknown');
    expect(result!.description).toBeNull();
  });

  it('resolves DTO climate with display fields', () => {
    const climate: RoomClimateStateDto = {
      key: 'climate.rain.room-1',
      type: 'Rain',
      displayName: 'Pluie fine',
      description: 'Une pluie personnalisée.',
      source: 'law-tempest-v1',
      expiresWhen: 'Fin de Room',
      roomId: 'room-1',
    };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.displayName).toBe('Pluie fine');
    expect(result!.description).toBe('Une pluie personnalisée.');
    expect(result!.source).toBe('law-tempest-v1');
    expect(result!.duration).toBe('Fin de Room');
    expect(result!.roomId).toBe('room-1');
  });

  it('uses fallback when DTO has no display fields', () => {
    const climate: RoomClimateStateDto = { type: 'Rain' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.displayName).toBe('Pluie');
    expect(result!.description).toBe('La pluie accélère le rythme de cette Room.');
  });

  it('uses type as fallback when no display name', () => {
    const climate: RoomClimateStateDto = { type: 'Unknown' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.displayName).toBe('Unknown');
  });

  it('uses generic fallback when no type', () => {
    const climate: RoomClimateStateDto = { key: 'some-key' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.displayName).toBe('some-key');
  });

  it('uses key as type fallback', () => {
    const climate: RoomClimateStateDto = { key: 'climate.unknown' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.type).toBe('climate.unknown');
  });

  it('sets default duration when no expires fields', () => {
    const climate: RoomClimateStateDto = { type: 'Rain' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.duration).toBe('Room actuelle uniquement');
  });

  it('uses expiresAt when expiresWhen is absent', () => {
    const climate: RoomClimateStateDto = { type: 'Rain', expiresAt: '2026-01-01' };
    const result = resolveRoomClimateDisplay(climate);
    expect(result!.duration).toBe('2026-01-01');
  });

  it('does not include numeric effect coefficients', () => {
    const result = resolveRoomClimateDisplay('Hail' as any);
    expect(result).not.toHaveProperty('powerMultiplier');
    expect(result).not.toHaveProperty('speedMultiplier');
    expect(result).not.toHaveProperty('healingMultiplier');
    expect(result).not.toHaveProperty('dotMultiplier');
  });

  it('returns all required fields for string climate', () => {
    const result = resolveRoomClimateDisplay('Grey' as any);
    expect(result).toHaveProperty('key');
    expect(result).toHaveProperty('type');
    expect(result).toHaveProperty('displayName');
    expect(result).toHaveProperty('description');
    expect(result).toHaveProperty('source');
    expect(result).toHaveProperty('duration');
    expect(result).toHaveProperty('roomId');
  });
});
