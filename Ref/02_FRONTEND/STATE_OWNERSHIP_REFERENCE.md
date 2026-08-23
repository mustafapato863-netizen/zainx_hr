# State Ownership Reference

Use this decision order:

1. Backend-authoritative data? → **TanStack Query**
2. Shareable/bookmarkable state? → **TanStack Router**
3. Form values/dirty/validation? → **React Hook Form**
4. State machine with valid transitions? → **XState**
5. Cross-module application UI state? → **Redux Toolkit**
6. Local-only component state? → **React**

Prohibited:
- duplicate Query data in Redux
- filters in Redux when URL should own them
- XState for trivial open/closed UI
- Redux for form fields
- server authorization truth in frontend state
- sensitive persisted browser state
