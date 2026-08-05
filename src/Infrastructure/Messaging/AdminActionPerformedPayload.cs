namespace KartAdminService.Infrastructure.Messaging;

/// <summary>event-contract.md's AdminActionPerformed payload — adminId, action, entityId. Built by the outbox relay from admin_actions' structured columns at publish time (not stored as a precomputed blob, unlike other services' Outbox rows).</summary>
public sealed record AdminActionPerformedPayload(string AdminId, string Action, string EntityId);
