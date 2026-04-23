namespace SSHClientAvalonia.Models;

/// <summary>Встроенный профиль для просмотра интерфейса без настройки сервера.</summary>
public static class DemoConnections
{
    /// <summary>Стабильный Id, чтобы история команд для демо хранилась отдельно.</summary>
    public static readonly Guid LocalDemoId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001");

    public static SshConnectionProfile CreateLocalDemo() => new()
    {
        Id = LocalDemoId,
        Name = "Демо (localhost)",
        Host = "127.0.0.1",
        Port = 22,
        Username = "demo",
        Password = "demo",
    };
}
