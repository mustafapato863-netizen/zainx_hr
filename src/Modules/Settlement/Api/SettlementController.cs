using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Payroll.Domain;
using Workforce.Modules.Payroll.Infrastructure;
using Workforce.Modules.Settlement.Domain;
using Workforce.Modules.Settlement.Domain.ExportAdapters;
using Workforce.Modules.Settlement.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Settlement.Api;

public record GenerateSettlementBatchRequest(
    Guid PayrollRunId,
    string BatchNumber,
    DateOnly PaymentDate
);

public record ApproveBatchRequest(uint ExpectedRowVersion);

public record SettlementBatchDto(
    Guid Id,
    Guid PayrollRunId,
    string BatchNumber,
    decimal TotalAmount,
    string Currency,
    DateOnly PaymentDate,
    string Status,
    int InstructionCount,
    uint RowVersion
);

public record PaymentInstructionDto(
    Guid Id,
    Guid EmploymentId,
    string BeneficiaryName,
    string BankCode,
    string AccountMasked,
    decimal Amount,
    string Status
);

public record SettlementBatchDetailDto(
    Guid Id,
    Guid PayrollRunId,
    string BatchNumber,
    decimal TotalAmount,
    string Currency,
    DateOnly PaymentDate,
    string Status,
    uint RowVersion,
    IReadOnlyList<PaymentInstructionDto> Instructions
);

[ApiController]
[Route("api/v1/settlement")]
public class SettlementController : ControllerBase
{
    private readonly ISettlementRepository _repository;
    private readonly IPayrollRepository _payrollRepository;
    private readonly IPaymentExportAdapter _exportAdapter;
    private readonly IUserContext _userContext;

    public SettlementController(
        ISettlementRepository repository,
        IPayrollRepository payrollRepository,
        IPaymentExportAdapter exportAdapter,
        IUserContext userContext)
    {
        _repository = repository;
        _payrollRepository = payrollRepository;
        _exportAdapter = exportAdapter;
        _userContext = userContext;
    }

    [HttpPost("batches/generate")]
    public async Task<ActionResult<SettlementBatchDto>> GenerateBatch(
        [FromBody] GenerateSettlementBatchRequest req,
        CancellationToken ct)
    {
        var run = await _payrollRepository.GetRunByIdAsync(_userContext.TenantId, req.PayrollRunId, ct);
        if (run == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Payroll run '{req.PayrollRunId}' not found." });

        if (run.Status != PayrollRunStatus.Finalized && run.Status != PayrollRunStatus.Approved)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = "Settlement batches can only be generated from Approved or Finalized payroll runs." });
        }

        var results = await _payrollRepository.GetEmployeeResultsAsync(run.Id, ct);
        if (results.Count == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "No employee results found in this payroll run." });
        }

        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var batch = new SettlementBatch(
            Guid.NewGuid(), _userContext.TenantId, legalEntityId,
            run.Id, req.BatchNumber, run.TotalNet, req.PaymentDate, run.Currency
        );

        foreach (var r in results)
        {
            var inst = new PaymentInstruction(
                Guid.NewGuid(), batch.Id, r.EmploymentId,
                $"Employee-{r.EmploymentId.ToString()[..8]}",
                "MISR",
                Workforce.SharedKernel.Security.AesGcmEncryptionService.EncryptDefault($"EG123456789012345678901234"),
                r.NetPay
            );
            batch.AddInstruction(inst);
        }

        await _repository.CreateBatchAsync(batch, ct);

        return Created($"/api/v1/settlement/batches/{batch.Id}", new SettlementBatchDto(
            batch.Id, batch.PayrollRunId, batch.BatchNumber, batch.TotalAmount,
            batch.Currency, batch.PaymentDate, batch.Status.ToString(),
            batch.Instructions.Count, batch.RowVersion
        ));
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyList<SettlementBatchDto>>> GetBatches(CancellationToken ct)
    {
        var legalEntityId = _userContext.LegalEntityId ?? new LegalEntityId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var batches = await _repository.GetBatchesAsync(_userContext.TenantId, legalEntityId, ct);
        var dtos = new List<SettlementBatchDto>();
        foreach (var b in batches)
        {
            dtos.Add(new SettlementBatchDto(
                b.Id, b.PayrollRunId, b.BatchNumber, b.TotalAmount,
                b.Currency, b.PaymentDate, b.Status.ToString(),
                b.Instructions.Count, b.RowVersion
            ));
        }

        return Ok(dtos);
    }

    [HttpGet("batches/{id:guid}")]
    public async Task<ActionResult<SettlementBatchDetailDto>> GetBatchById(Guid id, CancellationToken ct)
    {
        var batch = await _repository.GetBatchByIdAsync(_userContext.TenantId, id, ct);
        if (batch == null) return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Settlement batch '{id}' not found." });

        var instDtos = new List<PaymentInstructionDto>();
        foreach (var i in batch.Instructions)
        {
            var masked = i.EncryptedAccountOrIban.Length > 4
                ? $"•••• •••• •••• {i.EncryptedAccountOrIban[^4..]}"
                : "••••";

            instDtos.Add(new PaymentInstructionDto(
                i.Id, i.EmploymentId, i.BeneficiaryName, i.BankCode,
                masked, i.Amount, i.Status.ToString()
            ));
        }

        return Ok(new SettlementBatchDetailDto(
            batch.Id, batch.PayrollRunId, batch.BatchNumber, batch.TotalAmount,
            batch.Currency, batch.PaymentDate, batch.Status.ToString(),
            batch.RowVersion, instDtos
        ));
    }

    [HttpPost("batches/{id:guid}/approve")]
    public async Task<IActionResult> ApproveBatch(Guid id, [FromBody] ApproveBatchRequest req, CancellationToken ct)
    {
        var batch = await _repository.GetBatchByIdAsync(_userContext.TenantId, id, ct);
        if (batch == null) return NotFound();

        try
        {
            batch.Approve(req.ExpectedRowVersion);
            await _repository.UpdateBatchAsync(batch, ct);
            return Ok(new { status = batch.Status.ToString(), rowVersion = batch.RowVersion });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = "Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("batches/{id:guid}/export")]
    public async Task<IActionResult> ExportBatch(Guid id, CancellationToken ct)
    {
        var batch = await _repository.GetBatchByIdAsync(_userContext.TenantId, id, ct);
        if (batch == null) return NotFound();

        var exportRes = await _exportAdapter.GenerateExportAsync(batch, ct);
        var exportEntity = new PaymentExport(
            Guid.NewGuid(), batch.Id, _exportAdapter.Format,
            exportRes.FileName, exportRes.FileSha256
        );

        exportEntity.RecordDownload();
        await _repository.SaveExportAsync(exportEntity, ct);

        batch.MarkExported(batch.RowVersion);
        await _repository.UpdateBatchAsync(batch, ct);

        return File(exportRes.FileBytes, exportRes.ContentType, exportRes.FileName);
    }
}
