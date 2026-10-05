import { client } from './client';
import type { components } from './schema';

export type Login = components['schemas']['LoginRequest'];
export type Session = components['schemas']['SessionResponse'];

function failure(status: number): Error {
  return new Error(
    status === 426
      ? 'Secure sign-in requires HTTPS. Follow the local HTTPS setup in the README.'
      : status === 401
        ? 'Sign-in failed. Check your credentials and workspace, or try again after the lockout period.'
        : status === 403
          ? 'Your role does not have access to this information.'
          : status === 503
            ? 'Identity storage is unavailable. Start PostgreSQL and initialize the demo database.'
            : 'The request could not be completed. Please try again.',
  );
}

export async function getSession(signal: AbortSignal) {
  const result = await client.GET('/api/auth/session', { signal });
  if (result.response.status === 401) return null;
  if (!result.response.ok || !result.data)
    throw failure(result.response.status);
  return result.data;
}

async function csrf() {
  const result = await client.GET('/api/auth/csrf', {
    signal: AbortSignal.timeout(10000),
  });
  if (!result.response.ok || !result.data)
    throw failure(result.response.status);
  return result.data.requestToken;
}

export async function signIn(body: Login) {
  const token = await csrf();
  const result = await client.POST('/api/auth/login', {
    body,
    headers: { 'X-CSRF-TOKEN': token },
    signal: AbortSignal.timeout(10000),
  });
  if (!result.response.ok) throw failure(result.response.status);
}

export async function signOut() {
  const token = await csrf();
  const result = await client.POST('/api/auth/logout', {
    headers: { 'X-CSRF-TOKEN': token },
    signal: AbortSignal.timeout(10000),
  });
  if (!result.response.ok && result.response.status !== 401)
    throw failure(result.response.status);
}

export async function getMembers(
  organizationId: string,
  page: number,
  signal: AbortSignal,
) {
  const result = await client.GET(
    '/api/organizations/{organizationId}/members',
    {
      params: { path: { organizationId }, query: { page } },
      signal,
    },
  );
  if (!result.response.ok || !result.data)
    throw failure(result.response.status);
  return result.data;
}
