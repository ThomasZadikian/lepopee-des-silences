export type UpcomingRoomDto = {
  roomIndex: number;
  key: string | null;
  displayName: string | null;
};

export type GetUpcomingRoomsResponse = {
  runId: string;
  isRevealed: boolean;
  rooms: UpcomingRoomDto[];
};
