import createClient from 'openapi-fetch';
import type { paths, components } from './schema';

export type SystemSummary = components['schemas']['SystemSummary'];
export type ComponentSummary = components['schemas']['ComponentSummary'];

const client = createClient<paths>({
  baseUrl: window.location.origin,
  fetch: (request) => globalThis.fetch(request),
});

export async function getSystemSummary(
  signal: AbortSignal,
): Promise<SystemSummary> {
  const result = await client.GET('/api/system', {
    signal: AbortSignal.any([signal, AbortSignal.timeout(6000)]),
  });
  if (!result.response.ok || !result.data) {
    throw new Error('The local API could not return workspace status.');
  }
  return result.data;
}
