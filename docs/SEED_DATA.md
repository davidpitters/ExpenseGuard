# Seed-data strategy

Seed version `northstar-v1` will be synthetic, idempotent and explicit opt-in for Development/Test only. Stable IDs and fixed UTC timestamps make replay and gold expectations deterministic. Do not seed production during host startup.

The future dataset includes Northstar Labs and a second isolation-test organization; employee, finance reviewer, administrator and auditor accounts; immutable dated travel/meals clauses; USD/CAD rates with synthetic source/date; conference hotel (personal minibar), restaurant (missing attendees), currency mismatch and duplicate taxi evidence. Accounts/passwords are local demo data and are separate from infrastructure credentials.

Milestone 0 only initializes the PostgreSQL `vector` extension. It does not invent approved reimbursements, login accounts, or evidence. Domain seeds and EF migrations arrive with the responsible milestones and are tested for repeated execution and production refusal.
