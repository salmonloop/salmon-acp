using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainTransport = SalmonEgg.Domain.Interfaces.Transport.ITransport;
using SalmonEgg.Domain.Interfaces.Transport;
using NetworkTransport = SalmonEgg.Infrastructure.Network.ITransport;
using SalmonEgg.Infrastructure.Network;

namespace SalmonEgg.Infrastructure.Client;

public sealed class NetworkTransportAdapter : DomainTransport, IDisposable
{
    private readonly NetworkTransport _inner;
    private readonly string _url;
    private readonly List<IDisposable> _subscriptions = new();
    private bool _isConnected;
    private bool _disposed;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public event EventHandler<TransportErrorEventArgs>? ErrorOccurred;

    public bool IsConnected => _isConnected;

    public NetworkTransportAdapter(NetworkTransport inner, string url)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentException("URL cannot be empty", nameof(url)) : url.Trim();

        _subscriptions.Add(_inner.Messages.Subscribe(
            message =>
            {
                if (!string.IsNullOrEmpty(message))
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                }
            },
            ex => RaiseError("Transport message stream error", ex)));

        _subscriptions.Add(_inner.StateChanges.Subscribe(
            state =>
            {
                _isConnected = state == TransportState.Connected;
                if (state == TransportState.Error)
                {
                    RaiseError("Transport entered error state", null);
                }
            },
            ex => RaiseError("Transport state stream error", ex)));
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _inner.ConnectAsync(_url, cancellationToken).ConfigureAwait(false);
            _isConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            RaiseError("Failed to connect transport", ex);
            _isConnected = false;
            return false;
        }
    }

    public async Task<bool> DisconnectAsync()
    {
        try
        {
            await _inner.DisconnectAsync().ConfigureAwait(false);
            _isConnected = false;
            return true;
        }
        catch (Exception ex)
        {
            RaiseError("Failed to disconnect transport", ex);
            return false;
        }
    }

    public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        try
        {
            await _inner.SendAsync(message, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            RaiseError("Failed to send message", ex);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var subscription in _subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch
            {
            }
        }
        _subscriptions.Clear();

        if (_inner is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
            }
        }
    }

    private void RaiseError(string message, Exception? exception)
    {
        ErrorOccurred?.Invoke(this, new TransportErrorEventArgs(message, exception));
    }
}
