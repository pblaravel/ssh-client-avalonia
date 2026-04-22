namespace SSHClientAvalonia.Models;

public sealed class SshConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 22;

    public string Username { get; set; } = string.Empty;

    /// <summary>Plain password stored locally for convenience (MVP).</summary>
    public string? Password { get; set; }

    public string? PrivateKeyPath { get; set; }

    public string? PrivateKeyPassphrase { get; set; }
}
