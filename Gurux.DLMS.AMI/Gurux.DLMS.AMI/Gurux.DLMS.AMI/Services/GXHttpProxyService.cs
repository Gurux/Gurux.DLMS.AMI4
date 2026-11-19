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

using Gurux.DLMS.AMI.Http;
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

/// <summary>Fetches allowlisted public content using bounded downloads and a credential-free cache.</summary>
public sealed partial class GXHttpProxyService : IDisposable
{
    /// <summary>Publishes metrics for outgoing HTTP proxy operations.</summary>
    private static readonly Meter _meter = new("Gurux.DLMS.AMI.HttpProxy");
    /// <summary>Counts outgoing HTTP proxy requests.</summary>
    private static readonly Counter<long> _requests = _meter.CreateCounter<long>("http_proxy.requests");
    /// <summary>Counts HTTP proxy responses served from the cache.</summary>
    private static readonly Counter<long> _cacheHits = _meter.CreateCounter<long>("http_proxy.cache_hits");
    /// <summary>Counts callers sharing an already running HTTP request.</summary>
    private static readonly Counter<long> _coalesced = _meter.CreateCounter<long>("http_proxy.coalesced_requests");
    /// <summary>Counts failed HTTP proxy operations.</summary>
    private static readonly Counter<long> _errors = _meter.CreateCounter<long>("http_proxy.errors");
    /// <summary>Counts response bytes downloaded by the HTTP proxy.</summary>
    private static readonly Counter<long> _bytes = _meter.CreateCounter<long>("http_proxy.downloaded_bytes", "By");
    /// <summary>Records elapsed HTTP download durations in seconds.</summary>
    private static readonly Histogram<double> _duration = _meter.CreateHistogram<double>("http_proxy.download_duration", "s");
    /// <summary>Creates configured HTTP clients for outgoing requests.</summary>
    private readonly IHttpClientFactory _clients;
    /// <summary>Records operational failures and diagnostic information.</summary>
    private readonly ILogger<GXHttpProxyService>? _logger;
    /// <summary>Provides the clock used for expiration and scheduled operations.</summary>
    private readonly TimeProvider _time;
    /// <summary>Caches frequently reused results and their expiration state.</summary>
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 64 * 1024 * 1024 });
    /// <summary>Synchronizes reads and updates to the in-memory cache.</summary>
    private readonly Lock _cacheLock = new();
    /// <summary>Latest generation number recorded for each resource key.</summary>
    private readonly Dictionary<string, long> _generations = [];
    /// <summary>Coalesced HTTP requests shared by callers of the same resource.</summary>
    private readonly Dictionary<string, Flight> _flights = [];
    /// <summary>Limits concurrent outgoing HTTP downloads.</summary>
    private readonly ConcurrencyLimiter _downloads;
    /// <summary>Limits aggregate buffering capacity for concurrent proxy responses.</summary>
    private readonly ConcurrencyLimiter _buffers;
    /// <summary>Maximum buffer permits reserved for one proxy response.</summary>
    private readonly int _bufferPermits;
    /// <summary>Indicates that disposal has begun and this instance can no longer accept work.</summary>
    private bool _disposed;

    /// <summary>Shares one bounded HTTP download between callers requesting the same resource.</summary>
    /// <param name="timeoutSeconds">Maximum lifetime of the shared outgoing download in seconds.</param>
    private sealed class Flight(int timeoutSeconds)
    {
        /// <summary>Cancellation source bounding the lifetime of the queued operation.</summary>
        internal readonly CancellationTokenSource Cancellation = new(TimeSpan.FromSeconds(timeoutSeconds));
        /// <summary>Shared download task awaited by callers of a coalesced request.</summary>
        internal Task<GXHttpProxyResponse> Task = null!;
        /// <summary>Number of callers currently awaiting the shared operation.</summary>
        internal int Waiters;
        /// <summary>Indicates that this operation no longer accepts additional work.</summary>
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

    /// <summary>Builds the cache-key prefix for the current invalidation generation of a proxy scope.</summary>
    /// <param name="scope">The origin or policy scope used to isolate proxy cache entries.</param>
    /// <returns>The scope prefix including its current cache generation.</returns>
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
            throw new GXHttpException(Properties.Resources.HttpProxyDisabled, System.Net.HttpStatusCode.Forbidden);
        }
        var uri = ValidateTarget(url, settings);
        var origin = Uri.TryCreate(referer, UriKind.Absolute, out var referrer) &&
            referrer.Scheme is "http" or "https" && string.IsNullOrEmpty(referrer.UserInfo)
            ? referrer.GetLeftPart(UriPartial.Authority) + "/" : null;
        // Referer can affect the representation even when the upstream omits Vary.
        var hash = Hash(JsonSerializer.Serialize(settings) + "\n" + uri.AbsoluteUri + "\n" + origin);
        var scope = Hash(cacheScope) + ":";
        _requests.Add(1);
        Flight flight;
        lock (_cacheLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = CachePrefix(scope) + hash;
            if (_cache.TryGetValue<Entry>(key, out var cached) && cached!.Response.ExpiresAt > _time.GetUtcNow())
            {
                _cacheHits.Add(1);
                return Copy(cached.Response);
            }
            if (_flights.TryGetValue(key, out flight!)) {
                _coalesced.Add(1);
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

    /// <summary>Downloads or revalidates a proxy response under concurrency, memory, redirect, content-type, and size limits.</summary>
    /// <param name="key">The lookup key identifying the record or cached value.</param>
    /// <param name="scope">The origin or policy scope used to isolate proxy cache entries.</param>
    /// <param name="uri">The absolute upstream target of the proxy request.</param>
    /// <param name="referer">The optional referring page forwarded to the upstream target.</param>
    /// <param name="settings">The validated proxy limits and allowed origins applied to this request.</param>
    /// <param name="flight">The shared in-flight download and its waiter-linked cancellation state.</param>
    /// <returns>A task containing the downloaded or revalidated proxy response.</returns>
    /// <exception cref="GXHttpException">The target, response, resource budget, or upstream request violates the proxy policy.</exception>
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
                throw new GXHttpException(Properties.Resources.HttpProxyDownloadQueueFull, System.Net.HttpStatusCode.ServiceUnavailable);
            }
            var reservation = (int)(((long)settings.MaxResponseBytes * 3 + GXHttpProxyBuffer.ChunkSize * 2 + 1048575) / 1048576);
            if (reservation > _bufferPermits)
                {
                throw new GXHttpException(Properties.Resources.HttpProxyResponseExceedsBufferBudget, System.Net.HttpStatusCode.ServiceUnavailable);
            }
            using var memory = await _buffers.AcquireAsync(reservation, token);
            if (!memory.IsAcquired) {
                throw new GXHttpException(Properties.Resources.HttpProxyBufferCapacityExhausted, System.Net.HttpStatusCode.ServiceUnavailable);
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
                        throw new GXHttpException(Properties.Resources.HttpProxyInvalidRedirects, System.Net.HttpStatusCode.BadGateway);
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
                    throw new GXHttpException(Properties.Resources.HttpProxyUpstreamUnsuccessful, System.Net.HttpStatusCode.BadGateway);
                }
                var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                if (!IsAllowedContentType(mediaType))
                    {
                    throw new GXHttpException(Properties.Resources.HttpProxyUnsupportedContentType, System.Net.HttpStatusCode.UnsupportedMediaType);
                }
                if (response.Content.Headers.ContentLength > settings.MaxResponseBytes)
                    {
                    throw new GXHttpException(Properties.Resources.HttpProxyResponseTooLarge, System.Net.HttpStatusCode.RequestEntityTooLarge);
                }
                await using var stream = await response.Content.ReadAsStreamAsync(token);
                var body = await GXHttpProxyBuffer.ReadAsync(stream, settings.MaxResponseBytes, token);
                _bytes.Add(body.Length);
                return Store(key, scope, body, response.Content.Headers.ContentType!.ToString(), uri, response, settings,
                    null, received, responseDelay, receivedTimestamp);
            }
            throw new GXHttpException(Properties.Resources.HttpProxyRetrievalFailed, System.Net.HttpStatusCode.BadGateway);
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
            throw new GXHttpException(Properties.Resources.HttpProxyRequestTimedOut, System.Net.HttpStatusCode.GatewayTimeout);
        }
        catch (GXHttpException error)
        {
            RecordFailure((int)(error.StatusCode ?? HttpStatusCode.BadGateway), uri, null);
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or IOException)
        {
            RecordFailure(502, uri, error);
            throw new GXHttpException(Properties.Resources.HttpProxyRetrievalFailed, System.Net.HttpStatusCode.BadGateway);
        }
        finally
        {
            _duration.Record(_time.GetElapsedTime(started).TotalSeconds);
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

    /// <summary>Records a failed proxy request using bounded metadata without exposing the complete target URL.</summary>
    /// <param name="status">The HTTP status code assigned to the failed proxy operation.</param>
    /// <param name="uri">The absolute upstream target of the proxy request.</param>
    /// <param name="error">The optional underlying failure whose type and code may be logged.</param>
    private void RecordFailure(int status, Uri uri, Exception? error)
    {
        _errors.Add(1, new KeyValuePair<string, object?>("status", status));
        // HttpRequestException.Message/ToString can contain the complete target URL.
        // Record only bounded metadata, including the underlying error's type/code.
        _logger?.LogWarning("HTTP proxy request to {Host} failed: status {StatusCode}, error {ErrorType}, code {ErrorCode}",
            uri.IdnHost, status, error?.GetType().Name, error?.HResult);
    }

    /// <summary>Creates a proxy response with an independent copy of the downloaded body.</summary>
    /// <param name="response">Proxy response whose body is copied for the caller.</param>
    /// <returns>A response whose mutable body is independent of the input response.</returns>
    private static GXHttpProxyResponse Copy(GXHttpProxyResponse response) => response with { Body = response.Body.ToArray() };
    /// <summary>Computes the uppercase SHA-256 digest of UTF-8 input text.</summary>
    /// <param name="value">Input value to inspect or transform.</param>
    /// <returns>The uppercase hexadecimal SHA-256 digest.</returns>
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    /// <summary>Validates an absolute HTTP target against the configured origins and public-address restrictions.</summary>
    /// <param name="url">The absolute URL to validate before contacting an upstream server.</param>
    /// <param name="settings">The validated proxy limits and allowed origins applied to this request.</param>
    /// <returns>The validated absolute target URI.</returns>
    /// <exception cref="GXHttpException">The URL or target origin is not permitted.</exception>
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
            throw new GXHttpException(Properties.Resources.HttpProxyTargetNotAllowed, System.Net.HttpStatusCode.Forbidden);
        }
        return uri;
    }

    /// <summary>Determines whether the upstream media type is supported for proxied feeds, data, or raster images.</summary>
    /// <param name="type">The normalized upstream media type, or null when it was not supplied.</param>
    /// <returns>Whether the media type is permitted for proxy responses.</returns>
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