// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Gurux.DLMS.AMI.Client.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gurux.DLMS.AMI.Server.Services;

/// <summary>A proxy request rejected with a safe public error message.</summary>
public sealed class GXHttpProxyException(int statusCode, string message) : Exception(message)
{
    /// <summary>The HTTP status to return to the caller.</summary>
    public int StatusCode { get; } = statusCode;
}

/// <summary>Downloaded public content with its remaining freshness lifetime.</summary>
public sealed record GXHttpProxyResponse(byte[] Body, string ContentType, DateTimeOffset ExpiresAt, string SourceUrl);

/// <summary>Fetches allowlisted public content using bounded downloads and a credential-free cache.</summary>
public sealed partial class GXHttpProxyService : IDisposable
{
    private static readonly Meter Meter = new("Gurux.DLMS.AMI.HttpProxy");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("http_proxy.requests");
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("http_proxy.cache_hits");
    private static readonly Counter<long> Coalesced = Meter.CreateCounter<long>("http_proxy.coalesced_requests");
    private static readonly Counter<long> Errors = Meter.CreateCounter<long>("http_proxy.errors");
    private static readonly Counter<long> Bytes = Meter.CreateCounter<long>("http_proxy.downloaded_bytes", "By");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("http_proxy.download_duration", "s");
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<GXHttpProxyService>? _logger;
    private readonly TimeProvider _time;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64 * 1024 * 1024 });
    private readonly Lock _cacheLock = new();
    private readonly Dictionary<string, long> _generations = [];
    private readonly Dictionary<string, Flight> _flights = [];
    private readonly ConcurrencyLimiter _downloads;
    private readonly ConcurrencyLimiter _buffers;
    private readonly int _bufferPermits;
    private bool _disposed;

    private sealed class Flight(int timeoutSeconds)
    {
        internal readonly CancellationTokenSource Cancellation = new(TimeSpan.FromSeconds(timeoutSeconds));
        internal Task<GXHttpProxyResponse> Task = null!;
        internal int Waiters;
        internal bool Finished;
    }

    /// <summary>Creates a proxy with optional server limits, diagnostics and clock.</summary>
    public GXHttpProxyService(IHttpClientFactory clients, ILogger<GXHttpProxyService>? logger = null,
        TimeProvider? timeProvider = null, IOptions<GXHttpProxyOptions>? options = null)
    {
        _clients = clients;
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
        var limits = options?.Value ?? new GXHttpProxyOptions();
        Validator.ValidateObject(limits, new ValidationContext(limits), true);
        _downloads = new(new ConcurrencyLimiterOptions
        {
            PermitLimit = limits.MaxConcurrentDownloads,
            QueueLimit = limits.MaxQueuedDownloads,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
        // Whole MiB permits keep weighted reservations bounded without tracking individual byte waiters.
        _bufferPermits = (int)(limits.MaxBufferedBytes / (1024 * 1024));
        _buffers = new(new ConcurrencyLimiterOptions
        {
            PermitLimit = _bufferPermits,
            QueueLimit = _bufferPermits * limits.MaxConcurrentDownloads,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    }

    private string CachePrefix(string scope)
    {
        _generations.TryGetValue(scope, out var generation);
        return scope + generation + ":";
    }

    /// <summary>Invalidates one scope, including cache writes from already running downloads.</summary>
    public void ClearCache(string cacheScope)
    {
        var scope = Hash(cacheScope) + ":";
        lock (_cacheLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _generations.TryGetValue(scope, out var generation);
            _generations[scope] = generation + 1;
            foreach (var key in _cache.Keys.OfType<string>().Where(key => key.StartsWith(scope, StringComparison.Ordinal)).ToArray())
                {
                _cache.Remove(key);
            }
        }
    }

    /// <summary>Retrieves an independently owned body after validating settings and each redirect.</summary>
    public async Task<GXHttpProxyResponse> GetAsync(string url, HttpProxySettings settings, string? referer,
        CancellationToken cancellationToken, string cacheScope = "")
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Callers may reuse mutable settings; one operation must keep a stable network policy.
        settings = new HttpProxySettings
        {
            Enabled = settings.Enabled, AllowedOrigins = settings.AllowedOrigins?.ToArray()!,
            CacheTimeoutSeconds = settings.CacheTimeoutSeconds, RequestTimeoutSeconds = settings.RequestTimeoutSeconds,
            MaxResponseBytes = settings.MaxResponseBytes
        };
        settings.Validate();
        if (!settings.Enabled) {
            throw new GXHttpProxyException(403, "HTTP proxy is disabled.");
        }
        var uri = ValidateTarget(url, settings);
        var origin = Uri.TryCreate(referer, UriKind.Absolute, out var referrer) &&
            referrer.Scheme is "http" or "https" && string.IsNullOrEmpty(referrer.UserInfo)
            ? referrer.GetLeftPart(UriPartial.Authority) + "/" : null;
        // Referer can affect the representation even when the upstream omits Vary.
        var hash = Hash(JsonSerializer.Serialize(settings) + "\n" + uri.AbsoluteUri + "\n" + origin);
        var scope = Hash(cacheScope) + ":";
        Requests.Add(1);
        Flight flight;
        lock (_cacheLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = CachePrefix(scope) + hash;
            if (_cache.TryGetValue<Entry>(key, out var cached) && cached!.Response.ExpiresAt > _time.GetUtcNow())
            {
                CacheHits.Add(1);
                return Copy(cached.Response);
            }
            if (_flights.TryGetValue(key, out flight!)) {
                Coalesced.Add(1);
            }
            else
            {
                flight = new(settings.RequestTimeoutSeconds);
                _flights.Add(key, flight);
                flight.Task = DownloadAsync(key, scope, uri, origin, settings, flight);
            }
            ++flight.Waiters;
        }
        try
        {
            return Copy(await flight.Task.WaitAsync(cancellationToken));
        }
        finally
        {
            bool cancel;
            lock (_cacheLock)
            {
                cancel = --flight.Waiters == 0 && !flight.Finished;
                if (cancel)
                {
                    // New callers must not join a download whose last caller cancelled.
                    foreach (var pair in _flights.Where(p => ReferenceEquals(p.Value, flight)).ToArray())
                        {
                        _flights.Remove(pair.Key);
                    }
                }
            }
            if (cancel)
            {
                try { await flight.Cancellation.CancelAsync(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    private async Task<GXHttpProxyResponse> DownloadAsync(string key, string scope, Uri uri, string? referer,
        HttpProxySettings settings, Flight flight)
    {
        // Publish the flight and its first waiter before any synchronous completion or failure.
        await Task.Yield();
        var started = _time.GetTimestamp();
        var token = flight.Cancellation.Token;
        try
        {
            using var slot = await _downloads.AcquireAsync(1, token);
            if (!slot.IsAcquired) {
                throw new GXHttpProxyException(503, "HTTP proxy download queue is full.");
            }
            var reservation = (int)(((long)settings.MaxResponseBytes * 3 + GXHttpProxyBuffer.ChunkSize * 2 + 1048575) / 1048576);
            if (reservation > _bufferPermits)
                {
                throw new GXHttpProxyException(503, "Proxy response limit exceeds the server buffering budget.");
            }
            using var memory = await _buffers.AcquireAsync(reservation, token);
            if (!memory.IsAcquired) {
                throw new GXHttpProxyException(503, "HTTP proxy buffering capacity is exhausted.");
            }
            _cache.TryGetValue<Entry>(key, out var previous);
            using var client = _clients.CreateClient("HttpProxy");
            for (var redirects = 0; redirects <= 3; ++redirects)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd("Gurux.DLMS.AMI/4 (+https://gurux.fi)");
                if (referer != null) {
                    request.Headers.Referrer = new Uri(referer);
                }
                var revalidating = previous != null && uri.AbsoluteUri == previous.Response.SourceUrl;
                if (revalidating)
                {
                    if (EntityTagHeaderValue.TryParse(previous!.ETag, out var tag)) {
                        request.Headers.IfNoneMatch.Add(tag);
                    }
                    request.Headers.IfModifiedSince = previous.LastModified;
                }
                var sent = _time.GetTimestamp();
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                var received = _time.GetUtcNow();
                var receivedTimestamp = _time.GetTimestamp();
                var responseDelay = _time.GetElapsedTime(sent, receivedTimestamp);
                if (response.StatusCode is HttpStatusCode.MovedPermanently or HttpStatusCode.Found or HttpStatusCode.SeeOther or
                    HttpStatusCode.TemporaryRedirect or HttpStatusCode.PermanentRedirect)
                {
                    if (response.Headers.Location == null || redirects == 3)
                        {
                        throw new GXHttpProxyException(502, "Proxy target returned too many or invalid redirects.");
                    }
                    uri = ValidateTarget(new Uri(uri, response.Headers.Location).AbsoluteUri, settings);
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.NotModified && revalidating &&
                    (request.Headers.IfNoneMatch.Count > 0 || request.Headers.IfModifiedSince != null))
                    {
                    return Store(key, scope, previous!.Response.Body, previous.Response.ContentType, uri, response, settings,
                        previous, received, responseDelay, receivedTimestamp);
                }
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("HTTP proxy upstream {Host} returned status {StatusCode}", uri.IdnHost, (int)response.StatusCode);
                    throw new GXHttpProxyException(502, "Proxy target returned an unsuccessful response.");
                }
                var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (!IsAllowedContentType(mediaType))
                    {
                    throw new GXHttpProxyException(415, "Proxy target did not return a feed, data or raster image.");
                }
                if (response.Content.Headers.ContentLength > settings.MaxResponseBytes)
                    {
                    throw new GXHttpProxyException(413, "Proxy response exceeds the configured size limit.");
                }
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                var body = await GXHttpProxyBuffer.ReadAsync(stream, settings.MaxResponseBytes, token);
                Bytes.Add(body.Length);
                return Store(key, scope, body, response.Content.Headers.ContentType!.ToString(), uri, response, settings,
                    null, received, responseDelay, receivedTimestamp);
            }
            throw new GXHttpProxyException(502, "Unable to retrieve proxy target.");
        }
        catch (OperationCanceledException)
        {
            lock (_cacheLock)
            {
                if (_disposed || flight.Waiters == 0) {
                    throw;
                }
            }
            RecordFailure(504, uri, null);
            throw new GXHttpProxyException(504, "Proxy request timed out.");
        }
        catch (GXHttpProxyException error)
        {
            RecordFailure(error.StatusCode, uri, null);
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or IOException)
        {
            RecordFailure(502, uri, error);
            throw new GXHttpProxyException(502, "Unable to retrieve proxy target.");
        }
        finally
        {
            Duration.Record(_time.GetElapsedTime(started).TotalSeconds);
            lock (_cacheLock)
            {
                flight.Finished = true;
                if (_flights.TryGetValue(key, out var current) && ReferenceEquals(current, flight)) {
                    _flights.Remove(key);
                }
            }
            flight.Cancellation.Dispose();
        }
    }

    private void RecordFailure(int status, Uri uri, Exception? error)
    {
        Errors.Add(1, new KeyValuePair<string, object?>("status", status));
        // HttpRequestException.Message/ToString can contain the complete target URL.
        // Record only bounded metadata, including the underlying error's type/code.
        _logger?.LogWarning("HTTP proxy request to {Host} failed: status {StatusCode}, error {ErrorType}, code {ErrorCode}",
            uri.IdnHost, status, error?.GetType().Name, error?.HResult);
    }

    private static GXHttpProxyResponse Copy(GXHttpProxyResponse response) => response with { Body = response.Body.ToArray() };
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static Uri ValidateTarget(string url, HttpProxySettings settings)
    {
        if (url.Length > 8192 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) ||
            !settings.AllowedOrigins.Any(value =>
            {
                var origin = new Uri(value);
                return origin.Scheme == uri.Scheme && origin.IdnHost.Equals(uri.IdnHost, StringComparison.OrdinalIgnoreCase) && origin.Port == uri.Port;
            }) || (IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) && !GXHttpProxyTransport.IsPublicAddress(address)))
            {
            throw new GXHttpProxyException(403, "Proxy target is not allowed.");
        }
        return uri;
    }

    private static bool IsAllowedContentType(string? type) => type is
        "application/rss+xml" or "application/atom+xml" or "application/xml" or "text/xml" or
        "application/json" or "application/feed+json" or "text/plain" or
        "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/avif";

    /// <inheritdoc />
    public void Dispose()
    {
        Flight[] pending;
        lock (_cacheLock)
        {
            if (_disposed) {
                return;
            }
            _disposed = true;
            pending = _flights.Values.ToArray();
        }
        foreach (var flight in pending)
        {
            try { flight.Cancellation.Cancel(); }
            catch (ObjectDisposedException) { }
        }
        _downloads.Dispose();
        _buffers.Dispose();
        _cache.Dispose();
    }
}