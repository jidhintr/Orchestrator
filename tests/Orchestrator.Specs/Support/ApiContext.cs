using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
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

    public Dictionary<string, string> Aliases { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user:SA"] = "1",
        ["user:Admin"] = ApiHost.AdminId.ToString(CultureInfo.InvariantCulture),
    };

    private static long _mobile = Random.Shared.NextInt64(1_000_000_000, 8_000_000_000);

    public static string NextMobile() => Interlocked.Increment(ref _mobile).ToString(CultureInfo.InvariantCulture);

    private static readonly Dictionary<string, string> SeedIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dept:ADMINISTRATIVES"] = "2",
        ["dept:ACCOUNTING"] = "3",
        ["dept:QUALITY_CHECK"] = "4",
        ["dept:CUSTOMERS"] = "6",
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
            Mobile().Replace(text.Replace("{unique}", Unique, StringComparison.Ordinal).Replace("{id}", LastId ?? "<no id captured>", StringComparison.Ordinal), _ => NextMobile()),
            m => Aliases.TryGetValue(m.Groups[1].Value + ":" + m.Groups[2].Value, out var id) ? id : SeedIds.GetValueOrDefault(m.Groups[1].Value + ":" + m.Groups[2].Value, m.Value));

    [GeneratedRegex(@"{(dept|role|user|catalog):([^{}]+)}")]
    private static partial Regex Placeholder();

    [GeneratedRegex("{mobile}")]
    private static partial Regex Mobile();

    public Task SendAsync(string method, string path, string? body = null) => SendAsync(method, path, body, ActingUserId);

    public Task SendAsSuperAdminAsync(string method, string path, string? body = null) => SendAsync(method, path, body, "1");

    private async Task SendAsync(string method, string path, string? body, string? actor)
    {
        using var client = ApiHost.CreateClient(UseBrokenDatabase);
        using var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), Expand(path));
        if (actor is not null && int.TryParse(Expand(actor), CultureInfo.InvariantCulture, out var actorId) && await ApiHost.TokenAsync(actorId) is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
