namespace KartAdminService.Infrastructure.Messaging;

/// <summary>
/// Binds the "RabbitMq" configuration section. Deliberately holds only connection info -
/// everything topology-related (exchanges, routing keys) lives in
/// contracts/message-bus-manifest.json, not here.
/// </summary>
public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";

    /// <summary>
    /// AMQP port. Defaults to RabbitMQ's standard 5672 - override when the broker is reached
    /// through a remapped host port (e.g. kart-devops/docker-compose.yml's shared dev broker is
    /// published on host port 5673, since 5672 is commonly already taken locally).
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Dedicated non-guest broker credentials. RabbitMQ's default "guest" user is restricted to
    /// loopback-only connections, so any broker reached over a real network hop needs a real
    /// user. Left unset, RabbitMQ.Client falls back to its own guest/guest default.
    /// </summary>
    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string ManifestPath { get; set; } = "message-bus-manifest.json";
}
