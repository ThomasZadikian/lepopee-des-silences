const gameEngineApiUrl = import.meta.env.VITE_GAME_ENGINE_API_URL;

if (!gameEngineApiUrl) {
  console.warn(
    '[game-client] VITE_GAME_ENGINE_API_URL is not defined. Falling back to api.',
  );
}

export const environment = {
  gameEngineApiUrl: gameEngineApiUrl ?? 'http://localhost:5187',
} as const;