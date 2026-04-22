using System.Linq;
using System.Text.Json;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.Services;

public sealed class ConnectionStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;

    public ConnectionStorageService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SSHClientAvalonia");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "connections.json");
    }

    public async Task<IReadOnlyList<SshConnectionProfile>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<SshConnectionProfile>();

        await using var stream = File.OpenRead(_filePath);
        var list = await JsonSerializer.DeserializeAsync<List<SshConnectionProfile>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return list ?? [];
    }

    public async Task SaveAsync(IEnumerable<SshConnectionProfile> profiles, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, profiles.ToList(), JsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
