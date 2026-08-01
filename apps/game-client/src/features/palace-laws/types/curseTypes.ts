export type CurseDefinitionView = {
  key: string;
  displayName: string;
  description: string;
  narrativeText: string | null;
  severity: number;
  duration: string;
  trigger: string | null;
};
