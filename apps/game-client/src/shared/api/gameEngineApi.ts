import { httpRequest } from './httpClient';

export const gameEngineApi = {
  get: <TResponse>(path: string) =>
    httpRequest<TResponse>(path, {
      method: 'GET',
    }),

  getWithHeaders: <TResponse>(path: string, headers: HeadersInit) =>
    httpRequest<TResponse>(path, {
      method: 'GET',
      headers,
    }),

  post: <TResponse, TBody = unknown>(path: string, body?: TBody) =>
    httpRequest<TResponse>(path, {
      method: 'POST',
      body: body === undefined ? undefined : JSON.stringify(body),
    }),

  postWithHeaders: <TResponse, TBody = unknown>(
    path: string,
    body: TBody,
    headers: HeadersInit,
  ) => httpRequest<TResponse>(path, {
    method: 'POST',
    body: JSON.stringify(body),
    headers,
  }),
};
