import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { App } from './App';

function renderWorkspace(path = '/') {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: 0 } },
  });
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const summary = {
  product: 'ExpenseGuard',
  version: '0.1.0',
  milestone: '0',
  modelMode: 'Disabled',
  financialAuthority: 'Human finance reviewer',
  checkedAt: '2026-10-02T12:00:00Z',
  components: [
    {
      id: 'api',
      name: 'Application API',
      status: 'healthy',
      detail: 'HTTP request served.',
    },
    {
      id: 'postgres',
      name: 'PostgreSQL + pgvector',
      status: 'not_configured',
      detail: 'Local connection is not configured.',
    },
  ],
};

describe('workspace status', () => {
  it('shows loading before a response arrives', () => {
    vi.mocked(fetch).mockImplementation(() => new Promise<Response>(() => {}));
    renderWorkspace();
    expect(screen.getByText('Checking the local workspace…')).toBeVisible();
    expect(screen.queryByText('Healthy')).not.toBeInTheDocument();
  });

  it('uses actual health and explains the current milestone', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify(summary), {
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    renderWorkspace();
    expect(await screen.findByText('PostgreSQL + pgvector')).toBeVisible();
    expect(screen.getByText('Not configured')).toBeVisible();
    expect(screen.getByText('Healthy')).toBeVisible();
    expect(screen.getByText('MCP access is locked')).toBeVisible();
    expect(
      screen.queryByRole('button', { name: 'Approve' }),
    ).not.toBeInTheDocument();
  });

  it('shows safe failure and recovers using refresh', async () => {
    const user = userEvent.setup();
    renderWorkspace();
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot reach the workspace API',
    );
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify(summary), {
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    await user.click(screen.getByRole('button', { name: /Refresh status/ }));
    expect(await screen.findByText('Application API')).toBeVisible();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('preserves but marks stale results when refresh fails', async () => {
    const user = userEvent.setup();
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify(summary), {
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    renderWorkspace();
    await screen.findByText('Application API');
    vi.mocked(fetch).mockRejectedValue(new Error('offline'));
    await user.click(screen.getByRole('button', { name: /Refresh status/ }));
    await screen.findByRole('alert');
    expect(screen.getAllByText('Stale')).toHaveLength(2);
    expect(screen.queryByText('Healthy')).not.toBeInTheDocument();
  });

  it('allows keyboard-friendly refresh preferences', async () => {
    const user = userEvent.setup();
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify(summary), {
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    renderWorkspace();
    await screen.findByText('Application API');
    await user.selectOptions(screen.getByLabelText('Status refresh'), '0');
    await user.click(screen.getByRole('button', { name: 'Apply' }));
    await waitFor(() =>
      expect(screen.getByText('Automatic refresh paused')).toBeVisible(),
    );
  });

  it('provides a real system-design route', () => {
    renderWorkspace('/architecture');
    expect(
      screen.getByRole('heading', {
        name: 'A deliberate separation of responsibilities.',
      }),
    ).toBeVisible();
    expect(fetch).not.toHaveBeenCalled();
  });

  it('shows an empty response without inventing service status', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ ...summary, components: [] }), {
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    renderWorkspace();
    expect(await screen.findByText('No services reported')).toBeVisible();
    expect(screen.queryByText('Healthy')).not.toBeInTheDocument();
  });
});
