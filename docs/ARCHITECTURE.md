# POC architecture decisions

- **One API project, focused folders:** avoids database and infrastructure scaffolding while preserving replaceable service contracts.
- **Fixed C# fixtures:** all observations are explicit values. Aggregation is deterministic LINQ over 126 records, not a generation engine.
- **Shared filter state:** Angular signals feed typed API services; effects cancel stale subscriptions when filters change.
- **Normalized responses:** the frontend receives provider metadata, metrics, trends and account records; never raw provider responses.
- **Runtime integrations:** default fixtures are immutable; runtime overrides implement edit/disable/delete and reset on restart. Only user-supplied configurations and operational state live in mutable memory.
- **Separate secrets:** public DTOs contain nonsecret fields and a boolean credential indicator. Private runtime entries contain secrets. Response mapping remains ordinary configuration; headers/query parameters are treated as secret.
- **No background worker:** sync produces an explicit simulated result. No scheduler, seed engine or generator runs.
- **Demo capabilities:** all four fixture providers have request/token/cost observations. User breakdown is unsupported. This says nothing about official real-provider API availability.
- **No authentication:** POC is for local execution. Future authorization belongs on API policies, with route guards as a UX supplement.
- **Stable extension contracts:** IDashboardDataService separates UI-facing aggregation from the fixed data source. IAIProviderConnector describes future connection/normalized usage operations; no fake real implementations are shipped.
- **Known scope limits:** previous-period baseline is monthly, runtime integrations reset, custom REST is configuration-only, alerts are illustrative, audit UI shows latest 200 entries. These are exposed in the UI/README rather than simulated as production behavior.

