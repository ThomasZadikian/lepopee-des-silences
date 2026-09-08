const selectedCharacterStorageKey = 'leds.selected-character-id';
let selectedCharacterId: string | null = null;

function storage(): Storage | null {
  return typeof window !== 'undefined' && window.sessionStorage
    ? window.sessionStorage
    : null;
}

export function selectCharacter(characterId: string): void {
  const normalized = characterId.trim();
  if (!normalized) {
    clearSelectedCharacter();
    return;
  }

  selectedCharacterId = normalized;
  storage()?.setItem(selectedCharacterStorageKey, normalized);
}

export function getSelectedCharacterId(): string | null {
  return storage()?.getItem(selectedCharacterStorageKey) ?? selectedCharacterId;
}

export function clearSelectedCharacter(): void {
  selectedCharacterId = null;
  storage()?.removeItem(selectedCharacterStorageKey);
}
