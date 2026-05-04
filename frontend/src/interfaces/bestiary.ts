export interface Enemy {
  id:                 number
  name:               string
  type:               string
  maxHP:              number
  strength:           number
  intelligence:       number
  speed:              number
  physicalResistance: number
  magicalResistance:  number
  experienceReward:   number
  goldReward:         number
  description:        string | null
}

export interface BestiaryUnlock {
  id:         number
  playerId:   number
  enemyId:    number
  unlockedAt: string
  enemy:      Enemy
}