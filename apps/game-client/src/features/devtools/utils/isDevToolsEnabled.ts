export function isDevToolsEnabled(): boolean {
  return import.meta.env.DEV === true &&
    import.meta.env.VITE_GAME_CLIENT_DEVTOOLS_ENABLED === 'true';
}
