using System.Collections.Concurrent;
using System.Text.Json;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.Services;

/// <summary>Persists command history per connection on disk.</summary>
public sealed class CommandHistoryRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly ConcurrentDictionary<Guid, List<CommandHistoryEntry>> _cache = new();

    public CommandHistoryRepository()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SSHClientAvalonia");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "command-history.json");
    }

    public async Task LoadFromDiskAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
            return;

        await using var stream = File.OpenRead(_filePath);
        var dict = await JsonSerializer.DeserializeAsync<Dictionary<Guid, List<CommandHistoryEntry>>>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (dict is null)
            return;

        foreach (var (id, list) in dict)
            _cache[id] = list;
    }

    public IReadOnlyList<CommandHistoryEntry> GetHistory(Guid connectionId)
    {
        return _cache.GetOrAdd(connectionId, _ => []);
    }

    public void Add(Guid connectionId, CommandHistoryEntry entry)
    {
        var list = _cache.GetOrAdd(connectionId, _ => []);
        lock (list)
            list.Insert(0, entry);
        _ = PersistAsync();
    }

    public bool Remove(Guid connectionId, Guid entryId)
    {
        if (!_cache.TryGetValue(connectionId, out var list))
            return false;

        lock (list)
        {
            var idx = list.FindIndex(e => e.Id == entryId);
            if (idx < 0)
                return false;
            list.RemoveAt(idx);
        }

        _ = PersistAsync();
        return true;
    }

    public void Clear(Guid connectionId)
    {
        if (_cache.TryGetValue(connectionId, out var list))
        {
            lock (list)
                list.Clear();
        }

        _ = PersistAsync();
    }

    private async Task PersistAsync()
    {
        var snapshot = new Dictionary<Guid, List<CommandHistoryEntry>>();
        foreach (var kv in _cache)
        {
            lock (kv.Value)
                snapshot[kv.Key] = kv.Value.ToList();
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions).ConfigureAwait(false);
    }
}
