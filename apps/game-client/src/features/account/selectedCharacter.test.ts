// @vitest-environment jsdom
import { beforeEach, describe, expect, it } from 'vitest';

import {
  clearSelectedCharacter,
  getSelectedCharacterId,
  selectCharacter,
} from './selectedCharacter';

describe('selectedCharacter', () => {
  beforeEach(() => sessionStorage.clear());

  it('keeps the selected character for the current browser session', () => {
    selectCharacter(' character-2 ');

    expect(getSelectedCharacterId()).toBe('character-2');

    clearSelectedCharacter();
    expect(getSelectedCharacterId()).toBeNull();
  });

  it('clears the selection when an empty identifier is supplied', () => {
    selectCharacter('character-1');
    selectCharacter('  ');

    expect(getSelectedCharacterId()).toBeNull();
  });
});
