using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workforce.Modules.Integrations.Domain;

namespace Workforce.Modules.Integrations.Application;

public interface IOutboundIntegrationAdapter
{
    Task<(bool Succeeded, int StatusCode, string? ResponseOrError)> DeliverAsync(
        IntegrationConnector connector,
        IntegrationDeliveryJob job,
        string? decryptedSecret = null,
        CancellationToken ct = default);
}

public class GenericWebhookAdapter : IOutboundIntegrationAdapter
{
    private readonly HttpClient _httpClient;

    public GenericWebhookAdapter(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<(bool Succeeded, int StatusCode, string? ResponseOrError)> DeliverAsync(
        IntegrationConnector connector,
        IntegrationDeliveryJob job,
        string? decryptedSecret = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connector.EndpointUrl))
        {
            return (false, 400, "Invalid connector endpoint URL");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, connector.EndpointUrl);
            request.Content = new StringContent(job.PayloadJson, Encoding.UTF8, "application/json");

            // Standard headers
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            request.Headers.Add("X-ZainX-Delivery-Id", job.Id.ToString());
            request.Headers.Add("X-ZainX-Event", job.EventType);
            request.Headers.Add("X-ZainX-Timestamp", timestamp);

            // Compute HMAC-SHA256 signature if secret is present
            if (!string.IsNullOrWhiteSpace(decryptedSecret))
            {
                var signPayload = $"{timestamp}.{job.PayloadJson}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(decryptedSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signPayload));
                var signature = Convert.ToHexString(hash).ToLowerInvariant();
                request.Headers.Add("X-ZainX-Signature", $"sha256={signature}");
            }

            using var response = await _httpClient.SendAsync(request, ct);
            var statusCode = (int)response.StatusCode;
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            var isSuccess = response.IsSuccessStatusCode;
            return (isSuccess, statusCode, isSuccess ? "Delivered successfully" : responseBody);
        }
        catch (HttpRequestException ex)
        {
            return (false, (int)(ex.StatusCode ?? System.Net.HttpStatusCode.ServiceUnavailable), ex.Message);
        }
        catch (TaskCanceledException)
        {
            return (false, 408, "HTTP request timed out.");
        }
        catch (Exception ex)
        {
            return (false, 500, $"Delivery failure: {ex.Message}");
        }
    }
}
