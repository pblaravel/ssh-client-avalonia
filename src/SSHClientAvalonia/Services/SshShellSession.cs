using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using SSHClientAvalonia.Models;

namespace SSHClientAvalonia.Services;

public sealed class SshShellSession : IAsyncDisposable
{
    private readonly object _gate = new();
    private SshClient? _client;
    private ShellStream? _shell;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public bool IsConnected => _client?.IsConnected == true && _shell is not null;

    public event Action<string>? OutputReceived;

    public event Action<Exception>? ConnectionFailed;

    public async Task ConnectAsync(SshConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await DisconnectAsync().ConfigureAwait(false);

        var connectionInfo = BuildConnectionInfo(profile);
        var client = new SshClient(connectionInfo);

        await Task.Run(() => client.Connect(), cancellationToken).ConfigureAwait(false);

        var shell = client.CreateShellStream(
            "xterm",
            columns: 120,
            rows: 40,
            width: 800,
            height: 600,
            bufferSize: 8192);

        lock (_gate)
        {
            _client = client;
            _shell = shell;
        }

        _readCts = new CancellationTokenSource();
        var token = _readCts.Token;
        _readTask = Task.Run(() => ReadLoop(shell, token), token);

        client.ErrorOccurred += OnClientError;
    }

    private static ConnectionInfo BuildConnectionInfo(SshConnectionProfile profile)
    {
        var methods = new List<AuthenticationMethod>();

        if (!string.IsNullOrWhiteSpace(profile.PrivateKeyPath) && File.Exists(profile.PrivateKeyPath))
        {
            var keyFiles = new PrivateKeyFile[]
            {
                string.IsNullOrEmpty(profile.PrivateKeyPassphrase)
                    ? new PrivateKeyFile(profile.PrivateKeyPath)
                    : new PrivateKeyFile(profile.PrivateKeyPath, profile.PrivateKeyPassphrase),
            };
            methods.Add(new PrivateKeyAuthenticationMethod(profile.Username, keyFiles));
        }

        if (!string.IsNullOrEmpty(profile.Password))
            methods.Add(new PasswordAuthenticationMethod(profile.Username, profile.Password!));

        if (methods.Count == 0)
            throw new InvalidOperationException("Укажите пароль или путь к SSH-ключу.");

        return new ConnectionInfo(profile.Host, profile.Port, profile.Username, methods.ToArray());
    }

    private void OnClientError(object? sender, ExceptionEventArgs e)
    {
        ConnectionFailed?.Invoke(e.Exception);
    }

    private void ReadLoop(ShellStream shell, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var encoding = Encoding.UTF8;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = shell.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    break;

                var chunk = encoding.GetString(buffer, 0, read);
                OutputReceived?.Invoke(chunk);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        catch (ObjectDisposedException)
        {
            // expected
        }
        catch (Exception ex)
        {
            ConnectionFailed?.Invoke(ex);
        }
    }

    public void SendRaw(string data)
    {
        ShellStream? shell;
        lock (_gate)
            shell = _shell;

        if (shell is null)
            return;

        var bytes = Encoding.UTF8.GetBytes(data);
        shell.Write(bytes, 0, bytes.Length);
        shell.Flush();
    }

    public void SendLine(string line)
    {
        SendRaw(line + "\n");
    }

    public async Task DisconnectAsync()
    {
        Task? readTask;
        CancellationTokenSource? readCts;
        SshClient? client;
        ShellStream? shell;

        lock (_gate)
        {
            readTask = _readTask;
            readCts = _readCts;
            client = _client;
            shell = _shell;
            _readTask = null;
            _readCts = null;
            _shell = null;
            _client = null;
        }

        readCts?.Cancel();

        try
        {
            shell?.Dispose();
        }
        catch
        {
            // ignore
        }

        if (client is not null)
        {
            try
            {
                client.ErrorOccurred -= OnClientError;
                if (client.IsConnected)
                    client.Disconnect();
            }
            catch
            {
                // ignore
            }

            client.Dispose();
        }

        if (readTask is not null)
        {
            try
            {
                await readTask.ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        readCts?.Dispose();
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);
}
