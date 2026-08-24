using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Integrations.Domain;

public enum ConnectorType
{
    WebhookOutbound = 1,
    GenericHttp = 2,
    FileExport = 3
}

public enum IntegrationDirection
{
    Outbound = 1,
    Inbound = 2
}

public enum IntegrationAuthType
{
    None = 1,
    ApiKey = 2,
    HmacSignature = 3,
    BearerToken = 4
}

public enum DeliveryStatus
{
    Queued = 1,
    Sending = 2,
    Delivered = 3,
    FailedRetryable = 4,
    FailedPermanent = 5,
    DeadLettered = 6,
    Cancelled = 7
}

public class IntegrationConnector
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Code { get; private set; }
    public string NameEn { get; private set; }
    public string NameAr { get; private set; }
    public ConnectorType ConnectorType { get; private set; }
    public IntegrationDirection Direction { get; private set; }
    public string EndpointUrl { get; private set; }
    public IntegrationAuthType AuthType { get; private set; }
    public string? EncryptedCredentials { get; private set; }
    public int CredentialsKeyVersion { get; private set; }
    public bool IsActive { get; private set; }
    public string EventSubscriptionsJson { get; private set; }
    public string ConfigJson { get; private set; }
    public uint RowVersion { get; private set; }

    private IntegrationConnector()
    {
        Code = string.Empty;
        NameEn = string.Empty;
        NameAr = string.Empty;
        EndpointUrl = string.Empty;
        EventSubscriptionsJson = "[]";
        ConfigJson = "{}";
    }

    public IntegrationConnector(
        Guid id,
        TenantId tenantId,
        string code,
        string nameEn,
        string nameAr,
        ConnectorType connectorType,
        IntegrationDirection direction,
        string endpointUrl,
        IntegrationAuthType authType,
        string? encryptedCredentials = null,
        int credentialsKeyVersion = 1,
        bool isActive = true,
        string eventSubscriptionsJson = "[]",
        string configJson = "{}",
        uint rowVersion = 1)
    {
        if (id == Guid.Empty) throw new ArgumentException("Connector ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(nameEn)) throw new ArgumentException("Name (EN) cannot be empty.", nameof(nameEn));

        Id = id;
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? nameEn.Trim() : nameAr.Trim();
        ConnectorType = connectorType;
        Direction = direction;
        EndpointUrl = endpointUrl?.Trim() ?? string.Empty;
        AuthType = authType;
        EncryptedCredentials = encryptedCredentials;
        CredentialsKeyVersion = credentialsKeyVersion;
        IsActive = isActive;
        EventSubscriptionsJson = string.IsNullOrWhiteSpace(eventSubscriptionsJson) ? "[]" : eventSubscriptionsJson.Trim();
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
        RowVersion = rowVersion;
    }

    public void Update(
        string nameEn,
        string nameAr,
        string endpointUrl,
        IntegrationAuthType authType,
        string? encryptedCredentials,
        bool isActive,
        string eventSubscriptionsJson,
        string configJson,
        uint expectedRowVersion)
    {
        if (RowVersion != expectedRowVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict on Connector '{Code}'. Expected row version {expectedRowVersion} but found {RowVersion}.");
        }

        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? nameEn.Trim() : nameAr.Trim();
        EndpointUrl = endpointUrl?.Trim() ?? string.Empty;
        AuthType = authType;
        if (encryptedCredentials != null)
        {
            EncryptedCredentials = encryptedCredentials;
            CredentialsKeyVersion++;
        }
        IsActive = isActive;
        EventSubscriptionsJson = string.IsNullOrWhiteSpace(eventSubscriptionsJson) ? "[]" : eventSubscriptionsJson.Trim();
        ConfigJson = string.IsNullOrWhiteSpace(configJson) ? "{}" : configJson.Trim();
        RowVersion++;
    }
}

public class IntegrationDeliveryJob
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid ConnectorId { get; private set; }
    public Guid EventId { get; private set; }
    public string EventType { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime? NextAttemptAtUtc { get; private set; }
    public DateTime? LastAttemptAtUtc { get; private set; }
    public int? LastHttpStatus { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string PayloadJson { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private IntegrationDeliveryJob()
    {
        EventType = string.Empty;
        PayloadJson = "{}";
        IdempotencyKey = string.Empty;
    }

    public IntegrationDeliveryJob(
        Guid id,
        TenantId tenantId,
        Guid connectorId,
        Guid eventId,
        string eventType,
        string payloadJson,
        string idempotencyKey,
        int maxAttempts = 5)
    {
        Id = id;
        TenantId = tenantId;
        ConnectorId = connectorId;
        EventId = eventId;
        EventType = eventType.Trim();
        Status = DeliveryStatus.Queued;
        AttemptCount = 0;
        MaxAttempts = maxAttempts;
        NextAttemptAtUtc = DateTime.UtcNow;
        PayloadJson = payloadJson;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void RecordAttempt(bool succeeded, int httpStatus, string? errorMessage = null)
    {
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
        LastHttpStatus = httpStatus;
        LastErrorMessage = errorMessage;

        if (succeeded)
        {
            Status = DeliveryStatus.Delivered;
            NextAttemptAtUtc = null;
        }
        else if (httpStatus >= 400 && httpStatus < 500 && httpStatus != 429 && httpStatus != 408)
        {
            // Permanent client error (400, 401, 403, 404, etc.) -> Non-retryable
            Status = DeliveryStatus.FailedPermanent;
            NextAttemptAtUtc = null;
        }
        else if (AttemptCount >= MaxAttempts)
        {
            Status = DeliveryStatus.DeadLettered;
            NextAttemptAtUtc = null;
        }
        else
        {
            Status = DeliveryStatus.FailedRetryable;
            // Exponential backoff: 5s * 2^(attempts-1)
            var backoffSeconds = Math.Min(3600, 5 * Math.Pow(2, AttemptCount - 1));
            NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);
        }
    }

    public void ForceRetry()
    {
        Status = DeliveryStatus.Queued;
        NextAttemptAtUtc = DateTime.UtcNow;
    }
}

public class IntegrationInboxMessage
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string ProviderCode { get; private set; }
    public string ExternalMessageId { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string Status { get; private set; }

    public IntegrationInboxMessage(
        Guid id,
        TenantId tenantId,
        string providerCode,
        string externalMessageId,
        string payloadJson)
    {
        Id = id;
        TenantId = tenantId;
        ProviderCode = providerCode.Trim().ToUpperInvariant();
        ExternalMessageId = externalMessageId.Trim();
        PayloadJson = payloadJson;
        ReceivedAtUtc = DateTime.UtcNow;
        Status = "Received";
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        Status = "Processed";
    }
}
