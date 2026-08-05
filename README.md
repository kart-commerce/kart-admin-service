# kart-admin-service

Back-office operations and fine-grained, category-scoped RBAC for the platform's back-office
operators. A thin orchestration/control-plane service — never a second owner of another
service's domain data (Domain Invariant #3): every mutation is a synchronous proxy call to the
service that actually owns that data (Product, Category, Offer, Identity, Inventory), gated by
Admin's own `admin_permission_grants` fine-grained check on top of the coarse Identity-issued
`Admin` role claim.

Design docs: `kart-platform/docs/services/kart-admin-service/`.

Deliberately different from most other Kart services: **PostgreSQL only, no MongoDB, no read
model, no CQRS read/write split** — Admin has no read-heavy, latency-budgeted query path of its
own, and caching the permission-grant lookup was explicitly rejected (would reopen a
revocation-staleness security hole). See `database-design.md`'s "No read model" note.

## Layout

Clean Architecture + Vertical Slice (`docs/standards/folder-structure.md` in
[agent-reusables](https://github.com/kakon-mehedi/agent-reusables)):

```
src/
├── Api/              # ASP.NET Core controllers, thin — maps to Application
├── Application/       # Features/<UseCaseName>/ vertical slices (MediatR), one per ticket ADM-1..ADM-16
├── Domain/             # AdminPermissionGrant, AdminAction — the only two aggregates this service owns
└── Infrastructure/    # EF Core, RabbitMQ outbox relay, downstream HTTP clients (Product/Category/Offer/Identity/Inventory)
tests/
├── UnitTests/          # colocated by feature, mirrors Application/Features
├── IntegrationTests/   # Testcontainers Postgres+RabbitMQ — idempotency race, concurrency, outbox, RLS
└── ContractTests/      # validates live responses against contracts/api-contract.yaml
contracts/              # synced copy of the approved api-contract.yaml/message-bus-manifest.json (see contracts/README.md)
```

## The two tables this service owns

- `admin_permission_grants` — fine-grained, category-scoped, default-deny permission ledger.
  Five categories: `catalog-management`, `coupon-issuance`, `user-suspension`,
  `inventory-replenishment`, `permission-management` (the meta-category that governs the other
  four). At most one live grant per `(principal, category)`.
- `admin_actions` — append-only audit trail AND the Outbox row for the one event this service
  publishes, `AdminActionPerformed`. `idempotency_key` is unique — this is the concrete
  no-double-execution guarantee every mutating `/admin/*` action gets, whether the client retries
  sequentially or two requests race concurrently (see `AdminActionExecutor` and
  `AdminActionRepository.AddAndCommitOrGetExistingAsync`).

## Running locally

Requires the .NET 8 SDK, a PostgreSQL 16 instance, and a RabbitMQ instance.

```
dotnet build
dotnet test
```

Bootstrap the first `permission-management` grant for a deployment (not self-service — see
`requirement-spec.md` §6 Decision item 1) after running migrations:

```
scripts/migrate.sh
scripts/seed-permission-management-grant.sh <your-identity-principal-id>
dotnet run --project src/Api
```

`src/Api/appsettings.Development.json` points the five downstream service clients at the local
port convention `kart-devops/docker-compose.yml` uses. Every mutating `/admin/*` request requires
an `Idempotency-Key` header (UUID) — this is broader than the platform default of "money-moving
POSTs only," per this service's own `design-decisions.md`: back-office actions are the platform's
highest-privilege operations.

The GlobalConfig secrets file path is machine-specific — see `src/Api/appsettings.Local.json.example`.