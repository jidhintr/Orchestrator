using System.Net;
using System.Text;
using System.Text.Json;

namespace Orchestrator.Specs.Support;

public sealed class ApiContext
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public string Unique { get; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    public bool UseBrokenDatabase { get; set; }

    public string? CorrelationId { get; set; }

    public string? LastId { get; set; }

    public HttpStatusCode Status { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string? ContentType { get; private set; }

    public JsonElement? Json { get; private set; }

    public IReadOnlyDictionary<string, string> Headers => _headers;

    public string Expand(string text) =>
        text.Replace("{unique}", Unique, StringComparison.Ordinal)
            .Replace("{id}", LastId ?? "<no id captured>", StringComparison.Ordinal);

    public async Task SendAsync(string method, string path, string? body = null)
    {
        using var client = ApiHost.CreateClient(UseBrokenDatabase);
        using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), Expand(path));
        if (CorrelationId is not null)
        {
            request.Headers.Add("X-Correlation-Id", CorrelationId);
        }

        if (body is not null)
        {
            request.Content = new StringContent(Expand(body), Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        Status = response.StatusCode;
        Body = await response.Content.ReadAsStringAsync();
        ContentType = response.Content.Headers.ContentType?.MediaType;

        _headers.Clear();
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            _headers[header.Key] = string.Join(",", header.Value);
        }

        Json = TryParse(Body);
        if (Json is { ValueKind: JsonValueKind.Object } root && root.TryGetProperty("id", out var id) && id.ValueKind is JsonValueKind.Number or JsonValueKind.String)
        {
            LastId = id.ToString();
        }
    }

    private static JsonElement? TryParse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(body).RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
