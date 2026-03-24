using System.Text.Json;

namespace OpenRouterBudget;

public sealed class HistoryManager
{
    private readonly string _historyPath;
    private readonly string _dashboardPath;

    public HistoryManager(string appDir)
    {
        _historyPath = Path.Combine(appDir, "history.json");
        _dashboardPath = Path.Combine(appDir, "dashboard.html");
    }

    public Dictionary<string, double> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                var json = File.ReadAllText(_historyPath);
                return JsonSerializer.Deserialize<Dictionary<string, double>>(json)
                    ?? new Dictionary<string, double>();
            }
        }
        catch { }
        return new Dictionary<string, double>();
    }

    private void SaveHistory(Dictionary<string, double> history)
    {
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_historyPath, json);
    }

    public void RecordDaily(double dailyUsage)
    {
        var history = LoadHistory();
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        history[today] = Math.Round(dailyUsage, 4);
        SaveHistory(history);
    }

    public List<(string Date, double Amount)> GetRecentDays(int n)
    {
        var history = LoadHistory();
        var results = new List<(string, double)>();
        for (int i = 0; i < n; i++)
        {
            string d = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
            history.TryGetValue(d, out double amt);
            results.Add((d, amt));
        }
        return results;
    }

    public void GenerateDashboard(BudgetData state)
    {
        var history = LoadHistory();

        var days = new List<string>();
        var amounts = new List<double>();
        double total30d = 0;
        int activeDays = 0;

        for (int i = 29; i >= 0; i--)
        {
            string d = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
            days.Add(d);
            history.TryGetValue(d, out double amt);
            amounts.Add(amt);
            total30d += amt;
            if (amt > 0) activeDays++;
        }

        double avgDaily = activeDays > 0 ? total30d / activeDays : 0;

        string remainingColor = "#6366f1";  // indigo

        // Build daily log rows
        var logRows = new System.Text.StringBuilder();
        for (int i = 0; i < days.Count; i++)
        {
            if (amounts[i] > 0)
            {
                logRows.AppendLine($"    <tr><td>{days[i]}</td><td style=\"text-align:right;\">${amounts[i]:F4}</td></tr>");
            }
        }

        // Short day labels for chart (MM-DD)
        var shortDays = days.Select(d => d.Substring(5)).ToList();
        var daysJson = JsonSerializer.Serialize(shortDays);
        var amountsJson = JsonSerializer.Serialize(amounts);

        string html = $@"<!DOCTYPE html>
<html><head>
<meta charset=""utf-8"">
<title>OpenRouter Budget</title>
<script src=""https://cdn.jsdelivr.net/npm/chart.js@4""></script>
<style>
  body {{ font-family: 'Segoe UI', system-ui, sans-serif; background: #0f0f0f; color: #e0e0e0; margin: 40px; }}
  .card {{ background: #1a1a2e; border-radius: 12px; padding: 24px; margin-bottom: 20px; max-width: 800px; }}
  .stat {{ font-size: 2.5em; font-weight: 700; }}
  .label {{ color: #888; font-size: 0.9em; margin-top: 4px; }}
  .grid {{ display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 20px; max-width: 800px; }}
  canvas {{ max-height: 300px; }}
</style></head><body>
<h1>OpenRouter Budget</h1>
<div class=""grid"">
  <div class=""card"">
    <div class=""stat"" style=""color:{remainingColor}"">${state.Remaining:F2}</div>
    <div class=""label"">remaining of ${state.TotalCredits:F2}</div>
  </div>
  <div class=""card"">
    <div class=""stat"">${state.Today:F4}</div>
    <div class=""label"">today's spend</div>
  </div>
  <div class=""card"">
    <div class=""stat"">${avgDaily:F4}</div>
    <div class=""label"">avg daily (active days)</div>
  </div>
</div>
<div class=""card"">
  <h2>30-Day Spend History</h2>
  <canvas id=""chart""></canvas>
</div>
<div class=""card"">
  <h2>Daily Log</h2>
  <table style=""width:100%; border-collapse:collapse;"">
    <tr style=""color:#888;""><th style=""text-align:left;"">Date</th><th style=""text-align:right;"">Spend</th></tr>
{logRows}
  </table>
</div>
<script>
new Chart(document.getElementById('chart'), {{
  type: 'bar',
  data: {{
    labels: {daysJson},
    datasets: [{{
      label: 'Daily Spend ($)',
      data: {amountsJson},
      backgroundColor: 'rgba(34,197,94,0.6)',
      borderRadius: 4,
    }}]
  }},
  options: {{
    scales: {{ y: {{ beginAtZero: true, ticks: {{ color: '#888' }} }}, x: {{ ticks: {{ color: '#888', maxRotation: 45 }} }} }},
    plugins: {{ legend: {{ display: false }} }}
  }}
}});
</script>
<p style=""color:#555; font-size:0.8em;"">Updated: {state.Updated}</p>
</body></html>";

        File.WriteAllText(_dashboardPath, html);
    }

    public string DashboardPath => _dashboardPath;
}
