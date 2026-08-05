# Contracts

`api-contract.yaml` is a synced copy of the approved contract owned by
`kart-platform/docs/services/kart-admin-service/api-contract.yaml` (the source of truth). It is
vendored here so `tests/ContractTests` can validate this service's actual HTTP responses against
it in this repo's own CI, without a cross-repo checkout. Update it only by re-copying the upstream
file after a new contract revision is approved there — never edit it directly in this repo.

`message-bus-manifest.json` is likewise a synced copy of
`kart-platform/docs/services/kart-admin-service/message-bus-manifest.json` — this service's own
RabbitMQ topology (`admin.exchange`, owned by this service alone; no consumed events — every
`/admin/*` action is synchronous and human/operator-initiated). `RabbitMqTopologyProvisioner`
(from the vendored `Kart.Shared.Messaging` package) scans this file and declares the topology
idempotently at startup — nothing here is hardcoded in C#. Update it only by re-copying the
upstream file after a manifest revision is approved there.

`event-contract.md` is a synced copy of the same upstream directory's `event-contract.md` —
human-readable documentation of the one event this service publishes, `AdminActionPerformed`.
