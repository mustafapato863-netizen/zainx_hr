# ADR-FE-016 — Long-Running Operations & Realtime Push Transport

**Status:** Accepted (Polling Correctness Baseline) / Deferred (Push Transport)  
**Date:** 2026-08-24  
**Owners:** Architecture / Frontend / Platform Engineering

## Context
Long-running background calculations (Payroll batch calculation, statutory compliance audits, bulk export generation) require reliable UI progress tracking, error reporting, and completion notifications across corporate intranets and on-premise proxy environments.

## Decision
1. **Canonical Correctness Authority:** Establish HTTP Polling via TanStack Query (`refetchInterval` with exponential backoff) as the authoritative, guaranteed mechanism for background job state synchronization.
2. **Push Transport Role:** Realtime push mechanisms (SignalR / Server-Sent Events) are classified strictly as **optional latency accelerators**, never as the authoritative source of domain truth.
3. **Phase 1C Decision:** Defer live WebSocket/SignalR persistent connection implementation until real-time multi-user collaborative editing is required in later product phases. TanStack Query polling provides 100% reliable state reconciliation across all corporate firewalls, VPNs, and on-premise deployments.

## Alternatives Considered
- **WebSockets / SignalR as Primary Event Source:** Prone to corporate firewall blocking, connection drops, proxy buffering, and complex state reconciliation upon reconnection.

## Consequences
- **Positive:** Zero server-side WebSocket state management complexity in initial phases; 100% reliable on-premise and air-gapped Docker deployments.
- **Negative:** Polling introduces minor latency (1-3 seconds) between backend job state change and UI refresh.
