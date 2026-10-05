import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { Access } from './Access';

function renderAccess() {
  render(
    <QueryClientProvider
      client={
        new QueryClient({
          defaultOptions: { queries: { retry: false, gcTime: 0 } },
        })
      }
    >
      <Access />
    </QueryClientProvider>,
  );
}
const session = {
  userId: 'employee-id',
  organizationId: 'org-id',
  displayName: 'Alex Morgan',
  role: 'Employee',
};
function json(value: unknown) {
  return new Response(JSON.stringify(value), {
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('account access', () => {
  it('signs in and out with freshly issued antiforgery tokens without loading an employee directory', async () => {
    let signedIn = false;
    let tokens = 0;
    vi.mocked(fetch).mockImplementation(async (input) => {
      const request = input as Request;
      const path = new URL(request.url).pathname;
      if (path.endsWith('/session'))
        return signedIn ? json(session) : new Response(null, { status: 401 });
      if (path.endsWith('/csrf'))
        return json({ requestToken: `token-${++tokens}` });
      expect(request.headers.get('X-CSRF-TOKEN')).toBe(`token-${tokens}`);
      if (path.endsWith('/login')) {
        expect(await request.json()).toEqual({
          email: 'employee@northstar.example',
          password: 'synthetic-password',
          workspace: 'northstar-labs',
        });
        signedIn = true;
      } else if (path.endsWith('/logout')) signedIn = false;
      else throw new Error('Unexpected endpoint');
      return new Response(null, { status: 204 });
    });
    renderAccess();
    const user = userEvent.setup();
    await user.type(
      await screen.findByLabelText('Email'),
      'employee@northstar.example',
    );
    await user.type(screen.getByLabelText('Password'), 'synthetic-password');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByText('Alex Morgan')).toBeVisible();
    expect(screen.queryByText('Organization members')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(await screen.findByLabelText('Password')).toHaveValue('');
    expect(tokens).toBe(2);
  });

  it('explains HTTPS requirements and does not submit credentials when token issuance fails', async () => {
    vi.mocked(fetch).mockImplementation(
      async (input) =>
        new Response(null, {
          status: new URL((input as Request).url).pathname.endsWith('/session')
            ? 401
            : 426,
        }),
    );
    renderAccess();
    const user = userEvent.setup();
    await user.type(
      await screen.findByLabelText('Email'),
      'employee@northstar.example',
    );
    await user.type(screen.getByLabelText('Password'), 'synthetic-password');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'requires HTTPS',
    );
    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it('does not display a sign-in form when session verification is unavailable', async () => {
    vi.mocked(fetch).mockRejectedValue(new Error('offline'));
    renderAccess();
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot verify your session',
    );
    expect(screen.queryByLabelText('Password')).not.toBeInTheDocument();
  });
});
