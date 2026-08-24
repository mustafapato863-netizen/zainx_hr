using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Payroll.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Payroll.Infrastructure;

public interface IPayrollRepository
{
    Task CreatePeriodAsync(PayrollPeriod period, CancellationToken ct = default);
    Task<PayrollPeriod?> GetPeriodByIdAsync(TenantId tenantId, Guid periodId, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default);

    Task CreateRunAsync(PayrollRun run, CancellationToken ct = default);
    Task<PayrollRun?> GetRunByIdAsync(TenantId tenantId, Guid runId, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollRun>> GetRunsAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default);
    Task UpdateRunAsync(PayrollRun run, CancellationToken ct = default);

    Task SaveSnapshotsAsync(Guid runId, IEnumerable<PayrollInputSnapshot> snapshots, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollInputSnapshot>> GetSnapshotsByRunAsync(Guid runId, CancellationToken ct = default);

    Task SaveResultsAndTracesAsync(PayrollRun run, CancellationToken ct = default);
    Task<IReadOnlyList<PayrollEmployeeResult>> GetEmployeeResultsAsync(Guid runId, CancellationToken ct = default);
    Task<PayrollEmployeeResult?> GetEmployeeResultDetailAsync(Guid runId, Guid employmentId, CancellationToken ct = default);

    Task<IReadOnlyList<PayrollException>> GetExceptionsByRunAsync(Guid runId, CancellationToken ct = default);
    Task UpdateExceptionAsync(PayrollException exception, CancellationToken ct = default);
}

public class PayrollRepository : IPayrollRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PayrollRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task CreatePeriodAsync(PayrollPeriod period, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO payroll.payroll_periods (
                id, tenant_id, legal_entity_id, code, period_start, period_end, payment_date, is_active
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8);
        """);

        cmd.Parameters.AddWithValue(period.Id);
        cmd.Parameters.AddWithValue(period.TenantId.Value);
        cmd.Parameters.AddWithValue(period.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(period.Code);
        cmd.Parameters.AddWithValue(period.PeriodStart);
        cmd.Parameters.AddWithValue(period.PeriodEnd);
        cmd.Parameters.AddWithValue(period.PaymentDate);
        cmd.Parameters.AddWithValue(period.IsActive);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PayrollPeriod?> GetPeriodByIdAsync(TenantId tenantId, Guid periodId, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, code, period_start, period_end, payment_date, is_active
            FROM payroll.payroll_periods
            WHERE tenant_id = $1 AND id = $2;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(periodId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new PayrollPeriod(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetFieldValue<DateOnly>(4),
                reader.GetFieldValue<DateOnly>(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetBoolean(7)
            );
        }

        return null;
    }

    public async Task<IReadOnlyList<PayrollPeriod>> GetPeriodsAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default)
    {
        var list = new List<PayrollPeriod>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, code, period_start, period_end, payment_date, is_active
            FROM payroll.payroll_periods
            WHERE tenant_id = $1 AND legal_entity_id = $2
            ORDER BY period_start DESC;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(legalEntityId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PayrollPeriod(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetString(3),
                reader.GetFieldValue<DateOnly>(4),
                reader.GetFieldValue<DateOnly>(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetBoolean(7)
            ));
        }

        return list;
    }

    public async Task CreateRunAsync(PayrollRun run, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO payroll.payroll_runs (
                id, tenant_id, legal_entity_id, period_id, code, status, currency, total_gross, total_net,
                total_employer_contributions, employee_count, reproducibility_hash, row_version, created_at_utc, updated_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12, $13, $14, $15);
        """);

        cmd.Parameters.AddWithValue(run.Id);
        cmd.Parameters.AddWithValue(run.TenantId.Value);
        cmd.Parameters.AddWithValue(run.LegalEntityId.Value);
        cmd.Parameters.AddWithValue(run.PeriodId);
        cmd.Parameters.AddWithValue(run.Code);
        cmd.Parameters.AddWithValue((int)run.Status);
        cmd.Parameters.AddWithValue(run.Currency);
        cmd.Parameters.AddWithValue(run.TotalGross);
        cmd.Parameters.AddWithValue(run.TotalNet);
        cmd.Parameters.AddWithValue(run.TotalEmployerContributions);
        cmd.Parameters.AddWithValue(run.EmployeeCount);
        cmd.Parameters.AddWithValue(run.ReproducibilityHash);
        cmd.Parameters.AddWithValue((long)run.RowVersion);
        cmd.Parameters.AddWithValue(run.CreatedAtUtc);
        cmd.Parameters.AddWithValue(run.UpdatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PayrollRun?> GetRunByIdAsync(TenantId tenantId, Guid runId, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, period_id, code, status, currency, total_gross, total_net,
                   total_employer_contributions, employee_count, reproducibility_hash, approval_request_id,
                   finalized_at_utc, finalized_by_user_id, row_version, created_at_utc, updated_at_utc
            FROM payroll.payroll_runs
            WHERE tenant_id = $1 AND id = $2;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(runId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var run = new PayrollRun(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(6)
            );

            // Reconstruct aggregate state
            var status = (PayrollRunStatus)reader.GetInt32(5);
            var gross = reader.GetDecimal(7);
            var net = reader.GetDecimal(8);
            var employer = reader.GetDecimal(9);
            var count = reader.GetInt32(10);
            var hash = reader.GetString(11);
            var rowVersion = (uint)reader.GetInt64(15);

            // Apply private setters via reflection or loader
            var type = typeof(PayrollRun);
            type.GetProperty(nameof(PayrollRun.Status))!.SetValue(run, status);
            type.GetProperty(nameof(PayrollRun.TotalGross))!.SetValue(run, gross);
            type.GetProperty(nameof(PayrollRun.TotalNet))!.SetValue(run, net);
            type.GetProperty(nameof(PayrollRun.TotalEmployerContributions))!.SetValue(run, employer);
            type.GetProperty(nameof(PayrollRun.EmployeeCount))!.SetValue(run, count);
            type.GetProperty(nameof(PayrollRun.ReproducibilityHash))!.SetValue(run, hash);
            type.GetProperty(nameof(PayrollRun.RowVersion))!.SetValue(run, rowVersion);
            if (!reader.IsDBNull(12)) type.GetProperty(nameof(PayrollRun.ApprovalRequestId))!.SetValue(run, reader.GetGuid(12));
            if (!reader.IsDBNull(13)) type.GetProperty(nameof(PayrollRun.FinalizedAtUtc))!.SetValue(run, reader.GetDateTime(13));
            if (!reader.IsDBNull(14)) type.GetProperty(nameof(PayrollRun.FinalizedByUserId))!.SetValue(run, reader.GetGuid(14));

            return run;
        }

        return null;
    }

    public async Task<IReadOnlyList<PayrollRun>> GetRunsAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default)
    {
        var list = new List<PayrollRun>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, period_id, code, status, currency, total_gross, total_net,
                   total_employer_contributions, employee_count, reproducibility_hash, approval_request_id,
                   finalized_at_utc, finalized_by_user_id, row_version, created_at_utc, updated_at_utc
            FROM payroll.payroll_runs
            WHERE tenant_id = $1 AND legal_entity_id = $2
            ORDER BY created_at_utc DESC;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(legalEntityId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var run = new PayrollRun(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(6)
            );

            var type = typeof(PayrollRun);
            type.GetProperty(nameof(PayrollRun.Status))!.SetValue(run, (PayrollRunStatus)reader.GetInt32(5));
            type.GetProperty(nameof(PayrollRun.TotalGross))!.SetValue(run, reader.GetDecimal(7));
            type.GetProperty(nameof(PayrollRun.TotalNet))!.SetValue(run, reader.GetDecimal(8));
            type.GetProperty(nameof(PayrollRun.TotalEmployerContributions))!.SetValue(run, reader.GetDecimal(9));
            type.GetProperty(nameof(PayrollRun.EmployeeCount))!.SetValue(run, reader.GetInt32(10));
            type.GetProperty(nameof(PayrollRun.ReproducibilityHash))!.SetValue(run, reader.GetString(11));
            type.GetProperty(nameof(PayrollRun.RowVersion))!.SetValue(run, (uint)reader.GetInt64(15));
            if (!reader.IsDBNull(12)) type.GetProperty(nameof(PayrollRun.ApprovalRequestId))!.SetValue(run, reader.GetGuid(12));
            if (!reader.IsDBNull(13)) type.GetProperty(nameof(PayrollRun.FinalizedAtUtc))!.SetValue(run, reader.GetDateTime(13));
            if (!reader.IsDBNull(14)) type.GetProperty(nameof(PayrollRun.FinalizedByUserId))!.SetValue(run, reader.GetGuid(14));

            list.Add(run);
        }

        return list;
    }

    public async Task UpdateRunAsync(PayrollRun run, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            UPDATE payroll.payroll_runs
            SET status = $1,
                total_gross = $2,
                total_net = $3,
                total_employer_contributions = $4,
                employee_count = $5,
                reproducibility_hash = $6,
                approval_request_id = $7,
                finalized_at_utc = $8,
                finalized_by_user_id = $9,
                updated_at_utc = $10,
                row_version = $11
            WHERE tenant_id = $12 AND id = $13;
        """);

        cmd.Parameters.AddWithValue((int)run.Status);
        cmd.Parameters.AddWithValue(run.TotalGross);
        cmd.Parameters.AddWithValue(run.TotalNet);
        cmd.Parameters.AddWithValue(run.TotalEmployerContributions);
        cmd.Parameters.AddWithValue(run.EmployeeCount);
        cmd.Parameters.AddWithValue(run.ReproducibilityHash);
        cmd.Parameters.AddWithValue((object?)run.ApprovalRequestId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)run.FinalizedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)run.FinalizedByUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue(run.UpdatedAtUtc);
        cmd.Parameters.AddWithValue((long)run.RowVersion);
        cmd.Parameters.AddWithValue(run.TenantId.Value);
        cmd.Parameters.AddWithValue(run.Id);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveSnapshotsAsync(Guid runId, IEnumerable<PayrollInputSnapshot> snapshots, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        foreach (var s in snapshots)
        {
            await using var cmd = new NpgsqlCommand("""
                INSERT INTO payroll.payroll_input_snapshots (
                    id, payroll_run_id, employment_id, base_salary_monthly, allowances_json,
                    scheduled_days, verified_worked_minutes, approved_absence_days, approved_leave_days,
                    unpaid_leave_days, captured_at_utc
                ) VALUES ($1, $2, $3, $4, $5::jsonb, $6, $7, $8, $9, $10, $11)
                ON CONFLICT (payroll_run_id, employment_id) DO UPDATE
                SET base_salary_monthly = EXCLUDED.base_salary_monthly,
                    allowances_json = EXCLUDED.allowances_json,
                    scheduled_days = EXCLUDED.scheduled_days,
                    verified_worked_minutes = EXCLUDED.verified_worked_minutes,
                    approved_absence_days = EXCLUDED.approved_absence_days,
                    approved_leave_days = EXCLUDED.approved_leave_days,
                    unpaid_leave_days = EXCLUDED.unpaid_leave_days;
            """, conn, tx);

            cmd.Parameters.AddWithValue(s.Id);
            cmd.Parameters.AddWithValue(s.PayrollRunId);
            cmd.Parameters.AddWithValue(s.EmploymentId);
            cmd.Parameters.AddWithValue(s.BaseSalaryMonthly);
            cmd.Parameters.AddWithValue(s.AllowancesJson);
            cmd.Parameters.AddWithValue(s.ScheduledDays);
            cmd.Parameters.AddWithValue(s.VerifiedWorkedMinutes);
            cmd.Parameters.AddWithValue(s.ApprovedAbsenceDays);
            cmd.Parameters.AddWithValue(s.ApprovedLeaveDays);
            cmd.Parameters.AddWithValue(s.UnpaidLeaveDays);
            cmd.Parameters.AddWithValue(s.CapturedAtUtc);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<PayrollInputSnapshot>> GetSnapshotsByRunAsync(Guid runId, CancellationToken ct = default)
    {
        var list = new List<PayrollInputSnapshot>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, payroll_run_id, employment_id, base_salary_monthly, allowances_json,
                   scheduled_days, verified_worked_minutes, approved_absence_days, approved_leave_days,
                   unpaid_leave_days
            FROM payroll.payroll_input_snapshots
            WHERE payroll_run_id = $1;
        """);
        cmd.Parameters.AddWithValue(runId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PayrollInputSnapshot(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetDecimal(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9)
            ));
        }

        return list;
    }

    public async Task SaveResultsAndTracesAsync(PayrollRun run, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // Delete previous results and traces for this run if re-calculating
        await using (var delCmd = new NpgsqlCommand("DELETE FROM payroll.payroll_employee_results WHERE payroll_run_id = $1;", conn, tx))
        {
            delCmd.Parameters.AddWithValue(run.Id);
            await delCmd.ExecuteNonQueryAsync(ct);
        }

        foreach (var res in run.EmployeeResults)
        {
            await using var resCmd = new NpgsqlCommand("""
                INSERT INTO payroll.payroll_employee_results (
                    id, payroll_run_id, employment_id, gross_pay, net_pay, total_earnings,
                    total_deductions, employer_contributions, row_version
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9);
            """, conn, tx);

            resCmd.Parameters.AddWithValue(res.Id);
            resCmd.Parameters.AddWithValue(res.PayrollRunId);
            resCmd.Parameters.AddWithValue(res.EmploymentId);
            resCmd.Parameters.AddWithValue(res.GrossPay);
            resCmd.Parameters.AddWithValue(res.NetPay);
            resCmd.Parameters.AddWithValue(res.TotalEarnings);
            resCmd.Parameters.AddWithValue(res.TotalDeductions);
            resCmd.Parameters.AddWithValue(res.EmployerContributions);
            resCmd.Parameters.AddWithValue((long)res.RowVersion);

            await resCmd.ExecuteNonQueryAsync(ct);

            // Traces
            foreach (var t in res.Traces)
            {
                await using var tCmd = new NpgsqlCommand("""
                    INSERT INTO payroll.calculation_traces (
                        id, employee_result_id, step_order, rule_reference, description, formula_applied,
                        input_values_json, intermediate_amount, rounding_delta, final_amount
                    ) VALUES ($1, $2, $3, $4, $5, $6, $7::jsonb, $8, $9, $10);
                """, conn, tx);

                tCmd.Parameters.AddWithValue(t.Id);
                tCmd.Parameters.AddWithValue(t.EmployeeResultId);
                tCmd.Parameters.AddWithValue(t.StepOrder);
                tCmd.Parameters.AddWithValue(t.RuleReference);
                tCmd.Parameters.AddWithValue(t.Description);
                tCmd.Parameters.AddWithValue(t.FormulaApplied);
                tCmd.Parameters.AddWithValue(t.InputValuesJson);
                tCmd.Parameters.AddWithValue(t.IntermediateAmount);
                tCmd.Parameters.AddWithValue(t.RoundingDelta);
                tCmd.Parameters.AddWithValue(t.FinalAmount);

                await tCmd.ExecuteNonQueryAsync(ct);
            }

            // Lines
            foreach (var l in res.Lines)
            {
                await using var lCmd = new NpgsqlCommand("""
                    INSERT INTO payroll.payroll_lines (
                        id, employee_result_id, component_code, name_en, name_ar, category, amount,
                        calculation_type, rate, hours_or_days, trace_id
                    ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11);
                """, conn, tx);

                lCmd.Parameters.AddWithValue(l.Id);
                lCmd.Parameters.AddWithValue(l.EmployeeResultId);
                lCmd.Parameters.AddWithValue(l.ComponentCode);
                lCmd.Parameters.AddWithValue(l.NameEn);
                lCmd.Parameters.AddWithValue(l.NameAr);
                lCmd.Parameters.AddWithValue((int)l.Category);
                lCmd.Parameters.AddWithValue(l.Amount);
                lCmd.Parameters.AddWithValue((int)l.CalculationType);
                lCmd.Parameters.AddWithValue(l.Rate);
                lCmd.Parameters.AddWithValue(l.HoursOrDays);
                lCmd.Parameters.AddWithValue((object?)l.TraceId ?? DBNull.Value);

                await lCmd.ExecuteNonQueryAsync(ct);
            }
        }

        // Exceptions
        foreach (var ex in run.Exceptions)
        {
            await using var exCmd = new NpgsqlCommand("""
                INSERT INTO payroll.payroll_exceptions (
                    id, payroll_run_id, employment_id, severity, category, reason,
                    resolution_guidance, status, created_at_utc
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
                ON CONFLICT (id) DO NOTHING;
            """, conn, tx);

            exCmd.Parameters.AddWithValue(ex.Id);
            exCmd.Parameters.AddWithValue(ex.PayrollRunId);
            exCmd.Parameters.AddWithValue(ex.EmploymentId);
            exCmd.Parameters.AddWithValue((int)ex.Severity);
            exCmd.Parameters.AddWithValue(ex.Category);
            exCmd.Parameters.AddWithValue(ex.Reason);
            exCmd.Parameters.AddWithValue(ex.ResolutionGuidance);
            exCmd.Parameters.AddWithValue((int)ex.Status);
            exCmd.Parameters.AddWithValue(ex.CreatedAtUtc);

            await exCmd.ExecuteNonQueryAsync(ct);
        }

        // Update Run header
        await using (var runCmd = new NpgsqlCommand("""
            UPDATE payroll.payroll_runs
            SET status = $1,
                total_gross = $2,
                total_net = $3,
                total_employer_contributions = $4,
                employee_count = $5,
                reproducibility_hash = $6,
                updated_at_utc = $7,
                row_version = $8
            WHERE tenant_id = $9 AND id = $10;
        """, conn, tx))
        {
            runCmd.Parameters.AddWithValue((int)run.Status);
            runCmd.Parameters.AddWithValue(run.TotalGross);
            runCmd.Parameters.AddWithValue(run.TotalNet);
            runCmd.Parameters.AddWithValue(run.TotalEmployerContributions);
            runCmd.Parameters.AddWithValue(run.EmployeeCount);
            runCmd.Parameters.AddWithValue(run.ReproducibilityHash);
            runCmd.Parameters.AddWithValue(run.UpdatedAtUtc);
            runCmd.Parameters.AddWithValue((long)run.RowVersion);
            runCmd.Parameters.AddWithValue(run.TenantId.Value);
            runCmd.Parameters.AddWithValue(run.Id);

            await runCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<PayrollEmployeeResult>> GetEmployeeResultsAsync(Guid runId, CancellationToken ct = default)
    {
        var list = new List<PayrollEmployeeResult>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, payroll_run_id, employment_id, gross_pay, net_pay, total_earnings,
                   total_deductions, employer_contributions
            FROM payroll.payroll_employee_results
            WHERE payroll_run_id = $1
            ORDER BY gross_pay DESC;
        """);
        cmd.Parameters.AddWithValue(runId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PayrollEmployeeResult(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetDecimal(5),
                reader.GetDecimal(6),
                reader.GetDecimal(7)
            ));
        }

        return list;
    }

    public async Task<PayrollEmployeeResult?> GetEmployeeResultDetailAsync(Guid runId, Guid employmentId, CancellationToken ct = default)
    {
        PayrollEmployeeResult? result = null;

        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, payroll_run_id, employment_id, gross_pay, net_pay, total_earnings,
                   total_deductions, employer_contributions
            FROM payroll.payroll_employee_results
            WHERE payroll_run_id = $1 AND employment_id = $2;
        """);
        cmd.Parameters.AddWithValue(runId);
        cmd.Parameters.AddWithValue(employmentId);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                result = new PayrollEmployeeResult(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetGuid(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetDecimal(5),
                    reader.GetDecimal(6),
                    reader.GetDecimal(7)
                );
            }
        }

        if (result == null) return null;

        // Fetch lines
        await using var lineCmd = _dataSource.CreateCommand("""
            SELECT id, employee_result_id, component_code, name_en, name_ar, category, amount,
                   calculation_type, rate, hours_or_days, trace_id
            FROM payroll.payroll_lines
            WHERE employee_result_id = $1
            ORDER BY category ASC, amount DESC;
        """);
        lineCmd.Parameters.AddWithValue(result.Id);

        await using (var lReader = await lineCmd.ExecuteReaderAsync(ct))
        {
            while (await lReader.ReadAsync(ct))
            {
                result.AddLine(new PayrollLine(
                    lReader.GetGuid(0),
                    lReader.GetGuid(1),
                    lReader.GetString(2),
                    lReader.GetString(3),
                    lReader.GetString(4),
                    (ComponentCategory)lReader.GetInt32(5),
                    lReader.GetDecimal(6),
                    (CalculationType)lReader.GetInt32(7),
                    lReader.GetDecimal(8),
                    lReader.GetDecimal(9),
                    lReader.IsDBNull(10) ? (Guid?)null : lReader.GetGuid(10)
                ));
            }
        }

        // Fetch traces
        await using var traceCmd = _dataSource.CreateCommand("""
            SELECT id, employee_result_id, step_order, rule_reference, description, formula_applied,
                   input_values_json, intermediate_amount, rounding_delta, final_amount
            FROM payroll.calculation_traces
            WHERE employee_result_id = $1
            ORDER BY step_order ASC;
        """);
        traceCmd.Parameters.AddWithValue(result.Id);

        await using (var tReader = await traceCmd.ExecuteReaderAsync(ct))
        {
            while (await tReader.ReadAsync(ct))
            {
                result.AddTrace(new CalculationTrace(
                    tReader.GetGuid(0),
                    tReader.GetGuid(1),
                    tReader.GetInt32(2),
                    tReader.GetString(3),
                    tReader.GetString(4),
                    tReader.GetString(5),
                    tReader.GetString(6),
                    tReader.GetDecimal(7),
                    tReader.GetDecimal(8),
                    tReader.GetDecimal(9)
                ));
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<PayrollException>> GetExceptionsByRunAsync(Guid runId, CancellationToken ct = default)
    {
        var list = new List<PayrollException>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, payroll_run_id, employment_id, severity, category, reason,
                   resolution_guidance, status, resolved_by_user_id, resolution_note, created_at_utc
            FROM payroll.payroll_exceptions
            WHERE payroll_run_id = $1
            ORDER BY severity DESC, created_at_utc ASC;
        """);
        cmd.Parameters.AddWithValue(runId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var ex = new PayrollException(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                (ExceptionSeverity)reader.GetInt32(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                (ExceptionStatus)reader.GetInt32(7)
            );

            if (!reader.IsDBNull(8))
            {
                ex.Resolve(reader.GetGuid(8), reader.IsDBNull(9) ? "" : reader.GetString(9));
            }

            list.Add(ex);
        }

        return list;
    }

    public async Task UpdateExceptionAsync(PayrollException exception, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            UPDATE payroll.payroll_exceptions
            SET status = $1,
                resolved_by_user_id = $2,
                resolution_note = $3
            WHERE id = $4;
        """);

        cmd.Parameters.AddWithValue((int)exception.Status);
        cmd.Parameters.AddWithValue((object?)exception.ResolvedByUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)exception.ResolutionNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue(exception.Id);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
