using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Workforce.Modules.Ai.Application.Services;

/// <summary>
/// Closeout Gate 8: per-user/per-tenant fixed-window AI request limiter.
/// Exceeding the limit raises AiRequestLimitExceededException which the API
/// surfaces as HTTP 429 with a safe body. Core product routes are unaffected.
/// </summary>
public sealed class AiRateLimiter
{
    private readonly int _maxRequestsPerMinute;
    private readonly ConcurrentDictionary<string, WindowCounters> _windows = new();

    public AiRateLimiter(int maxRequestsPerMinute)
    {
        if (maxRequestsPerMinute < 1) throw new ArgumentOutOfRangeException(nameof(maxRequestsPerMinute));
        _maxRequestsPerMinute = maxRequestsPerMinute;
    }

    public int MaxRequestsPerMinute => _maxRequestsPerMinute;

    /// <exception cref="AiRequestLimitExceededException">When the caller exceeds the configured window limit.</exception>
    public void EnsureWithinLimit(Guid tenantId, Guid userId)
    {
        var key = $"{tenantId:N}:{userId:N}";
        var nowMinute = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;

        var counters = _windows.GetOrAdd(key, _ => new WindowCounters());
        lock (counters)
        {
            if (counters.WindowMinute != nowMinute)
            {
                counters.WindowMinute = nowMinute;
                counters.Count = 0;
            }

            counters.Count++;
            if (counters.Count > _maxRequestsPerMinute)
            {
                throw new AiRequestLimitExceededException(
                    $"AI request limit of {_maxRequestsPerMinute} per minute exceeded for this user/tenant context.");
            }
        }

        // Opportunistic cleanup to keep memory bounded.
        if (_windows.Count > 10_000)
        {
            foreach (var kv in _windows)
            {
                if (kv.Value.WindowMinute < nowMinute - 2)
                {
                    _windows.TryRemove(kv.Key, out _);
                }
            }
        }
    }

    private sealed class WindowCounters
    {
        public long WindowMinute { get; set; }
        public int Count { get; set; }
    }
}

public sealed class AiRequestLimitExceededException : Exception
{
    public AiRequestLimitExceededException(string message) : base(message) { }
}
