import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import * as Tooltip from '@radix-ui/react-tooltip';
import { getSystemSummary, type ComponentSummary } from './api/client';

const descriptions: Record<string, string> = {
  api: 'Human workflows & REST',
  worker: 'Durable investigation runtime',
  mcp: 'Controlled business tool boundary',
  postgres: 'Authoritative data & policy vectors',
  redis: 'Cache & live progress',
  'object-storage': 'Original receipt evidence',
};

export function Workspace() {
  const [interval, setInterval] = useState(30);
  const { register, handleSubmit } = useForm<{ interval: string }>({
    defaultValues: { interval: '30' },
  });
  const query = useQuery({
    queryKey: ['system-summary'],
    queryFn: ({ signal }) => getSystemSummary(signal),
    refetchInterval: interval === 0 ? false : interval * 1000,
  });
  const components = query.data?.components ?? [];
  const healthy = components.filter(
    (component) => component.status === 'healthy',
  ).length;
  const allReady =
    components.length > 0 && healthy === components.length && !query.isError;

  return (
    <>
      <div className="page-heading heading-row">
        <div>
          <p className="eyebrow">WORKSPACE OVERVIEW</p>
          <h1>Built on a clear foundation.</h1>
          <p className="lede">
            A live view of the services behind your expense workspace.
          </p>
        </div>
        <button
          className="button refresh"
          disabled={query.isFetching}
          onClick={() => void query.refetch()}
        >
          <span aria-hidden="true">↻</span>
          {query.isFetching ? 'Checking…' : 'Refresh status'}
        </button>
      </div>
      <div className="milestone-note">
        <span className="milestone-number">00</span>
        <div>
          <strong>Executable foundation</strong>
          <p>
            Service hosts and API contracts are available. Expense intake, agent
            reviews and finance decisions are upcoming milestones.
          </p>
        </div>
        <span className="outline-badge">IN DEVELOPMENT</span>
      </div>
      <div className="summary-grid">
        <section className="summary-card">
          <p className="card-label">SERVICE READINESS</p>
          <div className="summary-value">
            {query.data ? (
              <>
                {healthy}
                <span> / {components.length}</span>
              </>
            ) : (
              '—'
            )}
          </div>
          <p>
            {query.isError
              ? 'Connection lost · status may be stale'
              : allReady
                ? 'All local components responding'
                : query.isPending
                  ? 'Waiting for the local API'
                  : 'Some components need attention'}
          </p>
        </section>
        <section className="summary-card">
          <p className="card-label">AGENT MODE</p>
          <div className="summary-word">Inactive</div>
          <p>No model requests or API spend</p>
        </section>
        <section className="summary-card">
          <p className="card-label">FINANCIAL AUTHORITY</p>
          <div className="summary-word">Human reviewer</div>
          <p>Agent recommendations never decide</p>
        </section>
      </div>
      <div className="content-grid">
        <section
          className="panel service-panel"
          aria-labelledby="services-title"
        >
          <div className="panel-heading">
            <div>
              <h2 id="services-title">Local services</h2>
              <p>Readiness reflects actual dependency checks.</p>
            </div>
            <span
              className={
                allReady ? 'status-pill healthy' : 'status-pill neutral'
              }
            >
              {allReady ? 'Ready' : 'Setup in progress'}
            </span>
          </div>
          {query.isPending && (
            <div className="loading-state" role="status">
              <span className="loading-dot" />
              Checking the local workspace…
            </div>
          )}
          {query.isError && (
            <div className="error-state" role="alert">
              <strong>Cannot reach the workspace API</strong>
              <p>
                Start the API on localhost:5100, then refresh. Previously
                received results, if shown, are stale.
              </p>
            </div>
          )}
          {!query.isPending && !query.isError && components.length === 0 && (
            <div className="empty-state">
              <h3>No services reported</h3>
              <p>
                The API returned an empty component list. Refresh to check
                again.
              </p>
            </div>
          )}
          {components.length > 0 && (
            <div className="table-scroll">
              <table>
                <caption className="sr-only">
                  Local component health{query.isError ? ' (stale)' : ''}
                </caption>
                <thead>
                  <tr>
                    <th scope="col">COMPONENT</th>
                    <th scope="col">STATUS</th>
                  </tr>
                </thead>
                <tbody>
                  {components.map((component) => (
                    <ServiceRow
                      key={component.id}
                      component={component}
                      stale={query.isError}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="panel-footer">
            <span>
              {query.data?.checkedAt ? (
                <>
                  Last checked{' '}
                  <time dateTime={query.data.checkedAt}>
                    {new Intl.DateTimeFormat(undefined, {
                      hour: '2-digit',
                      minute: '2-digit',
                      second: '2-digit',
                    }).format(new Date(query.data.checkedAt))}
                  </time>
                </>
              ) : (
                'No status received yet'
              )}
            </span>
            <span>Loopback only</span>
          </div>
        </section>
        <aside
          className="panel boundary-panel"
          aria-labelledby="boundary-title"
        >
          <div className="boundary-icon" aria-hidden="true">
            ◇
          </div>
          <p className="eyebrow">BUILT-IN BOUNDARIES</p>
          <h2 id="boundary-title">
            Trust is earned
            <br />
            at every step.
          </h2>
          <ul className="boundary-list">
            <li>
              <strong>MCP access is locked</strong>
              <p>
                Business tools remain unavailable until scoped authorization is
                implemented.
              </p>
            </li>
            <li>
              <strong>No financial actions</strong>
              <p>Decisions are reserved for authorized finance reviewers.</p>
            </li>
            <li>
              <strong>Honest system health</strong>
              <p>
                Unavailable dependencies remain visible. A running process alone
                is not readiness.
              </p>
            </li>
          </ul>
          <div className="scope-note">
            Synthetic organization · No financial records or personal data
            loaded.
          </div>
        </aside>
      </div>
      <form
        className="refresh-settings"
        onSubmit={(event) =>
          void handleSubmit((values) => setInterval(Number(values.interval)))(
            event,
          )
        }
      >
        <div>
          <label htmlFor="refresh-interval">Status refresh</label>
          <p>Choose how often this page checks local services.</p>
        </div>
        <div className="refresh-controls">
          <select id="refresh-interval" {...register('interval')}>
            <option value="15">Every 15 seconds</option>
            <option value="30">Every 30 seconds</option>
            <option value="0">Manual only</option>
          </select>
          <button className="button secondary" type="submit">
            Apply
          </button>
        </div>
        <span className="refresh-confirmation" role="status">
          {interval === 0
            ? 'Automatic refresh paused'
            : `Checking every ${interval} seconds`}
        </span>
      </form>
    </>
  );
}

function ServiceRow({
  component,
  stale,
}: {
  component: ComponentSummary;
  stale: boolean;
}) {
  const status = stale ? 'stale' : component.status;
  const label =
    status === 'healthy'
      ? 'Healthy'
      : status === 'not_configured'
        ? 'Not configured'
        : status === 'stale'
          ? 'Stale'
          : 'Unavailable';
  return (
    <tr>
      <th scope="row">
        <div className="component-name">
          <span className="component-icon" aria-hidden="true">
            {component.id === 'postgres'
              ? '▤'
              : component.id === 'mcp'
                ? '⌘'
                : '◫'}
          </span>
          <div>
            {component.name}
            <small>
              {descriptions[component.id ?? ''] ?? 'Workspace component'}
            </small>
          </div>
        </div>
      </th>
      <td>
        <div className="status-cell">
          <span
            className={`status-pill ${status === 'healthy' ? 'healthy' : 'attention'}`}
          >
            <span className="status-dot" />
            {label}
          </span>
          <Tooltip.Provider>
            <Tooltip.Root>
              <Tooltip.Trigger asChild>
                <button
                  className="help-button"
                  aria-label={`Details for ${component.name}`}
                >
                  ?
                </button>
              </Tooltip.Trigger>
              <Tooltip.Portal>
                <Tooltip.Content className="tooltip-content" sideOffset={6}>
                  {stale
                    ? 'The last refresh failed. Check the connection before relying on this status.'
                    : component.detail}
                  <Tooltip.Arrow />
                </Tooltip.Content>
              </Tooltip.Portal>
            </Tooltip.Root>
          </Tooltip.Provider>
        </div>
      </td>
    </tr>
  );
}
