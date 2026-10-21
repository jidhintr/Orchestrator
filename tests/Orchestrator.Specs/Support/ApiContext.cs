using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchestrator.Specs.Support;

public sealed partial class ApiContext
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public string Unique { get; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    public bool UseBrokenDatabase { get; set; }

    public string? CorrelationId { get; set; }

    public string? ActingUserId { get; set; } = "1";

    public Dictionary<string, string> Aliases { get; } = new(StringComparer.OrdinalIgnoreCase) { ["user:SA"] = "1", ["user:Admin"] = "2" };

    private static readonly Dictionary<string, string> SeedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dept:SUPER_ADMIN"] = "1",
        ["dept:ADMIN"] = "2",
        ["dept:ACCOUNTS"] = "3",
        ["dept:QA"] = "4",
        ["dept:DELIVERY"] = "5",
        ["dept:CLIENTS"] = "6",
        ["dept:UNASSIGNED"] = "7",
        ["role:Super Admin"] = "1",
        ["role:Admin"] = "2",
        ["role:Manager"] = "3",
        ["role:Employee"] = "4",
        ["role:Client"] = "5",
    };

    public string? LastId { get; set; }

    public HttpStatusCode Status { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string? ContentType { get; private set; }

    public JsonElement? Json { get; private set; }

    public IReadOnlyDictionary<string, string> Headers => _headers;

    public string Expand(string text) =>
        Placeholder().Replace(
            text.Replace("{unique}", Unique, StringComparison.Ordinal).Replace("{id}", LastId ?? "<no id captured>", StringComparison.Ordinal),
            m => Aliases.TryGetValue(m.Groups[1].Value + ":" + m.Groups[2].Value, out var id) ? id : SeedIds.GetValueOrDefault(m.Groups[1].Value + ":" + m.Groups[2].Value, m.Value));

    [GeneratedRegex(@"{(dept|role|user):([^{}]+)}")]
    private static partial Regex Placeholder();

    public Task SendAsync(string method, string path, string? body = null) => SendAsync(method, path, body, ActingUserId);

    public Task SendAsSuperAdminAsync(string method, string path, string? body = null) => SendAsync(method, path, body, "1");

    private async Task SendAsync(string method, string path, string? body, string? actor)
    {
        using var client = ApiHost.CreateClient(UseBrokenDatabase);
        using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), Expand(path));
        if (actor is not null)
        {
            request.Headers.Add("X-Acting-User-Id", Expand(actor));
        }

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
