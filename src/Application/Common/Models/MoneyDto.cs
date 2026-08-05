namespace KartAdminService.Application.Common.Models;

/// <summary>api-contract.yaml Money schema.</summary>
public sealed record MoneyDto(decimal Amount, string Currency);
