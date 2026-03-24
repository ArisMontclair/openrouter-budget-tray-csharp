using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenRouterBudget;

public sealed class BudgetData
{
    [JsonPropertyName("remaining")]
    public double Remaining { get; set; }

    [JsonPropertyName("total_credits")]
    public double TotalCredits { get; set; }

    [JsonPropertyName("total_usage")]
    public double TotalUsage { get; set; }

    [JsonPropertyName("today")]
    public double Today { get; set; }

    [JsonPropertyName("weekly")]
    public double Weekly { get; set; }

    [JsonPropertyName("monthly")]
    public double Monthly { get; set; }

    [JsonPropertyName("updated")]
    public string Updated { get; set; } = "never";

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class BudgetService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public BudgetService(string apiKey)
    {
        _apiKey = apiKey;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<BudgetData> FetchAsync()
    {
        try
        {
            var keyTask = _http.GetStringAsync("https://openrouter.ai/api/v1/auth/key");
            var creditsTask = _http.GetStringAsync("https://openrouter.ai/api/v1/credits");

            await Task.WhenAll(keyTask, creditsTask);

            using var keyDoc = JsonDocument.Parse(await keyTask);
            using var creditsDoc = JsonDocument.Parse(await creditsTask);

            var keyData = keyDoc.RootElement.GetProperty("data");
            var creditsData = creditsDoc.RootElement.GetProperty("data");

            double totalCredits = creditsData.GetProperty("total_credits").GetDouble();
            double totalUsage = creditsData.GetProperty("total_usage").GetDouble();

            return new BudgetData
            {
                Remaining = Math.Round(totalCredits - totalUsage, 2),
                TotalCredits = totalCredits,
                TotalUsage = Math.Round(totalUsage, 4),
                Today = Math.Round(keyData.GetProperty("usage_daily").GetDouble(), 4),
                Weekly = Math.Round(keyData.GetProperty("usage_weekly").GetDouble(), 4),
                Monthly = Math.Round(keyData.GetProperty("usage_monthly").GetDouble(), 4),
                Updated = DateTime.Now.ToString("HH:mm:ss"),
                Error = null
            };
        }
        catch (Exception ex)
        {
            return new BudgetData
            {
                Updated = DateTime.Now.ToString("HH:mm:ss"),
                Error = ex.Message
            };
        }
    }
}
