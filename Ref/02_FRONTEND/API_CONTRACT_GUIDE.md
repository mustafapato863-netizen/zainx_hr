# API Contract Guide

## REST/OpenAPI
Primary for commands, payroll, approvals, operational grids, exports, imports, files, administration and high-risk mutation.

Pipeline:
`ASP.NET OpenAPI → Orval → generated TS client/models/query bindings/MSW mocks`

## GraphQL
Optional for read composition:
- Employee Profile summary
- Manager Home
- role-aware Home
- cross-module contextual reads

Pipeline:
`GraphQL Schema → Code Generator → Typed Documents → thin transport → TanStack Query`

## Rule
Do not hand-author transport DTOs when generated contracts exist. GraphQL does not become a second server-state cache.
