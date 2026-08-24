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
    Task<StatutoryRuleVersion?> GetActiveRuleVersionForPeriodAsync(string ruleCode, DateOnly periodStart, DateOnly periodEnd, DateOnly paymentDate, CancellationToken ct = default);
    Task<StatutoryRuleVersion?> GetActiveRuleVersionForEntitlementPeriodAsync(string ruleCode, DateOnly entitlementStart, DateOnly entitlementEnd, CancellationToken ct = default);
    Task<IReadOnlyList<StatutoryRuleVersion>> GetVersionsByRuleCodeAsync(string ruleCode, CancellationToken ct = default);
    Task CreateRuleAsync(StatutoryRule rule, CancellationToken ct = default);
    Task CreateRuleVersionAsync(StatutoryRuleVersion version, CancellationToken ct = default);
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
            SELECT id, jurisdiction, category, code, name_en, name_ar, source_reference_law, applicability_basis, is_verified
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
                (StatutoryApplicabilityBasis)reader.GetInt32(7),
                reader.GetBoolean(8)
            ));
        }

        return rules;
    }

    public async Task<IReadOnlyList<StatutoryRuleVersion>> GetVersionsByRuleCodeAsync(string ruleCode, CancellationToken ct = default)
    {
        var list = new List<StatutoryRuleVersion>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT v.id, v.rule_id, v.version_number, v.effective_from, v.effective_to, v.parameters_json, v.calculation_strategy_name, v.status
            FROM compliance.statutory_rule_versions v
            JOIN compliance.statutory_rules r ON v.rule_id = r.id
            WHERE r.code = $1
            ORDER BY v.version_number ASC;
        """);
        cmd.Parameters.AddWithValue(ruleCode.Trim().ToUpperInvariant());

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var effectiveFrom = reader.GetFieldValue<DateOnly>(3);
            var effectiveTo = reader.IsDBNull(4) ? (DateOnly?)null : reader.GetFieldValue<DateOnly>(4);
            list.Add(new StatutoryRuleVersion(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetInt32(2),
                new EffectivePeriod(effectiveFrom, effectiveTo),
                reader.GetString(5),
                reader.GetString(6),
                (VerificationStatus)reader.GetInt32(7)
            ));
        }

        return list;
    }

    public async Task<StatutoryRuleVersion?> GetActiveRuleVersionForPeriodAsync(
        string ruleCode,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly paymentDate,
        CancellationToken ct = default)
    {
        // 1. Resolve the rule's legally approved applicability basis
        var basis = StatutoryApplicabilityBasis.PayrollPeriod;
        await using (var basisCmd = _dataSource.CreateCommand("""
            SELECT applicability_basis
            FROM compliance.statutory_rules
            WHERE code = $1;
        """))
        {
            basisCmd.Parameters.AddWithValue(ruleCode.Trim().ToUpperInvariant());
            var res = await basisCmd.ExecuteScalarAsync(ct);
            if (res != null && res != DBNull.Value)
            {
                basis = (StatutoryApplicabilityBasis)Convert.ToInt32(res);
            }
        }

        // 2. Select effective evaluation date based on applicability basis
        var effectiveDate = basis switch
        {
            StatutoryApplicabilityBasis.PayrollPeriod => periodEnd,
            StatutoryApplicabilityBasis.PayrollTaxPeriod => periodEnd,
            StatutoryApplicabilityBasis.PaymentDate => paymentDate,
            StatutoryApplicabilityBasis.EffectiveBusinessDate => paymentDate,
            _ => periodEnd
        };

        // 3. Resolve active rule version
        return await GetActiveRuleVersionAsync(ruleCode, effectiveDate, ct);
    }

    public async Task<StatutoryRuleVersion?> GetActiveRuleVersionForEntitlementPeriodAsync(
        string ruleCode,
        DateOnly entitlementStart,
        DateOnly entitlementEnd,
        CancellationToken ct = default)
    {
        // For arrears/frozen wages, resolve the statutory rule active on the entitlement period end
        return await GetActiveRuleVersionAsync(ruleCode, entitlementEnd, ct);
    }

    public async Task<StatutoryRuleVersion?> GetActiveRuleVersionAsync(string ruleCode, DateOnly effectiveDate, CancellationToken ct = default)
    {
        var matchingVersions = new List<StatutoryRuleVersion>();

        // Query all versions that temporally cover the effectiveDate
        await using var cmd = _dataSource.CreateCommand("""
            SELECT v.id, v.rule_id, v.version_number, v.effective_from, v.effective_to, v.parameters_json, v.calculation_strategy_name, v.status
            FROM compliance.statutory_rule_versions v
            JOIN compliance.statutory_rules r ON v.rule_id = r.id
            WHERE r.code = $1
              AND v.effective_from <= $2
              AND (v.effective_to IS NULL OR v.effective_to >= $2)
            ORDER BY v.version_number DESC;
        """);
        cmd.Parameters.AddWithValue(ruleCode.Trim().ToUpperInvariant());
        cmd.Parameters.AddWithValue(effectiveDate);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var effectiveFrom = reader.GetFieldValue<DateOnly>(3);
                var effectiveTo = reader.IsDBNull(4) ? (DateOnly?)null : reader.GetFieldValue<DateOnly>(4);
                matchingVersions.Add(new StatutoryRuleVersion(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetInt32(2),
                    new EffectivePeriod(effectiveFrom, effectiveTo),
                    reader.GetString(5),
                    reader.GetString(6),
                    (VerificationStatus)reader.GetInt32(7)
                ));
            }
        }

        if (matchingVersions.Count == 0)
        {
            return null;
        }

        if (matchingVersions.Count > 1)
        {
            throw new InvalidOperationException(
                $"BLOCKING COMPLIANCE EXCEPTION: Multiple ({matchingVersions.Count}) overlapping statutory rule versions found for '{ruleCode}' on effective date {effectiveDate:yyyy-MM-dd}. Overlapping version definitions violate temporal integrity.");
        }

        return matchingVersions[0];
    }

    public async Task CreateRuleAsync(StatutoryRule rule, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rules (
                id, jurisdiction, category, code, name_en, name_ar, source_reference_law, applicability_basis, is_verified, created_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
            ON CONFLICT (code) DO UPDATE
            SET name_en = EXCLUDED.name_en,
                name_ar = EXCLUDED.name_ar,
                source_reference_law = EXCLUDED.source_reference_law,
                applicability_basis = EXCLUDED.applicability_basis,
                is_verified = EXCLUDED.is_verified;
        """);

        cmd.Parameters.AddWithValue(rule.Id);
        cmd.Parameters.AddWithValue((int)rule.Jurisdiction);
        cmd.Parameters.AddWithValue((int)rule.Category);
        cmd.Parameters.AddWithValue(rule.Code);
        cmd.Parameters.AddWithValue(rule.NameEn);
        cmd.Parameters.AddWithValue(rule.NameAr);
        cmd.Parameters.AddWithValue(rule.SourceReferenceLaw);
        cmd.Parameters.AddWithValue((int)rule.ApplicabilityBasis);
        cmd.Parameters.AddWithValue(rule.IsVerified);
        cmd.Parameters.AddWithValue(rule.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateRuleVersionAsync(StatutoryRuleVersion version, CancellationToken ct = default)
    {
        // First-line friendly domain/application validation check
        if (version.Status == VerificationStatus.Verified)
        {
            await using (var checkCmd = _dataSource.CreateCommand("""
                SELECT id, version_number, effective_from, effective_to
                FROM compliance.statutory_rule_versions
                WHERE rule_id = $1 AND id != $2 AND status = 1;
            """))
            {
                checkCmd.Parameters.AddWithValue(version.RuleId);
                checkCmd.Parameters.AddWithValue(version.Id);

                await using var reader = await checkCmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    var existingVerNum = reader.GetInt32(1);
                    var effFrom = reader.GetFieldValue<DateOnly>(2);
                    var effTo = reader.IsDBNull(3) ? (DateOnly?)null : reader.GetFieldValue<DateOnly>(3);
                    var existingPeriod = new EffectivePeriod(effFrom, effTo);

                    if (existingVerNum == version.VersionNumber)
                    {
                        throw new InvalidOperationException($"Duplicate version number {version.VersionNumber} for rule ID '{version.RuleId}'.");
                    }

                    if (existingPeriod.OverlapsWith(version.EffectivePeriod))
                    {
                        throw new InvalidOperationException(
                            $"Temporal violation: New version {version.VersionNumber} ({version.EffectivePeriod.EffectiveFrom:yyyy-MM-dd}..{version.EffectivePeriod.EffectiveTo:yyyy-MM-dd}) " +
                            $"overlaps with existing version {existingVerNum} ({effFrom:yyyy-MM-dd}..{effTo:yyyy-MM-dd}).");
                    }
                }
            }
        }

        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rule_versions (
                id, rule_id, version_number, effective_from, effective_to, parameters_json, calculation_strategy_name, status, created_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6::jsonb, $7, $8, $9)
            ON CONFLICT (rule_id, version_number) DO UPDATE
            SET effective_from = EXCLUDED.effective_from,
                effective_to = EXCLUDED.effective_to,
                parameters_json = EXCLUDED.parameters_json,
                calculation_strategy_name = EXCLUDED.calculation_strategy_name,
                status = EXCLUDED.status;
        """);

        cmd.Parameters.AddWithValue(version.Id);
        cmd.Parameters.AddWithValue(version.RuleId);
        cmd.Parameters.AddWithValue(version.VersionNumber);
        cmd.Parameters.AddWithValue(version.EffectivePeriod.EffectiveFrom);
        cmd.Parameters.AddWithValue((object?)version.EffectivePeriod.EffectiveTo ?? DBNull.Value);
        cmd.Parameters.AddWithValue(version.ParametersJson);
        cmd.Parameters.AddWithValue(version.CalculationStrategyName);
        cmd.Parameters.AddWithValue((int)version.Status);
        cmd.Parameters.AddWithValue(version.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
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
        // 1. Seed Rule Headers with Approved Legal Applicability Basis
        // EG_INCOME_TAX: PaymentDate (Tax withholding occurs on disbursement date)
        // EG_SOCIAL_INSURANCE: ContributionPeriod (Insurance bounds apply to working month)
        await using (var cmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rules (id, jurisdiction, category, code, name_en, name_ar, source_reference_law, applicability_basis, is_verified)
            VALUES 
                ('10000000-0000-0000-0000-000000000001', 1, 1, 'EG_INCOME_TAX', 'Egypt Income Tax', 'ضريبة كسب العمل المصرية', 'Income Tax Law No. 91 of 2005 as amended by Law No. 30 of 2023 and Law No. 7 of 2024', 2, TRUE),
                ('10000000-0000-0000-0000-000000000002', 1, 2, 'EG_SOCIAL_INSURANCE', 'Egypt Social Insurance', 'التأمينات الاجتماعية المصرية', 'Social Insurance and Pensions Law No. 148 of 2019 & NOSI Decrees', 1, TRUE)
            ON CONFLICT (code) DO UPDATE
            SET name_en = EXCLUDED.name_en,
                source_reference_law = EXCLUDED.source_reference_law,
                applicability_basis = EXCLUDED.applicability_basis,
                is_verified = EXCLUDED.is_verified;
        """))
        {
            await cmd.ExecuteNonQueryAsync(ct);
        }

        // 2. Seed Effective-Dated Versions for Social Insurance (2024, 2025, 2026)
        await using (var gosiCmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rule_versions (id, rule_id, version_number, effective_from, effective_to, parameters_json, calculation_strategy_name, status)
            VALUES
                ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 1, '2024-01-01', '2024-12-31', 
                 '{"sourceReference":"Law No. 148 of 2019 & NOSI Decree 2024","employeeRate":0.11,"employerRate":0.1875,"minInsuredMonthly":2000.00,"maxInsuredMonthly":12600.00}', 
                 'EgyptSocialInsuranceStrategy', 1),
                ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000002', 2, '2025-01-01', '2025-12-31', 
                 '{"sourceReference":"Law No. 148 of 2019 & NOSI Decree 2025","employeeRate":0.11,"employerRate":0.1875,"minInsuredMonthly":2300.00,"maxInsuredMonthly":14500.00}', 
                 'EgyptSocialInsuranceStrategy', 1),
                ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000002', 3, '2026-01-01', NULL, 
                 '{"sourceReference":"Law No. 148 of 2019 & NOSI Decree 2026","employeeRate":0.11,"employerRate":0.1875,"minInsuredMonthly":2700.00,"maxInsuredMonthly":16700.00}', 
                 'EgyptSocialInsuranceStrategy', 1)
            ON CONFLICT (rule_id, version_number) DO UPDATE
            SET effective_from = EXCLUDED.effective_from,
                effective_to = EXCLUDED.effective_to,
                parameters_json = EXCLUDED.parameters_json,
                calculation_strategy_name = EXCLUDED.calculation_strategy_name,
                status = EXCLUDED.status;
        """))
        {
            await gosiCmd.ExecuteNonQueryAsync(ct);
        }

        // 3. Seed Complete Article 8 Multi-Band Matrices for Income Tax (Law 30/2023 & Law 7/2024)
        var law30Json = """
        {
          "sourceReference": "Law No. 91 of 2005 as amended by Law No. 30 of 2023",
          "officialGazette": "Official Gazette Vol 24 bis, 15 June 2023",
          "personalExemptionYearly": 15000.00,
          "statutoryRounding": "RoundDownToNearest10",
          "incomeBands": [
            {
              "bandIndex": 1,
              "name": "Band 1: Up to 600,000 EGP",
              "minAnnualIncome": 0,
              "maxAnnualIncome": 600000.00,
              "tranches": [
                { "trancheIndex": 1, "from": 0, "to": 30000.00, "rate": 0.00 },
                { "trancheIndex": 2, "from": 30000.00, "to": 45000.00, "rate": 0.10 },
                { "trancheIndex": 3, "from": 45000.00, "to": 60000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
              ]
            },
            {
              "bandIndex": 2,
              "name": "Band 2: Over 600,000 to 700,000 EGP",
              "minAnnualIncome": 600000.00,
              "maxAnnualIncome": 700000.00,
              "tranches": [
                { "trancheIndex": 2, "from": 0, "to": 45000.00, "rate": 0.10 },
                { "trancheIndex": 3, "from": 45000.00, "to": 60000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
              ]
            },
            {
              "bandIndex": 3,
              "name": "Band 3: Over 700,000 to 800,000 EGP",
              "minAnnualIncome": 700000.00,
              "maxAnnualIncome": 800000.00,
              "tranches": [
                { "trancheIndex": 3, "from": 0, "to": 60000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 60000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
              ]
            },
            {
              "bandIndex": 4,
              "name": "Band 4: Over 800,000 to 900,000 EGP",
              "minAnnualIncome": 800000.00,
              "maxAnnualIncome": 900000.00,
              "tranches": [
                { "trancheIndex": 4, "from": 0, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
              ]
            },
            {
              "bandIndex": 5,
              "name": "Band 5: Over 900,000 to 1,000,000 EGP",
              "minAnnualIncome": 900000.00,
              "maxAnnualIncome": 1000000.00,
              "tranches": [
                { "trancheIndex": 5, "from": 0, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": null, "rate": 0.25 }
              ]
            },
            {
              "bandIndex": 6,
              "name": "Band 6: Over 1,000,000 EGP",
              "minAnnualIncome": 1000000.00,
              "maxAnnualIncome": null,
              "tranches": [
                { "trancheIndex": 6, "from": 0, "to": null, "rate": 0.25 }
              ]
            }
          ]
        }
        """;

        var law7Json = """
        {
          "sourceReference": "Law No. 91 of 2005 as amended by Law No. 7 of 2024",
          "officialGazette": "Official Gazette Issue 7 bis (a), 21 February 2024",
          "personalExemptionYearly": 20000.00,
          "statutoryRounding": "RoundDownToNearest10",
          "incomeBands": [
            {
              "bandIndex": 1,
              "name": "Band 1: Up to 600,000 EGP",
              "minAnnualIncome": 0,
              "maxAnnualIncome": 600000.00,
              "tranches": [
                { "trancheIndex": 1, "from": 0, "to": 40000.00, "rate": 0.00 },
                { "trancheIndex": 2, "from": 40000.00, "to": 55000.00, "rate": 0.10 },
                { "trancheIndex": 3, "from": 55000.00, "to": 70000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            },
            {
              "bandIndex": 2,
              "name": "Band 2: Over 600,000 to 700,000 EGP",
              "minAnnualIncome": 600000.00,
              "maxAnnualIncome": 700000.00,
              "tranches": [
                { "trancheIndex": 2, "from": 0, "to": 55000.00, "rate": 0.10 },
                { "trancheIndex": 3, "from": 55000.00, "to": 70000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            },
            {
              "bandIndex": 3,
              "name": "Band 3: Over 700,000 to 800,000 EGP",
              "minAnnualIncome": 700000.00,
              "maxAnnualIncome": 800000.00,
              "tranches": [
                { "trancheIndex": 3, "from": 0, "to": 70000.00, "rate": 0.15 },
                { "trancheIndex": 4, "from": 70000.00, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            },
            {
              "bandIndex": 4,
              "name": "Band 4: Over 800,000 to 900,000 EGP",
              "minAnnualIncome": 800000.00,
              "maxAnnualIncome": 900000.00,
              "tranches": [
                { "trancheIndex": 4, "from": 0, "to": 200000.00, "rate": 0.20 },
                { "trancheIndex": 5, "from": 200000.00, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            },
            {
              "bandIndex": 5,
              "name": "Band 5: Over 900,000 to 1,200,000 EGP",
              "minAnnualIncome": 900000.00,
              "maxAnnualIncome": 1200000.00,
              "tranches": [
                { "trancheIndex": 5, "from": 0, "to": 400000.00, "rate": 0.225 },
                { "trancheIndex": 6, "from": 400000.00, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            },
            {
              "bandIndex": 6,
              "name": "Band 6: Over 1,200,000 EGP",
              "minAnnualIncome": 1200000.00,
              "maxAnnualIncome": null,
              "tranches": [
                { "trancheIndex": 6, "from": 0, "to": 1200000.00, "rate": 0.25 },
                { "trancheIndex": 7, "from": 1200000.00, "to": null, "rate": 0.275 }
              ]
            }
          ]
        }
        """;

        await using (var taxCmd = _dataSource.CreateCommand("""
            INSERT INTO compliance.statutory_rule_versions (id, rule_id, version_number, effective_from, effective_to, parameters_json, calculation_strategy_name, status)
            VALUES
                ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 1, '2023-07-01', '2024-02-29',
                 $1::jsonb, 'EgyptProgressiveIncomeTaxStrategy', 1),
                ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 2, '2024-03-01', NULL,
                 $2::jsonb, 'EgyptProgressiveIncomeTaxStrategy', 1)
            ON CONFLICT (rule_id, version_number) DO UPDATE
            SET effective_from = EXCLUDED.effective_from,
                effective_to = EXCLUDED.effective_to,
                parameters_json = EXCLUDED.parameters_json,
                calculation_strategy_name = EXCLUDED.calculation_strategy_name,
                status = EXCLUDED.status;
        """))
        {
            taxCmd.Parameters.AddWithValue(law30Json);
            taxCmd.Parameters.AddWithValue(law7Json);
            await taxCmd.ExecuteNonQueryAsync(ct);
        }
    }
}
