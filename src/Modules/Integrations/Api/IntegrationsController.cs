using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Workforce.Modules.Integrations.Application;
using Workforce.Modules.Integrations.Domain;
using Workforce.Modules.Integrations.Infrastructure;
using Workforce.SharedKernel.Security;

namespace Workforce.Modules.Integrations.Api;

public record CreateConnectorRequest(
    string Code,
    string NameEn,
    string NameAr,
    ConnectorType ConnectorType,
    IntegrationDirection Direction,
    string EndpointUrl,
    IntegrationAuthType AuthType,
    string? SecretCredential,
    bool IsActive,
    string EventSubscriptionsJson,
    string ConfigJson
);

public record UpdateConnectorRequest(
    string NameEn,
    string NameAr,
    string EndpointUrl,
    IntegrationAuthType AuthType,
    string? SecretCredential,
    bool IsActive,
    string EventSubscriptionsJson,
    string ConfigJson,
    uint ExpectedRowVersion
);

[ApiController]
[Route("api/v1/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IIntegrationsRepository _repository;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly IOutboundIntegrationAdapter _outboundAdapter;
    private readonly IUserContext _userContext;

    public IntegrationsController(
        IIntegrationsRepository repository,
        IPiiEncryptionService encryptionService,
        IOutboundIntegrationAdapter outboundAdapter,
        IUserContext userContext)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _outboundAdapter = outboundAdapter;
        _userContext = userContext;
    }

    [HttpGet("connectors")]
    [ProducesResponseType(typeof(IReadOnlyList<IntegrationConnector>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListConnectors(CancellationToken ct)
    {
        var list = await _repository.ListConnectorsAsync(_userContext.TenantId, ct);
        return Ok(list);
    }

    [HttpGet("connectors/{id:guid}")]
    [ProducesResponseType(typeof(IntegrationConnector), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConnector(Guid id, CancellationToken ct)
    {
        var connector = await _repository.GetConnectorByIdAsync(_userContext.TenantId, id, ct);
        if (connector == null) return NotFound();
        return Ok(connector);
    }

    [HttpPost("connectors")]
    [ProducesResponseType(typeof(IntegrationConnector), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateConnector([FromBody] CreateConnectorRequest request, CancellationToken ct)
    {
        var encryptedCreds = !string.IsNullOrWhiteSpace(request.SecretCredential)
            ? _encryptionService.Encrypt(request.SecretCredential)
            : null;

        var connector = new IntegrationConnector(
            Guid.NewGuid(),
            _userContext.TenantId,
            request.Code,
            request.NameEn,
            request.NameAr,
            request.ConnectorType,
            request.Direction,
            request.EndpointUrl,
            request.AuthType,
            encryptedCreds,
            1,
            request.IsActive,
            request.EventSubscriptionsJson,
            request.ConfigJson
        );

        await _repository.CreateConnectorAsync(connector, ct);
        return CreatedAtAction(nameof(GetConnector), new { id = connector.Id }, connector);
    }

    [HttpPut("connectors/{id:guid}")]
    [ProducesResponseType(typeof(IntegrationConnector), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateConnector(Guid id, [FromBody] UpdateConnectorRequest request, CancellationToken ct)
    {
        var connector = await _repository.GetConnectorByIdAsync(_userContext.TenantId, id, ct);
        if (connector == null) return NotFound();

        var encryptedCreds = !string.IsNullOrWhiteSpace(request.SecretCredential)
            ? _encryptionService.Encrypt(request.SecretCredential)
            : null;

        try
        {
            connector.Update(
                request.NameEn,
                request.NameAr,
                request.EndpointUrl,
                request.AuthType,
                encryptedCreds,
                request.IsActive,
                request.EventSubscriptionsJson,
                request.ConfigJson,
                request.ExpectedRowVersion
            );

            await _repository.UpdateConnectorAsync(connector, ct);
            return Ok(connector);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Concurrency Conflict", Detail = ex.Message });
        }
    }

    [HttpGet("deliveries")]
    [ProducesResponseType(typeof(PagedDeliveriesResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListDeliveries(
        [FromQuery] Guid? connectorId,
        [FromQuery] DeliveryStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _repository.ListDeliveriesAsync(_userContext.TenantId, connectorId, status, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("deliveries/{id:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryDelivery(Guid id, CancellationToken ct)
    {
        var success = await _repository.RetryDeliveryAsync(_userContext.TenantId, id, ct);
        return Ok(new { success });
    }

    [HttpPost("inbound/webhook/{connectorCode}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleInboundWebhook(
        string connectorCode,
        [FromHeader(Name = "X-ZainX-Signature")] string? signature,
        [FromHeader(Name = "X-ZainX-Timestamp")] string? timestampStr,
        [FromHeader(Name = "X-ZainX-Event-Id")] string? eventId,
        CancellationToken ct = default)
    {
        var connector = await _repository.GetConnectorByCodeAsync(_userContext.TenantId, connectorCode, ct);
        if (connector == null || !connector.IsActive)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Inactive or Unknown Connector" });
        }

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(ct);

        // Anti-Replay Timestamp Check (within 5 minutes)
        if (long.TryParse(timestampStr, out var ts))
        {
            var requestTime = DateTimeOffset.FromUnixTimeSeconds(ts);
            if (Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes) > 5)
            {
                return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Expired or Replayed Webhook Request" });
            }
        }

        // HMAC-SHA256 Signature Verification
        if (connector.AuthType == IntegrationAuthType.HmacSignature)
        {
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(connector.EncryptedCredentials))
            {
                return Unauthorized(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Missing Webhook Signature" });
            }

            var secret = _encryptionService.Decrypt(connector.EncryptedCredentials);
            var expectedSignedPayload = $"{timestampStr}.{payload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedSignedPayload));
            var expectedSig = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(signature.ToLowerInvariant()), Encoding.UTF8.GetBytes(expectedSig)))
            {
                return Unauthorized(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Invalid Webhook Signature" });
            }
        }

        var messageId = !string.IsNullOrWhiteSpace(eventId) ? eventId : Guid.NewGuid().ToString("N");
        var inboxMsg = new IntegrationInboxMessage(
            Guid.NewGuid(),
            _userContext.TenantId,
            connector.Code,
            messageId,
            payload
        );

        var recorded = await _repository.RecordInboxMessageAsync(inboxMsg, ct);
        return Ok(new { status = "Accepted", deduplicated = !recorded });
    }
}
