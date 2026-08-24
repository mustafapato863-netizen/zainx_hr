using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Approvals.Domain;
using Workforce.Modules.Approvals.Infrastructure;
using Workforce.Modules.Recruitment.Domain;
using Workforce.Modules.Recruitment.Infrastructure;
using Workforce.SharedKernel.Primitives;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Recruitment.Api;

public record CreateOfferRequest(
    Guid ApplicationId,
    Guid CandidateId,
    string TitleEn,
    string TitleAr,
    DateOnly ProposedStartDate,
    decimal BaseSalaryMonthly,
    string Currency,
    string? AllowancesJson,
    string? ConditionsNote,
    DateOnly? ExpiryDate,
    Guid? OfferDocumentId
);

public record UpdateOfferTermsRequest(
    string TitleEn,
    string TitleAr,
    DateOnly ProposedStartDate,
    decimal BaseSalaryMonthly,
    string Currency,
    string? AllowancesJson,
    string? ConditionsNote,
    DateOnly? ExpiryDate,
    Guid? OfferDocumentId,
    uint ExpectedRowVersion
);

public record OfferDetailDto(
    Guid Id,
    Guid TenantId,
    Guid LegalEntityId,
    Guid ApplicationId,
    Guid CandidateId,
    int OfferVersionNumber,
    string TitleEn,
    string TitleAr,
    DateOnly ProposedStartDate,
    decimal? BaseSalaryMonthly, // Null when masked for unauthorized users
    string Currency,
    string? AllowancesJson,
    string? ConditionsNote,
    OfferStatus Status,
    Guid? ApprovalRequestId,
    DateTime? IssuedAtUtc,
    DateTime? AcceptedAtUtc,
    DateOnly? ExpiryDate,
    Guid? OfferDocumentId,
    DateTime CreatedAtUtc,
    uint RowVersion
);

[ApiController]
[Route("api/v1/recruitment/offers")]
public class RecruitmentOffersController : ControllerBase
{
    private readonly IRecruitmentRepository _repository;
    private readonly IApprovalsRepository? _approvalsRepository;
    private readonly IUserContext _userContext;

    public RecruitmentOffersController(
        IRecruitmentRepository repository,
        IUserContext userContext,
        IApprovalsRepository? approvalsRepository = null)
    {
        _repository = repository;
        _userContext = userContext;
        _approvalsRepository = approvalsRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OfferDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOffersForApplication([FromQuery] Guid applicationId, CancellationToken ct)
    {
        var offers = await _repository.GetOffersForApplicationAsync(_userContext.TenantId, applicationId, ct);
        var hasSensitivePermission = _userContext.HasPermission("recruitment.offer.read_sensitive") 
                                     || _userContext.HasPermission("admin");

        var dtos = offers.Select(o => MapOfferDto(o, hasSensitivePermission)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOfferById(Guid id, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        var hasSensitivePermission = _userContext.HasPermission("recruitment.offer.read_sensitive") 
                                     || _userContext.HasPermission("admin");

        return Ok(MapOfferDto(offer, hasSensitivePermission));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOffer([FromBody] CreateOfferRequest request, CancellationToken ct)
    {
        var app = await _repository.GetApplicationByIdAsync(_userContext.TenantId, request.ApplicationId, ct);
        if (app == null) return BadRequest("Application not found.");

        var existingOffers = await _repository.GetOffersForApplicationAsync(_userContext.TenantId, request.ApplicationId, ct);
        var nextVersion = existingOffers.Count > 0 ? existingOffers.Max(o => o.OfferVersionNumber) + 1 : 1;

        var offer = new Offer(
            Guid.NewGuid(),
            _userContext.TenantId,
            app.LegalEntityId,
            request.ApplicationId,
            request.CandidateId,
            nextVersion,
            request.TitleEn,
            request.TitleAr,
            request.ProposedStartDate,
            request.BaseSalaryMonthly,
            request.Currency,
            request.AllowancesJson,
            request.ConditionsNote,
            request.ExpiryDate,
            request.OfferDocumentId
        );

        await _repository.CreateOfferAsync(offer, ct);
        return CreatedAtAction(nameof(GetOfferById), new { id = offer.Id }, MapOfferDto(offer, true));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOfferTerms(Guid id, [FromBody] UpdateOfferTermsRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.UpdateTerms(
                request.TitleEn,
                request.TitleAr,
                request.ProposedStartDate,
                request.BaseSalaryMonthly,
                request.Currency,
                request.AllowancesJson,
                request.ConditionsNote,
                request.ExpiryDate,
                request.OfferDocumentId,
                request.ExpectedRowVersion
            );

            await _repository.UpdateOfferAsync(offer, ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/submit-approval")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForApproval(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            var approvalId = Guid.NewGuid();
            if (_approvalsRepository != null)
            {
                var approvalReq = new ApprovalRequest(
                    approvalId,
                    _userContext.TenantId,
                    offer.LegalEntityId,
                    "RECRUITMENT",
                    offer.Id,
                    "OFFER",
                    $"Offer: {offer.TitleEn} (v{offer.OfferVersionNumber})",
                    _userContext.UserId.Value,
                    Guid.Empty,
                    null,
                    1
                );
                await _approvalsRepository.SaveApprovalRequestAsync(approvalReq);
            }

            offer.SubmitForApproval(approvalId, request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveOffer(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.Approve(request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "OfferApproved", new OfferApprovedEvent(offer.Id, offer.ApplicationId, offer.CandidateId), ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/issue")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueOffer(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.Issue(request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "OfferIssued", new OfferIssuedEvent(offer.Id, offer.ApplicationId, offer.CandidateId), ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptOffer(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.Accept(request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            await _repository.SaveOutboxMessageAsync(_userContext.TenantId, "OfferAccepted", new OfferAcceptedEvent(offer.Id, offer.ApplicationId, offer.CandidateId), ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/decline")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeclineOffer(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.Decline(request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(OfferDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> WithdrawOffer(Guid id, [FromBody] ConcurrencyActionRequest request, CancellationToken ct)
    {
        var offer = await _repository.GetOfferByIdAsync(_userContext.TenantId, id, ct);
        if (offer == null) return NotFound();

        try
        {
            offer.Withdraw(request.ExpectedRowVersion);
            await _repository.UpdateOfferAsync(offer, ct);
            return Ok(MapOfferDto(offer, true));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    private static OfferDetailDto MapOfferDto(Offer o, bool includeSensitive)
    {
        return new OfferDetailDto(
            o.Id,
            o.TenantId.Value,
            o.LegalEntityId.Value,
            o.ApplicationId,
            o.CandidateId,
            o.OfferVersionNumber,
            o.TitleEn,
            o.TitleAr,
            o.ProposedStartDate,
            includeSensitive ? o.BaseSalaryMonthly : null,
            o.Currency,
            includeSensitive ? o.AllowancesJson : null,
            o.ConditionsNote,
            o.Status,
            o.ApprovalRequestId,
            o.IssuedAtUtc,
            o.AcceptedAtUtc,
            o.ExpiryDate,
            o.OfferDocumentId,
            o.CreatedAtUtc,
            o.RowVersion
        );
    }
}
