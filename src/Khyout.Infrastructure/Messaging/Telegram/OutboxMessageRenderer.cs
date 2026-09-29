using System.Text.Json;
using Khyout.Domain.Enums;

namespace Khyout.Infrastructure.Messaging.Telegram;

/// <summary>Renders outbox payloads into plain notification texts (Phase 4 may upgrade to rich templates).</summary>
public static class OutboxMessageRenderer
{
    public static string Render(OutboxMessageType type, string payloadJson)
    {
        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(payloadJson).RootElement;
        }
        catch (JsonException)
        {
            return $"[{type}]";
        }

        string Get(string name) =>
            payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty(name, out var value)
                ? value.ToString()
                : string.Empty;

        return type switch
        {
            OutboxMessageType.RfqCreated =>
                $"New RFQ {Short(Get("rfqId"))}: {Get("title")} — suppliers in the category can now bid.",
            OutboxMessageType.QuotationSubmitted =>
                $"New bid received on RFQ {Short(Get("rfqId"))}.",
            OutboxMessageType.QuotationAccepted =>
                $"Your bid on RFQ {Short(Get("rfqId"))} was accepted.",
            OutboxMessageType.QuotationRejected =>
                $"A bid on RFQ {Short(Get("rfqId"))} was not selected.",
            OutboxMessageType.QuotationExpired =>
                $"A bid on RFQ {Short(Get("rfqId"))} has expired.",
            OutboxMessageType.SampleRequested =>
                $"New sample request {Short(Get("sampleId"))}.",
            OutboxMessageType.SampleStatusChanged =>
                $"Sample request {Short(Get("sampleId"))} is now '{Get("status")}'.",
            _ => $"[{type}]"
        };
    }

    private static string Short(string id) => id.Length <= 8 ? id : id[..8];
}
