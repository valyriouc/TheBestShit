# Top5 Resources Platform

Show only the current top 5 community‑validated resources per topic. Full catalog stays discoverable via navigation, not search. Users surface quality by voting and reviewing.

## Goals
- Reduce noise: search returns only the best 5 (ranked)
- Encourage curation via votes + short reviews
- Provide auditability (transparent score breakdown)
- Prevent rapid score manipulation

## Core Features
- Topic pages with live Top 5 list
- Expand to browse all hidden (non-top) resources
- Upvote / downvote with cooldown + karma weighting
- Lightweight reviews (optional)
- Rank decay to let new items emerge
- Abuse + duplicate detection (future)

## Ranking (Draft)
Score = (W_up - W_down) * TrustFactor * FreshnessDecay
- W_up / W_down: weighted by voter reputation
- TrustFactor: lowers if flags / rapid swings detected
- FreshnessDecay: exponential after configurable half‑life
Tie-breakers: lower variance of recent sentiment, then earlier creation time.

## Data Model (Simplified)
Resource(id, title, url, topicId, createdAt)
Vote(id, resourceId, userId, value[+1|-1], createdAt)
Review(id, resourceId, userId, rating(1–5 optional), text?, createdAt)
User(id, handle, reputation, createdAt)

## API Sketch
GET /topics/:id/top5
GET /resources/:id
POST /resources
POST /resources/:id/votes
POST /resources/:id/reviews
GET /resources/:id/history (score timeline)

## Anti-Gaming Ideas
- Per-user vote rate limiting
- Reputation gain from sustained agreement over time
- Downweight burst voting clusters
- Shadow quarantine for anomalous spikes

## Tech Stack (Placeholder)
Backend: (e.g. Node.js / FastAPI / Go)
DB: Postgres (window functions for rank history)
Cache: Redis
Queue: for async score recompute
Optional: Elasticsearch for non-top browse (NOT for top ranking)

## Scoring Pipeline
1. Ingest vote -> enqueue recompute
2. Aggregate net weighted votes
3. Apply decay + trust adjustments
4. Persist snapshot (resource_score_history)
5. Rebuild topic top5 cache atomically

## Local Dev Quick Start (Example)
pnpm install
pnpm dev
# or: docker compose up

Set env:
DATABASE_URL=...
REDIS_URL=...

Run migrations:
pnpm migrate

## Testing
Unit: ranking math
Integration: vote flows, decay rollover
Property-based: monotonic decay assurance

## Roadmap (Short)
- v0: Static topics + manual resource add + votes
- v1: Reviews + decay
- v2: Reputation + anti-gaming heuristics
- v3: UI polish + public score transparency graphs

## Contributing
Open issue first for non-trivial changes. Keep functions pure where possible (ranking). Add test for each scoring rule.

## Open Questions
- Minimum votes threshold before eligibility?
- Should neutral (0) adjustments exist for retract?
- Per-topic decay customization?

Refine as requirements firm up.