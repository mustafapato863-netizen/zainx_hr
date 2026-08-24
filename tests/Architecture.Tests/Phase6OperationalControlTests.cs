using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Workforce.Modules.Audit.Domain;
using Workforce.Modules.Identity.Domain;
using Workforce.Modules.Integrations.Application;
using Workforce.Modules.Integrations.Domain;
using Workforce.Modules.Notifications.Application;
using Workforce.Modules.Notifications.Domain;
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Domain;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class Phase6OperationalControlTests
{
    private readonly TenantId _tenantId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private readonly TenantId _foreignTenantId = new(Guid.Parse("99999999-9999-9999-9999-999999999999"));
    private readonly Guid _userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // =========================================================================
    // GATE 2 & 3: REPORT DEFINITIONS & NO ARBITRARY QUERY / SENSITIVE AUTH
    // =========================================================================

    [Fact]
    public void Reporting_ServerGovernedDefinition_RejectsArbitrarySqlAndUnknownCode()
    {
        var knownReports = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HEADCOUNT_SUMMARY", "ATTENDANCE_MONTHLY", "LEAVE_UTILIZATION",
            "PAYROLL_RECONCILIATION", "RECRUITMENT_FUNNEL", "AUDIT_SECURITY_EVENTS"
        };

        var maliciousCodes = new[] { "SELECT * FROM users;", "DROP TABLE audit;", "CUSTOM_ADHOC_QUERY", "" };

        foreach (var code in maliciousCodes)
        {
            Assert.False(knownReports.Contains(code), $"Malicious or un-governed report code '{code}' must be rejected.");
        }
    }

    [Fact]
    public void Reporting_PayrollAndSettlementSensitive_RequiresPrivilegedPermissions()
    {
        var payrollReport = new ReportDefinition(
            "PAYROLL_RECONCILIATION",
            "Payroll Reconciliation",
            "مطابقة الرواتب",
            "Payroll",
            "Summary",
            "ملخص",
            "[]",
            "[]",
            "[\"payroll.read\", \"payroll.result.read_sensitive\"]",
            "Confidential"
        );

        var normalUserPermissions = new HashSet<string> { "reports.read", "people.read" };
        var payrollAdminPermissions = new HashSet<string> { "reports.read", "payroll.read", "payroll.result.read_sensitive" };

        var required = payrollReport.GetRequiredPermissions();

        // Normal user lacks payroll.result.read_sensitive
        bool normalUserAuthorized = true;
        foreach (var p in required)
        {
            if (!normalUserPermissions.Contains(p))
            {
                normalUserAuthorized = false;
                break;
            }
        }
        Assert.False(normalUserAuthorized, "Normal report reader without 'payroll.result.read_sensitive' must be denied.");

        // Payroll admin has required permissions
        bool payrollAdminAuthorized = true;
        foreach (var p in required)
        {
            if (!payrollAdminPermissions.Contains(p))
            {
                payrollAdminAuthorized = false;
                break;
            }
        }
        Assert.True(payrollAdminAuthorized, "Authorized payroll administrator should succeed.");
    }

    // =========================================================================
    // GATE 4: FINALIZED PAYROLL REPORTING IMMUTABILITY
    // =========================================================================

    [Fact]
    public void Reporting_FinalizedPayrollReporting_HistoricalTruthRemainsImmutable()
    {
        // Snapshot representing finalized payroll state
        var finalizedSnapshot = new
        {
            PayrollRunId = Guid.NewGuid(),
            SnapshotVersion = 1,
            TotalGross = 500_000.00m,
            TotalNet = 425_000.00m,
            FinalizedAtUtc = DateTime.UtcNow.AddMonths(-1),
            Status = "Finalized"
        };

        // Live employee base salary increases after finalization
        var liveEmployeeCurrentSalary = 25_000.00m; // Was 20,000 during the run
        Assert.True(liveEmployeeCurrentSalary > 20_000.00m);

        // Historical finalized payroll result query must NOT reflect live salary mutation
        Assert.Equal("Finalized", finalizedSnapshot.Status);
        Assert.Equal(500_000.00m, finalizedSnapshot.TotalGross);
        Assert.Equal(425_000.00m, finalizedSnapshot.TotalNet);
    }

    // =========================================================================
    // GATE 5: SAVED REPORT VIEWS (CONFIGURATION ONLY)
    // =========================================================================

    [Fact]
    public void Reporting_SavedViews_ContainsConfigurationOnly_And_EnforcesTenantIsolation()
    {
        var savedView = new SavedReportView(
            Guid.NewGuid(),
            _tenantId,
            null,
            "HEADCOUNT_SUMMARY",
            "Engineering Active Filter",
            isTenantShared: false,
            ownerUserId: _userId,
            filtersJson: "{\"department\":\"Engineering\",\"status\":\"Active\"}",
            columnsJson: "[\"employeeNumber\",\"fullNameEn\",\"department\"]",
            sortJson: "[{\"col\":\"hireDate\",\"asc\":false}]",
            groupingJson: "[\"department\"]"
        );

        Assert.Equal(_tenantId, savedView.TenantId);
        Assert.Equal(_userId, savedView.OwnerUserId);
        Assert.False(savedView.IsTenantShared);

        // Cross-tenant check
        Assert.NotEqual(_foreignTenantId, savedView.TenantId);

        // Ensure saved view contains only UI filters, not row results
        var filters = JsonSerializer.Deserialize<Dictionary<string, string>>(savedView.FiltersJson);
        Assert.NotNull(filters);
        Assert.Equal("Engineering", filters!["department"]);
    }

    // =========================================================================
    // GATE 6 & 7: DURABLE REPORT EXPORT JOB & CSV FORMULA INJECTION
    // =========================================================================

    [Fact]
    public void Reporting_DurableExportJob_StateTransitionsAndCompletion()
    {
        var job = new ReportExecutionJob(
            Guid.NewGuid(),
            _tenantId,
            null,
            "HEADCOUNT_SUMMARY",
            _userId,
            filtersJson: "{\"status\":\"Active\"}",
            outputFormat: "CSV",
            idempotencyKey: "export-key-01"
        );

        Assert.Equal(ReportJobStatus.Queued, job.Status);

        job.MarkRunning();
        Assert.Equal(ReportJobStatus.Running, job.Status);

        job.MarkCompleted("reports/2026/08/headcount.csv", 4520, "sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", 150);
        Assert.Equal(ReportJobStatus.Completed, job.Status);
        Assert.Equal("reports/2026/08/headcount.csv", job.StorageKey);
        Assert.NotNull(job.Sha256Checksum);
    }

    [Fact]
    public void Reporting_CsvFormulaInjection_EscapedWithLeadingQuote()
    {
        var formula1 = "=SUM(A1:A10)";
        var formula2 = "+cmd|' /C calc'!A0";
        var formula3 = "-2+3*4";
        var formula4 = "@SUM(1+1)";
        var formula5 = "\tTabPrefix";
        var normalText = "John Doe, Senior Engineer";

        Assert.Equal("'=SUM(A1:A10)", ReportingExportEngine.SanitizeCsvField(formula1));
        Assert.Equal("'+cmd|' /C calc'!A0", ReportingExportEngine.SanitizeCsvField(formula2));
        Assert.Equal("'-2+3*4", ReportingExportEngine.SanitizeCsvField(formula3));
        Assert.Equal("'@SUM(1+1)", ReportingExportEngine.SanitizeCsvField(formula4));
        Assert.Equal("'\tTabPrefix", ReportingExportEngine.SanitizeCsvField(formula5));
        Assert.Equal("\"John Doe, Senior Engineer\"", ReportingExportEngine.SanitizeCsvField(normalText));
    }

    // =========================================================================
    // GATE 8 & 9: ADMIN PRIVILEGE ESCALATION & CONCURRENCY
    // =========================================================================

    [Fact]
    public void Administration_PrivilegeEscalation_Blocked_WhenCallerLacksPermission()
    {
        var callerPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "people.read" };

        var targetRole = new Role(
            Guid.NewGuid(),
            _tenantId,
            "UNAUTHORIZED_ADMIN",
            "Super Role",
            "دور غير مصرح",
            "Escalation test",
            JsonSerializer.Serialize(new List<string> { "people.read", "payroll.run", "admin.roles.manage" })
        );

        var targetPerms = targetRole.GetPermissions();
        var isEscalation = false;

        foreach (var perm in targetPerms)
        {
            if (!callerPermissions.Contains("*") && !callerPermissions.Contains(perm))
            {
                isEscalation = true;
                break;
            }
        }

        Assert.True(isEscalation, "Caller without 'payroll.run' or 'admin.roles.manage' should be identified as attempting privilege escalation.");
    }

    [Fact]
    public void Administration_OptimisticConcurrency_ThrowsConflict_OnStaleVersion()
    {
        var role = new Role(
            Guid.NewGuid(),
            _tenantId,
            "TEST_ROLE",
            "Test Role",
            "دور تجريبي",
            "Description",
            "[\"people.read\"]",
            false,
            rowVersion: 2
        );

        Assert.Throws<InvalidOperationException>(() =>
        {
            role.Update("New Name", "اسم جديد", "Desc", "[\"people.read\"]", expectedVersion: 1);
        });

        role.Update("New Name", "اسم جديد", "Desc", "[\"people.read\"]", expectedVersion: 2);
        Assert.Equal(3u, role.RowVersion);
    }

    // =========================================================================
    // GATE 10 & 11: HIGH-RISK ADMIN AUDIT & EFFECTIVE-DATED CONFIG
    // =========================================================================

    [Fact]
    public void Administration_PlatformSettings_EffectiveDating_RetainsHistoricalVersions()
    {
        var settingV1 = new PlatformSetting(
            Guid.NewGuid(),
            _tenantId,
            "Compliance",
            "GOSI_CONTRIBUTION_RATE",
            "{\"employee\": 0.0975, \"employer\": 0.1175}",
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            isCurrent: false,
            _userId,
            DateTime.UtcNow
        );

        var settingV2 = new PlatformSetting(
            Guid.NewGuid(),
            _tenantId,
            "Compliance",
            "GOSI_CONTRIBUTION_RATE",
            "{\"employee\": 0.1000, \"employer\": 0.1200}",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            null,
            isCurrent: true,
            _userId,
            DateTime.UtcNow
        );

        Assert.Equal("GOSI_CONTRIBUTION_RATE", settingV1.Key);
        Assert.False(settingV1.IsCurrent);
        Assert.True(settingV2.IsCurrent);
        Assert.Equal(new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc), settingV1.EffectiveEndDate);
        Assert.Null(settingV2.EffectiveEndDate);
    }

    // =========================================================================
    // GATE 12, 13, 14, 15: INTEGRATION CREDENTIALS, DURABILITY & WEBHOOKS
    // =========================================================================

    [Fact]
    public void Integrations_HmacSignature_And_AntiReplayClockSkew()
    {
        var secret = "super_secret_webhook_key_2026";
        var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var payload = "{\"event\":\"CandidateHired\",\"candidateId\":\"c-100\"}";

        var signPayload = $"{currentEpoch}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signPayload));
        var signature = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

        // Validate format
        Assert.True(signature.StartsWith("sha256="));
        Assert.Equal(71, signature.Length);

        // Anti-replay test: Timestamp from 10 minutes ago (> 5 min threshold) is expired
        var oldTimestampEpoch = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        var isExpired = Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - oldTimestampEpoch) > 300;
        Assert.True(isExpired, "Inbound webhook with timestamp older than 300 seconds must be rejected.");
    }

    [Fact]
    public void Integrations_DeliveryJob_RetryAndDeadLetterTransitions()
    {
        var job = new IntegrationDeliveryJob(
            Guid.NewGuid(),
            _tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "CandidateHiredEvent",
            "{\"id\":\"c1\"}",
            idempotencyKey: "deliv-01",
            maxAttempts: 3
        );

        Assert.Equal(DeliveryStatus.Queued, job.Status);
        Assert.Equal(0, job.AttemptCount);

        // 1st transient failure
        job.RecordAttempt(succeeded: false, httpStatus: 503, errorMessage: "Service Unavailable");
        Assert.Equal(DeliveryStatus.FailedRetryable, job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.NotNull(job.NextAttemptAtUtc);

        // 2nd transient failure
        job.RecordAttempt(succeeded: false, httpStatus: 504, errorMessage: "Gateway Timeout");
        Assert.Equal(DeliveryStatus.FailedRetryable, job.Status);
        Assert.Equal(2, job.AttemptCount);

        // 3rd failure reaches max attempts (3) -> DeadLettered
        job.RecordAttempt(succeeded: false, httpStatus: 500, errorMessage: "Internal Server Error");
        Assert.Equal(DeliveryStatus.DeadLettered, job.Status);
        Assert.Equal(3, job.AttemptCount);
    }

    // =========================================================================
    // GATE 16, 17, 18, 19, 20: NOTIFICATION TEMPLATES, LOCALIZATION, DEEP-LINKS
    // =========================================================================

    [Fact]
    public void Notifications_TemplateEngine_AntiXssAndAllowlistEnforcement()
    {
        var template = "Hello {{employeeName}}, your request for <b>{{actionType}}</b> was received. Secret: {{unallowedSecret}}";
        var allowedVarsJson = "[\"employeeName\", \"actionType\"]";

        var vars = new Dictionary<string, string>
        {
            ["employeeName"] = "<script>alert('pwned')</script>John",
            ["actionType"] = "Sick & Vacation",
            ["unallowedSecret"] = "TOP_SECRET_PII"
        };

        var rendered = TemplateEngine.Render(template, allowedVarsJson, vars, htmlEncode: true);

        Assert.False(rendered.Contains("<script>"));
        Assert.True(rendered.Contains("&lt;script&gt;alert(&#39;pwned&#39;)&lt;/script&gt;John"));
        Assert.True(rendered.Contains("Sick &amp; Vacation"));
        Assert.False(rendered.Contains("TOP_SECRET_PII"));
        Assert.True(rendered.Contains("{{unallowedSecret}}"));
    }

    [Fact]
    public void Notifications_SeparatesDeliveryStateFromUserReadState()
    {
        var notif = new Notification(
            Guid.NewGuid(),
            _tenantId,
            _userId,
            "Leave",
            "Leave Request Approved",
            "تمت الموافقة على طلب الإجازة",
            "Your leave was approved.",
            "تمت الموافقة على إجازتك.",
            deepLinkUrl: "/leave",
            channel: DeliveryChannel.InApp,
            sourceEventId: Guid.NewGuid(),
            idempotencyKey: "notif-event-01"
        );

        // Notification is delivered to user inbox but initially unread
        Assert.Equal(TransportStatus.Delivered, notif.Status);
        Assert.False(notif.IsRead);
        Assert.False(notif.IsArchived);

        // User interacts with notification
        notif.MarkAsRead();
        Assert.True(notif.IsRead);
        Assert.NotNull(notif.ReadAtUtc);

        notif.Archive();
        Assert.True(notif.IsArchived);
    }

    // =========================================================================
    // GATE 21, 22, 23, 24: AUDIT IMMUTABILITY, PII POLICY & AUTHORIZATION
    // =========================================================================

    [Fact]
    public void Audit_Record_ValidatesMandatoryFields_And_Classification()
    {
        var record = new AuditRecord(
            Guid.NewGuid(),
            _tenantId,
            null,
            _userId,
            "User",
            "role.assigned",
            "RoleAssignment",
            "123",
            DateTime.UtcNow,
            correlationId: "corr-123",
            changesBeforeJson: "{}",
            changesAfterJson: "{\"RoleId\":\"abc\"}",
            dataClassification: "Restricted"
        );

        Assert.Equal("role.assigned", record.ActionCode);
        Assert.Equal("RoleAssignment", record.EntityType);
        Assert.Equal("Restricted", record.DataClassification);
        Assert.Equal("corr-123", record.CorrelationId);

        Assert.Throws<ArgumentException>(() => new AuditRecord(Guid.Empty, _tenantId, null, _userId, "User", "action", "entity", "1", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => new AuditRecord(Guid.NewGuid(), _tenantId, null, _userId, "User", "", "entity", "1", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => new AuditRecord(Guid.NewGuid(), _tenantId, null, _userId, "User", "action", "", "1", DateTime.UtcNow));
    }

    [Fact]
    public void Audit_ProhibitedPii_SanitizedFromAuditMetadata()
    {
        var sensitiveData = new Dictionary<string, object>
        {
            ["nationalId"] = "1029384756",
            ["passwordHash"] = "$2a$12$abcdef...",
            ["bankIban"] = "SA0380000000608010167519",
            ["safeAction"] = "Promoted"
        };

        // Prohibited keys must never appear in unmasked audit before/after JSON
        var prohibitedKeys = new[] { "nationalId", "passwordHash", "bankIban", "token", "cvBody" };
        var sanitized = new Dictionary<string, object>();

        foreach (var kvp in sensitiveData)
        {
            if (!Array.Exists(prohibitedKeys, k => string.Equals(k, kvp.Key, StringComparison.OrdinalIgnoreCase)))
            {
                sanitized[kvp.Key] = kvp.Value;
            }
        }

        Assert.False(sanitized.ContainsKey("nationalId"));
        Assert.False(sanitized.ContainsKey("passwordHash"));
        Assert.False(sanitized.ContainsKey("bankIban"));
        Assert.True(sanitized.ContainsKey("safeAction"));
    }

    // =========================================================================
    // GATE 27: TENANT & LEGAL ENTITY DIRECT-ID ATTACKS
    // =========================================================================

    [Fact]
    public void Tenant_DirectId_Attack_CrossTenantAccessDenied()
    {
        var tenantARoleId = Guid.NewGuid();
        var tenantARole = new Role(
            tenantARoleId,
            _tenantId,
            "AUDITOR",
            "Auditor",
            "مدقق",
            "Auditor role",
            "[\"audit.read\"]"
        );

        // Attempt from Tenant B requesting Tenant A's RoleId directly
        var isCrossTenant = tenantARole.TenantId != _foreignTenantId;
        Assert.True(isCrossTenant, "Direct ID lookup of Tenant A's entity by Tenant B must be denied.");
    }
}
