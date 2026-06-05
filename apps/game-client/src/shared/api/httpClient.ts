import { environment } from '../config/environment';

export class HttpError extends Error {
  public readonly status: number;
  public readonly body: unknown;

  public constructor(message: string, status: number, body: unknown) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
    this.body = body;
  }
}

export async function httpRequest<TResponse>(
  path: string,
  options: RequestInit = {},
): Promise<TResponse> {
  const response = await fetch(`${environment.gameEngineApiUrl}${path}`, {
    ...options,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });

  const contentType = response.headers.get('content-type');
  const hasJson = contentType?.includes('application/json') ?? false;
  const body = hasJson ? await response.json() : await response.text();

  if (!response.ok) {
    throw new HttpError(
      `Game Engine API request failed with status ${response.status}.`,
      response.status,
      body,
    );
  }

  return body as TResponse;
}