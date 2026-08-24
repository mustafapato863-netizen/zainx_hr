using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Notifications.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Notifications.Infrastructure;

public record PagedNotificationsResult(
    IReadOnlyList<Notification> Items,
    long TotalCount,
    long UnreadCount,
    int Page,
    int PageSize
);

public interface INotificationsRepository
{
    Task<bool> CreateNotificationAsync(Notification notification, CancellationToken ct = default);
    Task<long> GetUnreadCountAsync(TenantId tenantId, Guid userId, CancellationToken ct = default);
    Task<PagedNotificationsResult> ListNotificationsAsync(TenantId tenantId, Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(TenantId tenantId, Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(TenantId tenantId, Guid userId, CancellationToken ct = default);
    Task<bool> ArchiveAsync(TenantId tenantId, Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(TenantId tenantId, Guid userId, CancellationToken ct = default);
    Task SavePreferenceAsync(NotificationPreference preference, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(TenantId tenantId, CancellationToken ct = default);
    Task<NotificationTemplate?> GetTemplateAsync(TenantId tenantId, string templateCode, string locale, DeliveryChannel channel, CancellationToken ct = default);
    Task SaveTemplateAsync(NotificationTemplate template, CancellationToken ct = default);
}

public class NotificationsRepository : INotificationsRepository
{
    private readonly string _connectionString;

    public NotificationsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> CreateNotificationAsync(Notification notification, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO notifications.notifications (
                id, tenant_id, recipient_user_id, category, title_en, title_ar,
                body_en, body_ar, deep_link_url, channel, status, is_read,
                read_at_utc, is_archived, created_at_utc, source_event_id, idempotency_key
            ) VALUES (
                @id, @tenantId, @recipientUserId, @category, @titleEn, @titleAr,
                @bodyEn, @bodyAr, @deepLinkUrl, @channel, @status, @isRead,
                @readAtUtc, @isArchived, @createdAtUtc, @sourceEventId, @idempotencyKey
            ) ON CONFLICT (tenant_id, idempotency_key) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notification.Id);
        cmd.Parameters.AddWithValue("tenantId", notification.TenantId.Value);
        cmd.Parameters.AddWithValue("recipientUserId", notification.RecipientUserId);
        cmd.Parameters.AddWithValue("category", notification.Category);
        cmd.Parameters.AddWithValue("titleEn", notification.TitleEn);
        cmd.Parameters.AddWithValue("titleAr", notification.TitleAr);
        cmd.Parameters.AddWithValue("bodyEn", notification.BodyEn);
        cmd.Parameters.AddWithValue("bodyAr", notification.BodyAr);
        cmd.Parameters.AddWithValue("deepLinkUrl", (object?)notification.DeepLinkUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("channel", (int)notification.Channel);
        cmd.Parameters.AddWithValue("status", (int)notification.Status);
        cmd.Parameters.AddWithValue("isRead", notification.IsRead);
        cmd.Parameters.AddWithValue("readAtUtc", (object?)notification.ReadAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("isArchived", notification.IsArchived);
        cmd.Parameters.AddWithValue("createdAtUtc", notification.CreatedAtUtc);
        cmd.Parameters.AddWithValue("sourceEventId", (object?)notification.SourceEventId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("idempotencyKey", (object?)notification.IdempotencyKey ?? DBNull.Value);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<long> GetUnreadCountAsync(TenantId tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT COUNT(*) 
            FROM notifications.notifications 
            WHERE tenant_id = @tenantId AND recipient_user_id = @userId AND is_read = FALSE AND is_archived = FALSE;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        return (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
    }

    public async Task<PagedNotificationsResult> ListNotificationsAsync(TenantId tenantId, Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var whereClause = "WHERE tenant_id = @tenantId AND recipient_user_id = @userId AND is_archived = FALSE";
        if (unreadOnly)
        {
            whereClause += " AND is_read = FALSE";
        }

        // Count Total
        var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM notifications.notifications {whereClause};", conn);
        countCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        countCmd.Parameters.AddWithValue("userId", userId);
        var totalCount = (long)(await countCmd.ExecuteScalarAsync(ct) ?? 0L);

        var unreadCount = await GetUnreadCountAsync(tenantId, userId, ct);

        var limit = Math.Clamp(pageSize, 1, 100);
        var p = Math.Max(1, page);
        var offset = (p - 1) * limit;

        var listSql = $@"
            SELECT id, tenant_id, recipient_user_id, category, title_en, title_ar,
                   body_en, body_ar, deep_link_url, channel, status, is_read,
                   read_at_utc, is_archived, created_at_utc, source_event_id, idempotency_key
            FROM notifications.notifications
            {whereClause}
            ORDER BY created_at_utc DESC
            LIMIT {limit} OFFSET {offset};
        ";

        await using var listCmd = new NpgsqlCommand(listSql, conn);
        listCmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        listCmd.Parameters.AddWithValue("userId", userId);

        var items = new List<Notification>();
        await using var reader = await listCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var notif = new Notification(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                (DeliveryChannel)reader.GetInt32(9),
                reader.IsDBNull(15) ? null : reader.GetGuid(15),
                reader.IsDBNull(16) ? null : reader.GetString(16)
            );

            if (reader.GetBoolean(11)) notif.MarkAsRead();
            if (reader.GetBoolean(13)) notif.Archive();

            items.Add(notif);
        }

        return new PagedNotificationsResult(items, totalCount, unreadCount, p, limit);
    }

    public async Task<bool> MarkAsReadAsync(TenantId tenantId, Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE notifications.notifications
            SET is_read = TRUE, read_at_utc = NOW()
            WHERE id = @id AND tenant_id = @tenantId AND recipient_user_id = @userId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notificationId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<int> MarkAllAsReadAsync(TenantId tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE notifications.notifications
            SET is_read = TRUE, read_at_utc = NOW()
            WHERE tenant_id = @tenantId AND recipient_user_id = @userId AND is_read = FALSE;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ArchiveAsync(TenantId tenantId, Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            UPDATE notifications.notifications
            SET is_archived = TRUE
            WHERE id = @id AND tenant_id = @tenantId AND recipient_user_id = @userId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", notificationId);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        var affected = await cmd.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task<IReadOnlyList<NotificationPreference>> GetPreferencesAsync(TenantId tenantId, Guid userId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, user_id, category, allow_email, allow_in_app, allow_push
            FROM notifications.preferences
            WHERE tenant_id = @tenantId AND user_id = @userId;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("userId", userId);

        var list = new List<NotificationPreference>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new NotificationPreference(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetBoolean(5),
                reader.GetBoolean(6)
            ));
        }

        return list;
    }

    public async Task SavePreferenceAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO notifications.preferences (
                id, tenant_id, user_id, category, allow_email, allow_in_app, allow_push
            ) VALUES (
                @id, @tenantId, @userId, @category, @allowEmail, @allowInApp, @allowPush
            ) ON CONFLICT (tenant_id, user_id, category) DO UPDATE
            SET allow_email = EXCLUDED.allow_email,
                allow_in_app = EXCLUDED.allow_in_app,
                allow_push = EXCLUDED.allow_push;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", preference.Id);
        cmd.Parameters.AddWithValue("tenantId", preference.TenantId.Value);
        cmd.Parameters.AddWithValue("userId", preference.UserId);
        cmd.Parameters.AddWithValue("category", preference.Category);
        cmd.Parameters.AddWithValue("allowEmail", preference.AllowEmail);
        cmd.Parameters.AddWithValue("allowInApp", preference.AllowInApp);
        cmd.Parameters.AddWithValue("allowPush", preference.AllowPush);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationTemplate>> ListTemplatesAsync(TenantId tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, template_code, locale, subject, body_template, allowed_variables_json, channel, is_active, version
            FROM notifications.templates
            WHERE tenant_id = @tenantId
            ORDER BY template_code, locale;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);

        var list = new List<NotificationTemplate>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new NotificationTemplate(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                (DeliveryChannel)reader.GetInt32(7),
                reader.GetBoolean(8),
                reader.GetInt32(9)
            ));
        }

        return list;
    }

    public async Task<NotificationTemplate?> GetTemplateAsync(TenantId tenantId, string templateCode, string locale, DeliveryChannel channel, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT id, tenant_id, template_code, locale, subject, body_template, allowed_variables_json, channel, is_active, version
            FROM notifications.templates
            WHERE tenant_id = @tenantId AND template_code = @code AND locale = @locale AND channel = @channel
            LIMIT 1;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("tenantId", tenantId.Value);
        cmd.Parameters.AddWithValue("code", templateCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue("locale", locale.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("channel", (int)channel);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new NotificationTemplate(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                (DeliveryChannel)reader.GetInt32(7),
                reader.GetBoolean(8),
                reader.GetInt32(9)
            );
        }

        return null;
    }

    public async Task SaveTemplateAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            INSERT INTO notifications.templates (
                id, tenant_id, template_code, locale, subject, body_template, allowed_variables_json, channel, is_active, version
            ) VALUES (
                @id, @tenantId, @templateCode, @locale, @subject, @bodyTemplate, @allowedVars::jsonb, @channel, @isActive, @version
            ) ON CONFLICT (tenant_id, template_code, locale, channel) DO UPDATE
            SET subject = EXCLUDED.subject,
                body_template = EXCLUDED.body_template,
                allowed_variables_json = EXCLUDED.allowed_variables_json,
                is_active = EXCLUDED.is_active,
                version = EXCLUDED.version;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("id", template.Id);
        cmd.Parameters.AddWithValue("tenantId", template.TenantId.Value);
        cmd.Parameters.AddWithValue("templateCode", template.TemplateCode);
        cmd.Parameters.AddWithValue("locale", template.Locale);
        cmd.Parameters.AddWithValue("subject", template.Subject);
        cmd.Parameters.AddWithValue("bodyTemplate", template.BodyTemplate);
        cmd.Parameters.AddWithValue("allowedVars", template.AllowedVariablesJson);
        cmd.Parameters.AddWithValue("channel", (int)template.Channel);
        cmd.Parameters.AddWithValue("isActive", template.IsActive);
        cmd.Parameters.AddWithValue("version", template.Version);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
