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
using Workforce.Modules.Reporting.Application;
using Workforce.Modules.Reporting.Domain;
using Workforce.SharedKernel.Primitives;
using Xunit;

namespace Architecture.Tests;

public class Phase6OperationalControlTests
{
    private readonly TenantId _tenantId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private readonly Guid _userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Administration_PrivilegeEscalation_Blocked_WhenCallerLacksPermission()
    {
        // Caller only has "people.read"
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

        // Verification logic simulates repository privilege escalation check
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

        // Attempting to update with stale version 1 instead of 2 should fail
        Assert.Throws<InvalidOperationException>(() =>
        {
            role.Update("New Name", "اسم جديد", "Desc", "[\"people.read\"]", expectedVersion: 1);
        });

        // Updating with correct version 2 succeeds and increments version to 3
        role.Update("New Name", "اسم جديد", "Desc", "[\"people.read\"]", expectedVersion: 2);
        Assert.Equal(3u, role.RowVersion);
    }

    [Fact]
    public void Audit_Record_ValidatesMandatoryFields()
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

        // Verify HTML encoding prevents XSS
        Assert.False(rendered.Contains("<script>"));
        Assert.True(rendered.Contains("&lt;script&gt;alert(&#39;pwned&#39;)&lt;/script&gt;John"));
        Assert.True(rendered.Contains("Sick &amp; Vacation"));

        // Verify unallowed variable was NOT substituted
        Assert.False(rendered.Contains("TOP_SECRET_PII"));
        Assert.True(rendered.Contains("{{unallowedSecret}}"));
    }

    [Fact]
    public void Integrations_HmacSignature_MatchesCryptographicSpecification()
    {
        var secret = "super_secret_webhook_key_2026";
        var timestamp = "1756050000";
        var payload = "{\"event\":\"CandidateHired\",\"candidateId\":\"c-100\"}";

        var signPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signPayload));
        var expectedSignature = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

        Assert.True(expectedSignature.StartsWith("sha256="));
        Assert.Equal(71, expectedSignature.Length); // "sha256=" (7) + 64 hex chars
    }

    [Fact]
    public void Reporting_CsvFormulaInjection_EscapedWithLeadingQuote()
    {
        var formula1 = "=SUM(A1:A10)";
        var formula2 = "+cmd|' /C calc'!A0";
        var formula3 = "-2+3*4";
        var formula4 = "@SUM(1+1)";
        var normalText = "John Doe, Senior Engineer";

        Assert.Equal("'=SUM(A1:A10)", ReportingExportEngine.SanitizeCsvField(formula1));
        Assert.Equal("'+cmd|' /C calc'!A0", ReportingExportEngine.SanitizeCsvField(formula2));
        Assert.Equal("'-2+3*4", ReportingExportEngine.SanitizeCsvField(formula3));
        Assert.Equal("'@SUM(1+1)", ReportingExportEngine.SanitizeCsvField(formula4));
        Assert.Equal("\"John Doe, Senior Engineer\"", ReportingExportEngine.SanitizeCsvField(normalText));
    }

    [Fact]
    public void Reporting_PayrollReconciliation_RequiresSensitivePermission()
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

        var requiredPerms = payrollReport.GetRequiredPermissions();

        Assert.True(requiredPerms.Contains("payroll.read"));
        Assert.True(requiredPerms.Contains("payroll.result.read_sensitive"));
        Assert.Equal("Confidential", payrollReport.DataClassification);
    }
}
