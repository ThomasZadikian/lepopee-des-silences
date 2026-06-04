// ── Enemy ──────────────────────────────────────────────────────────────────
export const ENEMY_TYPE_BASIC    = 'basic'
export const ENEMY_TYPE_MINIBOSS = 'miniboss'
export const ENEMY_TYPE_BOSS     = 'boss'

export const ENEMY_STATE_REPOS      = 'REPOS'
export const ENEMY_STATE_PATROUILLE = 'PATROUILLE'
export const ENEMY_STATE_CHASSE     = 'CHASSE'
export const ENEMY_STATE_FUITE      = 'FUITE'

// ── Item ───────────────────────────────────────────────────────────────────
export const ITEM_TYPE_WEAPON     = 'weapon'
export const ITEM_TYPE_ARMOR      = 'armor'
export const ITEM_TYPE_ACCESSORY  = 'accessory'
export const ITEM_TYPE_CONSUMABLE = 'consumable'

export const ITEM_CATEGORY_POTION_HP = 'potion_hp'
export const ITEM_CATEGORY_POTION_MP = 'potion_mp'
export const ITEM_CATEGORY_ELIXIR    = 'elixir'

// ── Skill ──────────────────────────────────────────────────────────────────
export const EFFECT_DAMAGE = 'damage'
export const EFFECT_HEAL   = 'heal'
export const EFFECT_BUFF   = 'buff'
export const EFFECT_DEBUFF = 'debuff'

export const ELEMENT_NEUTRAL   = 'neutral'
export const ELEMENT_FIRE      = 'fire'
export const ELEMENT_ICE       = 'ice'
export const ELEMENT_LIGHTNING = 'lightning'

// ── NPC ────────────────────────────────────────────────────────────────────
export const NPC_TYPE_NEUTRAL  = 'neutral'
export const NPC_TYPE_MERCHANT = 'merchant'
export const NPC_TYPE_QUEST    = 'quest'
export const NPC_TYPE_ALLY     = 'ally'

export const NPC_STATE_SEREIN  = 'SEREIN'
export const NPC_STATE_APEURE  = 'APEURÉ'
export const NPC_STATE_SOULAGE = 'SOULAGÉ'
export const NPC_STATE_PANIQUE = 'PANIQUÉ'

// ── Companion ──────────────────────────────────────────────────────────────
export const COMPANION_STATE_REPOS   = 'REPOS'
export const COMPANION_STATE_JEU     = 'JEU'
export const COMPANION_STATE_MANGER  = 'MANGER'
export const COMPANION_STATE_EXCITE  = 'EXCITE'
export const COMPANION_STATE_TRISTE  = 'TRISTE'
export const COMPANION_STATE_ENDORMI = 'ENDORMI'

// ── NpcInteraction ─────────────────────────────────────────────────────────
export const NPC_EVENT_DIALOGUE       = 'DIALOGUE'
export const NPC_EVENT_TRADE          = 'TRADE'
export const NPC_EVENT_QUEST_START    = 'QUEST_START'
export const NPC_EVENT_QUEST_COMPLETE = 'QUEST_COMPLETE'

// ── Combat States ──────────────────────────────────────────────────────────
export const COMBAT_STATE_AGRESSIF  = 'AGRESSIF'
export const COMBAT_STATE_DEFENSIF  = 'DÉFENSIF'
export const COMBAT_STATE_PANIQUE   = 'PANIQUÉ'
export const COMBAT_STATE_ENRAGE    = 'ENRAGÉ'

// ── Rôles ──────────────────────────────────────────────────────────────────
export const ROLE_PLAYER = 'Player'
export const ROLE_ADMIN  = 'Admin'