import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  getMembers,
  getSession,
  signIn,
  signOut,
  type Login,
  type Session,
} from './api/auth';

export function Access() {
  const cache = useQueryClient();
  const session = useQuery({
    queryKey: ['identity', 'session'],
    queryFn: ({ signal }) => getSession(signal),
    retry: false,
  });
  const form = useForm<Login>({
    defaultValues: { workspace: 'northstar-labs', email: '', password: '' },
  });
  const login = useMutation({
    mutationFn: signIn,
    onSuccess: async () => {
      form.resetField('password');
      cache.removeQueries({ queryKey: ['members'] });
      await cache.invalidateQueries({ queryKey: ['identity'] });
    },
  });
  const logout = useMutation({
    mutationFn: signOut,
    onSuccess: () => {
      cache.removeQueries({ queryKey: ['members'] });
      cache.setQueryData(['identity', 'session'], null);
      login.reset();
    },
  });
  return (
    <>
      <div className="page-heading">
        <p className="eyebrow">ACCOUNT ACCESS</p>
        <h1>Your workspace. Your responsibilities.</h1>
        <p className="lede">
          Access is scoped to your organization and assigned role. Financial
          decisions remain with authorized human reviewers.
        </p>
      </div>
      <section className="panel access-panel" aria-labelledby="access-title">
        <h2 id="access-title">
          {session.data ? 'Signed in' : 'Sign in to ExpenseGuard'}
        </h2>
        {session.isPending ? (
          <p role="status">Checking your session…</p>
        ) : session.isError ? (
          <div role="alert">
            <p>Cannot verify your session. Check your connection and retry.</p>
            <button onClick={() => void session.refetch()}>
              Retry session
            </button>
          </div>
        ) : session.data ? (
          <>
            <dl className="session-details">
              <dt>Name</dt>
              <dd>{session.data.displayName}</dd>
              <dt>Role</dt>
              <dd>{session.data.role}</dd>
              <dt>Organization ID</dt>
              <dd>{session.data.organizationId}</dd>
            </dl>
            <button disabled={logout.isPending} onClick={() => logout.mutate()}>
              {logout.isPending ? 'Signing out…' : 'Sign out'}
            </button>
            {logout.isError && <p role="alert">{logout.error.message}</p>}
          </>
        ) : (
          <form
            className="access-form"
            onSubmit={form.handleSubmit((values) => login.mutate(values))}
          >
            <label htmlFor="workspace">Workspace</label>
            <input
              id="workspace"
              autoComplete="organization"
              {...form.register('workspace', { required: true, maxLength: 80 })}
              aria-invalid={!!form.formState.errors.workspace}
            />
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              autoComplete="username"
              {...form.register('email', { required: true, maxLength: 254 })}
              aria-invalid={!!form.formState.errors.email}
            />
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              {...form.register('password', { required: true, maxLength: 256 })}
              aria-invalid={!!form.formState.errors.password}
            />
            {Object.keys(form.formState.errors).length > 0 && (
              <p role="alert">Enter a workspace, email and password.</p>
            )}
            {login.isError && <p role="alert">{login.error.message}</p>}
            <button type="submit" disabled={login.isPending}>
              {login.isPending ? 'Signing in…' : 'Sign in'}
            </button>
            <p className="scope-note">
              Local demo accounts require HTTPS and an initialized PostgreSQL
              database. Setup and synthetic credentials are in the README.
            </p>
          </form>
        )}
      </section>
      {session.data &&
        !session.isError &&
        ['Administrator', 'Auditor'].includes(session.data.role) && (
          <MemberDirectory
            key={`${session.data.userId}:${session.data.organizationId}`}
            session={session.data}
          />
        )}
    </>
  );
}

function MemberDirectory({ session }: { session: Session }) {
  const [page, setPage] = useState(1);
  const members = useQuery({
    queryKey: ['members', session.userId, session.organizationId, page],
    queryFn: ({ signal }) => getMembers(session.organizationId, page, signal),
    retry: false,
  });
  return (
    <section className="panel access-panel" aria-labelledby="members-title">
      <h2 id="members-title">Organization members</h2>
      {members.isPending ? (
        <p role="status">Loading members…</p>
      ) : members.isError ? (
        <div role="alert">
          <p>{members.error.message}</p>
          <button onClick={() => void members.refetch()}>Retry members</button>
        </div>
      ) : (
        <>
          {members.data.items.length === 0 ? (
            <p>No active members on this page.</p>
          ) : (
            <div className="table-scroll">
              <table>
                <caption>Active organization memberships</caption>
                <thead>
                  <tr>
                    <th scope="col">Name</th>
                    <th scope="col">Role</th>
                  </tr>
                </thead>
                <tbody>
                  {members.data.items.map((member) => (
                    <tr key={member.userId}>
                      <td>{member.displayName}</td>
                      <td>{member.role}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <div className="member-pagination">
            <button disabled={page === 1} onClick={() => setPage(page - 1)}>
              Previous
            </button>
            <span>Page {page}</span>
            <button
              disabled={!members.data.hasMore}
              onClick={() => setPage(page + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}
    </section>
  );
}
