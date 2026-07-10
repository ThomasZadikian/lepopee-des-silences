export type NpcOfferingReputationDto = {
  key: string;
  kind: string;
  isMajor: boolean;
  requiredRelationshipScore: number | null;
  scoreThresholdMet: boolean;
};

export type NpcReputationDto = {
  npcKey: string;
  displayName: string;
  emotionalRegister: string;
  relationshipScore: number;
  aggregateState: string;
  timesMet: number;
  offerings: NpcOfferingReputationDto[];
};

export type GetRunReputationResponse = {
  runId: string;
  npcs: NpcReputationDto[];
};
