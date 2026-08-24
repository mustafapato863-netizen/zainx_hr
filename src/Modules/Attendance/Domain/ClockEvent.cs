using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Attendance.Domain;

/// <summary>
/// Immutable raw clock event with full hardware / system provenance.
/// Raw clock events are append-only and never mutated.
/// </summary>
public class ClockEvent
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public ClockType Type { get; private set; }
    public ClockSource Source { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public string? SourceDeviceId { get; private set; }
    public string? CorrelationId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private ClockEvent() { }

    public ClockEvent(
        Guid id,
        TenantId tenantId,
        Guid employmentId,
        ClockType type,
        ClockSource source,
        DateTime capturedAtUtc,
        DateTime receivedAtUtc,
        string? sourceDeviceId = null,
        string? correlationId = null,
        Guid? actorUserId = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id cannot be empty.", nameof(id));
        if (employmentId == Guid.Empty) throw new ArgumentException("EmploymentId cannot be empty.", nameof(employmentId));

        Id = id;
        TenantId = tenantId;
        EmploymentId = employmentId;
        Type = type;
        Source = source;
        CapturedAtUtc = capturedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        SourceDeviceId = sourceDeviceId;
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        ActorUserId = actorUserId;
        Latitude = latitude;
        Longitude = longitude;
    }
}
