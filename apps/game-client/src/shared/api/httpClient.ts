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

function getErrorMessage(body: unknown, fallback: string): string {
  if (
    typeof body === 'object' &&
    body !== null &&
    'message' in body &&
    typeof body.message === 'string'
  ) {
    return body.message;
  }

  if (
    typeof body === 'object' &&
    body !== null &&
    'title' in body &&
    typeof body.title === 'string'
  ) {
    return body.title;
  }

  if (
    typeof body === 'object' &&
    body !== null &&
    'detail' in body &&
    typeof body.detail === 'string'
  ) {
    return body.detail;
  }

  if (typeof body === 'string' && body.trim().length > 0) {
    return body;
  }

  return fallback;
}

async function parseResponseBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get('content-type');
  const hasJson = contentType?.includes('application/json') ?? false;

  if (response.status === 204) {
    return null;
  }

  if (hasJson) {
    return response.json();
  }

  return response.text();
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

  const body = await parseResponseBody(response);

  if (!response.ok) {
    throw new HttpError(
      getErrorMessage(
        body,
        `Game Engine API request failed with status ${response.status}.`,
      ),
      response.status,
      body,
    );
  }

  return body as TResponse;
}