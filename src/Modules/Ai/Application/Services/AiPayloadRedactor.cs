using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Workforce.Modules.Ai.Application.Services;

/// <summary>
/// Closeout Gate 9/10: Redacts sensitive values from AI audit persistence.
/// Full-fidelity tool output remains available in-process for answer synthesis,
/// but what lands in ai.tool_executions / ai.source_references is minimized:
/// salaries, bank data, national IDs, resume/scorecard bodies and provider
/// secrets never persist in plaintext by default.
/// </summary>
public static class AiPayloadRedactor
{
    private static readonly Regex SensitiveKeyPattern = new(
        "(?i)(salary|netpay|grosspay|gross|net|iban|bankaccount|accountnumber|nationalid|national_id|ssn|" +
        "resume|scorecard|strengths|concerns|recommendation|secret|apikey|api_key|password|token|connectionstring|" +
        "earning|deduction|contribution|compensation|basicpay|basepay)",
        RegexOptions.Compiled);

    private const string RedactedMarker = "[REDACTED]";
    private const int MaxPersistedPayloadLength = 2000;

    public static string RedactJson(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return "{}";
        }

        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var redacted = RedactElement(doc.RootElement);
            var json = redacted.GetRawText();
            return json.Length > MaxPersistedPayloadLength
                ? json.Substring(0, MaxPersistedPayloadLength) + "...(truncated)"
                : json;
        }
        catch (JsonException)
        {
            // Non-JSON payloads are reduced to a safe length marker only.
            return "{\"redacted\":true,\"reason\":\"non-json payload\",\"length\":" + Math.Min(payloadJson!.Length, MaxPersistedPayloadLength) + "}";
        }
    }

    private static JsonElement RedactElement(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteElement(writer, element);
        }
        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    if (SensitiveKeyPattern.IsMatch(prop.Name))
                    {
                        writer.WritePropertyName(prop.Name);
                        writer.WriteStringValue(RedactedMarker);
                    }
                    else
                    {
                        writer.WritePropertyName(prop.Name);
                        WriteElement(writer, prop.Value);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }
}
