import { httpRequest } from './httpClient';

export const gameEngineApi = {
  get: <TResponse>(path: string) =>
    httpRequest<TResponse>(path, {
      method: 'GET',
    }),

  post: <TResponse, TBody = unknown>(path: string, body?: TBody) =>
    httpRequest<TResponse>(path, {
      method: 'POST',
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
};