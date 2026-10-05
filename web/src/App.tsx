import { NavLink, Route, Routes, Link } from 'react-router-dom';
import { Workspace } from './Workspace';
import { Access } from './Access';

export function App() {
  return (
    <div className="app-shell">
      <a className="skip-link" href="#main">
        Skip to main content
      </a>
      <aside className="sidebar" aria-label="Workspace navigation">
        <Link className="brand" to="/" aria-label="ExpenseGuard home">
          <span className="brand-mark" aria-hidden="true">
            e<span>g</span>
          </span>
          <span>
            ExpenseGuard<small>FINANCIAL OPERATIONS</small>
          </span>
        </Link>
        <div className="workspace-name">
          <span className="workspace-avatar">N</span>
          <div>
            Northstar Labs<small>Local development workspace</small>
          </div>
        </div>
        <p className="nav-label">WORKSPACE</p>
        <nav>
          <NavLink to="/access">
            <span aria-hidden="true">◉</span> Account access
          </NavLink>
          <NavLink to="/" end>
            <span aria-hidden="true">▦</span> Overview
          </NavLink>
          <NavLink to="/architecture">
            <span aria-hidden="true">◇</span> System design
          </NavLink>
        </nav>
        <div className="sidebar-foot">
          <span className="environment-dot" aria-hidden="true" />
          <span>
            Local environment<small>Foundation · v0.1.0</small>
          </span>
        </div>
      </aside>
      <div className="main-shell">
        <header className="topbar">
          <span>
            Workspace <span aria-hidden="true">/</span>{' '}
            <strong>Foundation</strong>
          </span>
          <span className="local-badge">DEVELOPMENT</span>
        </header>
        <main id="main">
          <Routes>
            <Route path="/access" element={<Access />} />
            <Route path="/" element={<Workspace />} />
            <Route path="/architecture" element={<Architecture />} />
            <Route
              path="*"
              element={
                <div className="empty-state">
                  <h1>Page not found</h1>
                  <p>This workspace page does not exist.</p>
                  <Link to="/">Return to overview</Link>
                </div>
              }
            />
          </Routes>
        </main>
        <footer>
          Every expense explained. Every decision traceable.
          <span>Human judgment stays in control.</span>
        </footer>
      </div>
    </div>
  );
}

function Architecture() {
  return (
    <>
      <div className="page-heading">
        <p className="eyebrow">SYSTEM DESIGN</p>
        <h1>A deliberate separation of responsibilities.</h1>
        <p className="lede">
          One C# agent investigates. Deterministic tools calculate. A human
          makes the financial decision.
        </p>
      </div>
      <section
        className="panel architecture-panel"
        aria-labelledby="architecture-title"
      >
        <h2 id="architecture-title">The review path</h2>
        <ol className="architecture-flow">
          <li>
            <strong>01 · Employee workspace</strong>
            <p>
              Receipts and expense details enter through the authenticated
              application API.
            </p>
          </li>
          <li>
            <strong>02 · Durable agent worker</strong>
            <p>
              A bounded run discovers MCP tools, gathers evidence, and asks for
              missing information.
            </p>
          </li>
          <li>
            <strong>03 · Protected MCP server</strong>
            <p>
              Run-scoped access to policy, duplicates, deterministic
              calculations, clarification and recommendations.
            </p>
          </li>
          <li>
            <strong>04 · Finance review</strong>
            <p>
              Evidence and a recommendation support an independently recorded
              human decision.
            </p>
          </li>
        </ol>
        <p className="scope-note">
          This is the target architecture. The current foundation implements
          process hosts, health checks and API contracts. Expense workflows and
          the complete investigation are subsequent milestones. Account access
          now provides organization-scoped Identity sessions.
        </p>
      </section>
      <div className="two-columns">
        <section className="panel">
          <h2>Evidence before conclusions</h2>
          <p>
            Immutable receipts, dated policy clauses and calculation records
            will make recommendations inspectable. Missing information requires
            clarification.
          </p>
        </section>
        <section className="panel">
          <h2>Authority belongs to people</h2>
          <p>
            Approval, rejection, export, payment and administration will never
            be available as agent tools. The MCP endpoint is currently locked.
          </p>
        </section>
      </div>
    </>
  );
}
