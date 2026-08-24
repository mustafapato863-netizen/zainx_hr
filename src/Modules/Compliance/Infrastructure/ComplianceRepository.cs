using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Compliance.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Compliance.Infrastructure;

public interface IComplianceRepository
{
    Task<IReadOnlyList<StatutoryRule>> GetRulesByJurisdictionAsync(Jurisdiction jurisdiction, CancellationToken ct = default);
    Task<StatutoryRuleVersion?> GetActiveRuleVersionAsync(string ruleCode, DateOnly effectiveDate, CancellationToken ct = default);
    Task SaveValidationAsync(ComplianceValidation validation, CancellationToken ct = default);
    Task SeedDefaultEgyptRulesAsync(CancellationToken ct = default);
}

public class ComplianceRepository : IComplianceRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ComplianceRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<StatutoryRule>> GetRulesByJurisdictionAsync(Jurisdiction jurisdiction, CancellationToken ct = default)
    {
        var rules = new List<StatutoryRule>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, jurisdiction, category, code, name_en, name_ar, source_reference_law, is_verified
            FROM compliance.statutory_rules
            WHERE jurisdiction = $1 OR jurisdiction = 99
            ORDER BY code;
        """);
        cmd.Parameters.AddWithValue((int)jurisdiction);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rules.Add(new StatutoryRule(
                reader.GetGuid(0),
                (Jurisdiction)reader.GetInt32(1),
                (RuleCategory)reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7)
            ));
        }

        return rules;
    }

    public async Task<StatutoryRuleVersion?> GetActiveRuleVersionAsync(string ruleCode, DateOnly effectiveDate, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT v.id, v.rule_id, v.version_number, v.effective_from, v.effective_to, v.parameters_json, v.calculation_strategy_name, v.status
            FROM compliance.statutory_rule_versions v
            JOIN compliance.statutory_rules r ON v.rule_id = r.id
            WHERE r.code = $1
              AND v.effective_from <= $2
              AND (v.effective_to IS NULL OR v.effective_to >= $2)
            ORDER BY v.version_number DESC
            LIMIT 1;
        """);
        cmd.Parameters.AddWithValue(ruleCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue(effectiveDate);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var effectiveFrom = reader.GetFieldValue<DateOnly>(3);
            var effectiveTo = reader.IsDBNull(4) ? (DateOnly?)null : reader.GetFieldValue<DateOnly>(4);
            return new StatutoryRuleVersion(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                new EffectivePeriod(effectiveFrom, effectiveTo),
                reader.GetString(5),
                reader.GetString(6),
                (VerificationStatus)reader.GetInt32(7)
            );
        }

        return null;
    }

    public async Task SaveValidationAsync(ComplianceValidation validation, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.compliance_validations (
                id, tenant_id, payroll_run_id, employment_id, rule_version_id, is_passed, severity, message, evaluated_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9);
        """);

        cmd.Parameters.AddWithValue(validation.Id);
        cmd.Parameters.AddWithValue(validation.TenantId.Value);
        cmd.Parameters.AddWithValue(validation.PayrollRunId);
        cmd.Parameters.AddWithValue(validation.EmploymentId);
        cmd.Parameters.AddWithValue(validation.RuleVersionId);
        cmd.Parameters.AddWithValue(validation.IsPassed);
        cmd.Parameters.AddWithValue(validation.Severity);
        cmd.Parameters.AddWithValue(validation.Message);
        cmd.Parameters.AddWithValue(validation.EvaluatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SeedDefaultEgyptRulesAsync(CancellationToken ct = default)
    {
        // Seed default Egypt statutory rules with legal citations
        var taxRuleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var gosiRuleId = Guid.Parse("10000000-0000-0000-0000-000000000002");

        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rules (id, jurisdiction, category, code, name_en, name_ar, source_reference_law, is_verified)
            VALUES 
                ('10000000-0000-0000-0000-000000000001', 1, 1, 'EG_INCOME_TAX', 'Egypt Income Tax', 'ضريبة كسب العمل المصرية', 'Law No. 91 of 2005 as amended by Law No. 30 of 2023', TRUE),
                ('10000000-0000-0000-0000-000000000002', 1, 2, 'EG_SOCIAL_INSURANCE', 'Egypt Social Insurance', 'التأمينات الاجتماعية المصرية', 'Social Insurance and Pensions Law No. 148 of 2019', TRUE)
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO compliance.statutory_rule_versions (id, rule_id, version_number, effective_from, effective_to, parameters_json, calculation_strategy_name, status)
            VALUES
                ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 1, '2024-01-01', NULL, '{"personalExemptionYearly": 20000.00}', 'EgyptProgressiveIncomeTaxStrategy', 1),
                ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 1, '2024-01-01', NULL, '{"employeeRate": 0.11, "employerRate": 0.1875, "minInsuredMonthly": 2000.00, "maxInsuredMonthly": 12600.00}', 'EgyptSocialInsuranceStrategy', 1)
            ON CONFLICT (rule_id, version_number) DO NOTHING;
        """);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
