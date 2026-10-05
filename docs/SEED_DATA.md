# Seed-data strategy

Seed version `northstar-v1` will be synthetic, idempotent and explicit opt-in for Development/Test only. Stable IDs and fixed UTC timestamps make replay and gold expectations deterministic. Do not seed production during host startup.

The future dataset includes Northstar Labs and a second isolation-test organization; employee, finance reviewer, administrator and auditor accounts; immutable dated travel/meals clauses; USD/CAD rates with synthetic source/date; conference hotel (personal minibar), restaurant (missing attendees), currency mismatch and duplicate taxi evidence. Accounts/passwords are local demo data and are separate from infrastructure credentials.

Milestone 1 adds stable Northstar organization/account IDs and four synthetic accounts. See LOCAL_SETUP.md for opt-in startup and credentials. Tests verify repeated execution and production refusal. Expense evidence, rates and policies remain future seeds; no approved reimbursements are invented.
