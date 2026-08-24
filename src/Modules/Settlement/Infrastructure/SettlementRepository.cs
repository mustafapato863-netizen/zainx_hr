using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Workforce.Modules.Settlement.Domain;
using Workforce.SharedKernel.Primitives;

namespace Workforce.Modules.Settlement.Infrastructure;

public interface ISettlementRepository
{
    Task CreateBatchAsync(SettlementBatch batch, CancellationToken ct = default);
    Task<SettlementBatch?> GetBatchByIdAsync(TenantId tenantId, Guid batchId, CancellationToken ct = default);
    Task<SettlementBatch?> GetBatchByPayrollRunIdAsync(TenantId tenantId, Guid payrollRunId, CancellationToken ct = default);
    Task<IReadOnlyList<SettlementBatch>> GetBatchesAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default);
    Task UpdateBatchAsync(SettlementBatch batch, CancellationToken ct = default);
    Task SaveExportAsync(PaymentExport export, CancellationToken ct = default);
    Task<PaymentExport?> GetLatestExportAsync(Guid settlementBatchId, CancellationToken ct = default);
}

public class SettlementRepository : ISettlementRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SettlementRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task CreateBatchAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        await using (var cmd = new NpgsqlCommand("""
            INSERT INTO settlement.settlement_batches (
                id, tenant_id, legal_entity_id, payroll_run_id, batch_number, total_amount,
                currency, payment_date, status, row_version, created_at_utc, updated_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11, $12);
        """, conn, tx))
        {
            cmd.Parameters.AddWithValue(batch.Id);
            cmd.Parameters.AddWithValue(batch.TenantId.Value);
            cmd.Parameters.AddWithValue(batch.LegalEntityId.Value);
            cmd.Parameters.AddWithValue(batch.PayrollRunId);
            cmd.Parameters.AddWithValue(batch.BatchNumber);
            cmd.Parameters.AddWithValue(batch.TotalAmount);
            cmd.Parameters.AddWithValue(batch.Currency);
            cmd.Parameters.AddWithValue(batch.PaymentDate);
            cmd.Parameters.AddWithValue((int)batch.Status);
            cmd.Parameters.AddWithValue((long)batch.RowVersion);
            cmd.Parameters.AddWithValue(batch.CreatedAtUtc);
            cmd.Parameters.AddWithValue(batch.UpdatedAtUtc);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        foreach (var inst in batch.Instructions)
        {
            await using var iCmd = new NpgsqlCommand("""
                INSERT INTO settlement.payment_instructions (
                    id, settlement_batch_id, employment_id, beneficiary_name, bank_code,
                    encrypted_account_or_iban, amount, status
                ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8);
            """, conn, tx);

            iCmd.Parameters.AddWithValue(inst.Id);
            iCmd.Parameters.AddWithValue(inst.SettlementBatchId);
            iCmd.Parameters.AddWithValue(inst.EmploymentId);
            iCmd.Parameters.AddWithValue(inst.BeneficiaryName);
            iCmd.Parameters.AddWithValue(inst.BankCode);
            iCmd.Parameters.AddWithValue(inst.EncryptedAccountOrIban);
            iCmd.Parameters.AddWithValue(inst.Amount);
            iCmd.Parameters.AddWithValue((int)inst.Status);

            await iCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<SettlementBatch?> GetBatchByIdAsync(TenantId tenantId, Guid batchId, CancellationToken ct = default)
    {
        SettlementBatch? batch = null;

        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, payroll_run_id, batch_number, total_amount,
                   currency, payment_date, status, row_version, created_at_utc, updated_at_utc
            FROM settlement.settlement_batches
            WHERE tenant_id = $1 AND id = $2;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(batchId);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                batch = new SettlementBatch(
                    reader.GetGuid(0),
                    new TenantId(reader.GetGuid(1)),
                    new LegalEntityId(reader.GetGuid(2)),
                    reader.GetGuid(3),
                    reader.GetString(4),
                    reader.GetDecimal(5),
                    reader.GetFieldValue<DateOnly>(7),
                    reader.GetString(6)
                );

                var status = (SettlementStatus)reader.GetInt32(8);
                var rowVersion = (uint)reader.GetInt64(9);

                var type = typeof(SettlementBatch);
                type.GetProperty(nameof(SettlementBatch.Status))!.SetValue(batch, status);
                type.GetProperty(nameof(SettlementBatch.RowVersion))!.SetValue(batch, rowVersion);
            }
        }

        if (batch == null) return null;

        // Load instructions
        await using var instCmd = _dataSource.CreateCommand("""
            SELECT id, settlement_batch_id, employment_id, beneficiary_name, bank_code,
                   encrypted_account_or_iban, amount, status
            FROM settlement.payment_instructions
            WHERE settlement_batch_id = $1;
        """);
        instCmd.Parameters.AddWithValue(batch.Id);

        await using (var iReader = await instCmd.ExecuteReaderAsync(ct))
        {
            while (await iReader.ReadAsync(ct))
            {
                batch.AddInstruction(new PaymentInstruction(
                    iReader.GetGuid(0),
                    iReader.GetGuid(1),
                    iReader.GetGuid(2),
                    iReader.GetString(3),
                    iReader.GetString(4),
                    iReader.GetString(5),
                    iReader.GetDecimal(6),
                    (PaymentInstructionStatus)iReader.GetInt32(7)
                ));
            }
        }

        return batch;
    }

    public async Task<SettlementBatch?> GetBatchByPayrollRunIdAsync(TenantId tenantId, Guid payrollRunId, CancellationToken ct = default)
    {
        Guid? batchId = null;
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id FROM settlement.settlement_batches
            WHERE tenant_id = $1 AND payroll_run_id = $2
            LIMIT 1;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(payrollRunId);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct)) batchId = reader.GetGuid(0);
        }

        if (batchId == null) return null;
        return await GetBatchByIdAsync(tenantId, batchId.Value, ct);
    }

    public async Task<IReadOnlyList<SettlementBatch>> GetBatchesAsync(TenantId tenantId, LegalEntityId legalEntityId, CancellationToken ct = default)
    {
        var list = new List<SettlementBatch>();
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, tenant_id, legal_entity_id, payroll_run_id, batch_number, total_amount,
                   currency, payment_date, status, row_version, created_at_utc, updated_at_utc
            FROM settlement.settlement_batches
            WHERE tenant_id = $1 AND legal_entity_id = $2
            ORDER BY created_at_utc DESC;
        """);
        cmd.Parameters.AddWithValue(tenantId.Value);
        cmd.Parameters.AddWithValue(legalEntityId.Value);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var batch = new SettlementBatch(
                reader.GetGuid(0),
                new TenantId(reader.GetGuid(1)),
                new LegalEntityId(reader.GetGuid(2)),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetDecimal(5),
                reader.GetFieldValue<DateOnly>(7),
                reader.GetString(6)
            );

            var type = typeof(SettlementBatch);
            type.GetProperty(nameof(SettlementBatch.Status))!.SetValue(batch, (SettlementStatus)reader.GetInt32(8));
            type.GetProperty(nameof(SettlementBatch.RowVersion))!.SetValue(batch, (uint)reader.GetInt64(9));

            list.Add(batch);
        }

        return list;
    }

    public async Task UpdateBatchAsync(SettlementBatch batch, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            UPDATE settlement.settlement_batches
            SET status = $1,
                updated_at_utc = $2,
                row_version = $3
            WHERE tenant_id = $4 AND id = $5;
        """);

        cmd.Parameters.AddWithValue((int)batch.Status);
        cmd.Parameters.AddWithValue(batch.UpdatedAtUtc);
        cmd.Parameters.AddWithValue((long)batch.RowVersion);
        cmd.Parameters.AddWithValue(batch.TenantId.Value);
        cmd.Parameters.AddWithValue(batch.Id);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveExportAsync(PaymentExport export, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            INSERT INTO settlement.payment_exports (
                id, settlement_batch_id, format, storage_path, file_sha256, download_count, created_at_utc
            ) VALUES ($1, $2, $3, $4, $5, $6, $7);
        """);

        cmd.Parameters.AddWithValue(export.Id);
        cmd.Parameters.AddWithValue(export.SettlementBatchId);
        cmd.Parameters.AddWithValue((int)export.Format);
        cmd.Parameters.AddWithValue(export.StoragePath);
        cmd.Parameters.AddWithValue(export.FileSha256);
        cmd.Parameters.AddWithValue(export.DownloadCount);
        cmd.Parameters.AddWithValue(export.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PaymentExport?> GetLatestExportAsync(Guid settlementBatchId, CancellationToken ct = default)
    {
        await using var cmd = _dataSource.CreateCommand("""
            SELECT id, settlement_batch_id, format, storage_path, file_sha256, download_count
            FROM settlement.payment_exports
            WHERE settlement_batch_id = $1
            ORDER BY created_at_utc DESC
            LIMIT 1;
        """);
        cmd.Parameters.AddWithValue(settlementBatchId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            var export = new PaymentExport(
                reader.GetGuid(0),
                reader.GetGuid(1),
                (ExportFormat)reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4)
            );
            return export;
        }

        return null;
    }
}
