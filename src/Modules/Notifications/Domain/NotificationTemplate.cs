using System;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Notifications.Domain;

public enum DeliveryChannel
{
    InApp = 1,
    Email = 2,
    Push = 3
}

public enum TransportStatus
{
    Queued = 1,
    Sending = 2,
    Delivered = 3,
    FailedRetryable = 4,
    FailedPermanent = 5,
    Cancelled = 6
}

public class NotificationTemplate
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string TemplateCode { get; private set; }
    public string Locale { get; private set; }
    public string Subject { get; private set; }
    public string BodyTemplate { get; private set; }
    public string AllowedVariablesJson { get; private set; }
    public DeliveryChannel Channel { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    private NotificationTemplate()
    {
        TemplateCode = string.Empty;
        Locale = "en";
        Subject = string.Empty;
        BodyTemplate = string.Empty;
        AllowedVariablesJson = "[]";
    }

    public NotificationTemplate(
        Guid id,
        TenantId tenantId,
        string templateCode,
        string locale,
        string subject,
        string bodyTemplate,
        string allowedVariablesJson,
        DeliveryChannel channel,
        bool isActive = true,
        int version = 1)
    {
        if (id == Guid.Empty) throw new ArgumentException("Template ID cannot be empty.", nameof(id));
        if (string.IsNullOrWhiteSpace(templateCode)) throw new ArgumentException("Template code cannot be empty.", nameof(templateCode));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("Body template cannot be empty.", nameof(bodyTemplate));

        Id = id;
        TenantId = tenantId;
        TemplateCode = templateCode.Trim().ToUpperInvariant();
        Locale = string.IsNullOrWhiteSpace(locale) ? "en" : locale.Trim().ToLowerInvariant();
        Subject = subject.Trim();
        BodyTemplate = bodyTemplate;
        AllowedVariablesJson = string.IsNullOrWhiteSpace(allowedVariablesJson) ? "[]" : allowedVariablesJson.Trim();
        Channel = channel;
        IsActive = isActive;
        Version = version;
    }

    public void Update(string subject, string bodyTemplate, string allowedVariablesJson, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject cannot be empty.", nameof(subject));
        if (string.IsNullOrWhiteSpace(bodyTemplate)) throw new ArgumentException("Body template cannot be empty.", nameof(bodyTemplate));

        Subject = subject.Trim();
        BodyTemplate = bodyTemplate;
        AllowedVariablesJson = string.IsNullOrWhiteSpace(allowedVariablesJson) ? "[]" : allowedVariablesJson.Trim();
        IsActive = isActive;
        Version++;
    }
}

public class Notification
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Category { get; private set; }
    public string TitleEn { get; private set; }
    public string TitleAr { get; private set; }
    public string BodyEn { get; private set; }
    public string BodyAr { get; private set; }
    public string? DeepLinkUrl { get; private set; }
    public DeliveryChannel Channel { get; private set; }
    public TransportStatus Status { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? SourceEventId { get; private set; }
    public string? IdempotencyKey { get; private set; }

    private Notification()
    {
        Category = "General";
        TitleEn = string.Empty;
        TitleAr = string.Empty;
        BodyEn = string.Empty;
        BodyAr = string.Empty;
    }

    public Notification(
        Guid id,
        TenantId tenantId,
        Guid recipientUserId,
        string category,
        string titleEn,
        string titleAr,
        string bodyEn,
        string bodyAr,
        string? deepLinkUrl = null,
        DeliveryChannel channel = DeliveryChannel.InApp,
        Guid? sourceEventId = null,
        string? idempotencyKey = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Notification ID cannot be empty.", nameof(id));
        if (recipientUserId == Guid.Empty) throw new ArgumentException("Recipient user ID cannot be empty.", nameof(recipientUserId));
        if (string.IsNullOrWhiteSpace(titleEn)) throw new ArgumentException("Title (EN) cannot be empty.", nameof(titleEn));

        Id = id;
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        TitleEn = titleEn.Trim();
        TitleAr = string.IsNullOrWhiteSpace(titleAr) ? titleEn.Trim() : titleAr.Trim();
        BodyEn = bodyEn ?? string.Empty;
        BodyAr = string.IsNullOrWhiteSpace(bodyAr) ? (bodyEn ?? string.Empty) : bodyAr.Trim();
        DeepLinkUrl = deepLinkUrl;
        Channel = channel;
        Status = TransportStatus.Delivered;
        IsRead = false;
        IsArchived = false;
        CreatedAtUtc = DateTime.UtcNow;
        SourceEventId = sourceEventId;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAtUtc = DateTime.UtcNow;
        }
    }

    public void Archive()
    {
        IsArchived = true;
    }
}

public class NotificationPreference
{
    public Guid Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Category { get; private set; }
    public bool AllowEmail { get; private set; }
    public bool AllowInApp { get; private set; }
    public bool AllowPush { get; private set; }

    private NotificationPreference()
    {
        Category = "General";
    }

    public NotificationPreference(
        Guid id,
        TenantId tenantId,
        Guid userId,
        string category,
        bool allowEmail = true,
        bool allowInApp = true,
        bool allowPush = false)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Category = category.Trim();
        AllowEmail = allowEmail;
        AllowInApp = allowInApp;
        AllowPush = allowPush;
    }

    public void Update(bool allowEmail, bool allowInApp, bool allowPush)
    {
        AllowEmail = allowEmail;
        AllowInApp = allowInApp;
        AllowPush = allowPush;
    }
}
