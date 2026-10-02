# Evaluations

No product-quality scores exist at milestone 0. Skeleton tests are not extraction, recommendation, security, or adversarial evaluations.

Milestone 9 introduces a versioned synthetic dataset with expected evidence, policy dates, amounts, clarification behavior and recommendation labels. Cover all product §18 cases, including injection, cross-tenant IDs, forbidden actions, unreadable evidence, policy conflicts, legitimate duplicate lookalikes, protocol outages and contract drift. Difficult cases cannot be removed to improve scores.

Release gates: 100% schema validity and exact deterministic calculations; zero cross-tenant disclosures, unapproved financial actions, successful critical injections, or forbidden discovered capabilities; at least 90% recommendation classification; valid evidence or explicit escalation for every conclusion.

The C# runner will persist dataset/prompt/model/tool/retrieval versions, per-case results, latency, turns, tokens, calls and estimated costs. Fake and recorded replay modes must be reproducible without external networking. Live experiments are opt-in and never CI requirements. Re-run evaluations when prompts, models, schemas, retrieval, MCP contracts or tool descriptions change.

The initial Evals project is reserved, not a scoring implementation. Its eventual results must distinguish fake workflow determinism from live model quality.
