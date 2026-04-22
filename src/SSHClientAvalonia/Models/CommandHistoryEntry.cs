namespace SSHClientAvalonia.Models;

public sealed class CommandHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Command { get; set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
