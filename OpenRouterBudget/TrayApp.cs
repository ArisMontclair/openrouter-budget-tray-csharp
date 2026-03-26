namespace OpenRouterBudget;

public sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly BudgetService _budgetService;
    private readonly HistoryManager _history;
    private readonly string _appDir;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    private BudgetData _state = new();
    private ToolStripMenuItem? _todayItem;
    private ToolStripMenuItem? _weekItem;
    private ToolStripMenuItem? _monthItem;
    private ToolStripMenuItem? _updatedItem;
    private ToolStripMenuItem? _headerItem;
    private ToolStripMenuItem? _historyHeaderItem;
    private List<ToolStripMenuItem> _historyItems = new();

    public TrayApp(string apiKey, string appDir)
    {
        _appDir = appDir;
        _budgetService = new BudgetService(apiKey);
        _history = new HistoryManager(appDir);

        _trayIcon = new NotifyIcon
        {
            Text = "OpenRouter Budget",
            Visible = true
        };

        // Build context menu
        BuildMenu();

        // Initial fetch
        RefreshBudget();

        // Auto-refresh every 2 minutes
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 120_000 };
        _refreshTimer.Tick += (_, _) => RefreshBudget();
        _refreshTimer.Start();
    }

    private void BuildMenu()
    {
        var menu = new ContextMenuStrip();

        _headerItem = new ToolStripMenuItem("Loading...") { Enabled = false };
        menu.Items.Add(_headerItem);
        menu.Items.Add(new ToolStripSeparator());

        _todayItem = new ToolStripMenuItem("  Today:   $0.0000") { Enabled = false };
        _weekItem = new ToolStripMenuItem("  Week:    $0.0000") { Enabled = false };
        _monthItem = new ToolStripMenuItem("  Month:   $0.0000") { Enabled = false };
        menu.Items.Add(_todayItem);
        menu.Items.Add(_weekItem);
        menu.Items.Add(_monthItem);

        menu.Items.Add(new ToolStripSeparator());

        _historyHeaderItem = new ToolStripMenuItem("  -- Last 7 Days --") { Enabled = false };
        menu.Items.Add(_historyHeaderItem);

        // Placeholder history items (updated on refresh)
        RefreshHistoryMenuItems(menu);

        menu.Items.Add(new ToolStripSeparator());
        _updatedItem = new ToolStripMenuItem("  Updated: never") { Enabled = false };
        menu.Items.Add(_updatedItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Refresh Now", null, (_, _) => RefreshBudget()));
        menu.Items.Add(new ToolStripMenuItem("View 30-Day History", null, (_, _) => OpenDashboard()));
        menu.Items.Add(new ToolStripMenuItem("OpenRouter Dashboard", null, (_, _) =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://openrouter.ai/settings/credits",
                UseShellExecute = true
            })));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => Exit()));

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => OpenDashboard();
    }

    private void RefreshHistoryMenuItems(ContextMenuStrip menu)
    {
        // Remove old history items
        foreach (var item in _historyItems)
            menu.Items.Remove(item);
        _historyItems.Clear();

        var recent = _history.GetRecentDays(7);
        int insertIndex = menu.Items.IndexOf(_historyHeaderItem!) + 1;

        foreach (var (date, amount) in recent)
        {
            var item = new ToolStripMenuItem($"  {date}:  ${amount:F4}") { Enabled = false };
            menu.Items.Insert(insertIndex, item);
            _historyItems.Add(item);
            insertIndex++;
        }
    }

    private async void RefreshBudget()
    {
        _state = await _budgetService.FetchAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_state.Error != null)
        {
            SetTrayIcon(TrayIconRenderer.CreateErrorIcon());
            _trayIcon.Text = $"OpenRouter Budget\nError: {_state.Error[..Math.Min(50, _state.Error.Length)]}";
            _headerItem!.Text = $"Error: {_state.Error[..Math.Min(40, _state.Error.Length)]}";
        }
        else
        {
            SetTrayIcon(TrayIconRenderer.CreateBudgetIcon(_state.Today));
            _trayIcon.Text = $"Remaining: ${_state.Remaining:F2} / ${_state.TotalCredits:F2}\nToday: ${_state.Today:F4}";
            _headerItem!.Text = $"  ${_state.Remaining:F2} left of ${_state.TotalCredits:F2}";

            // Record to history
            _history.RecordDaily(_state.Today);
            _history.GenerateDashboard(_state);

            // Update history menu items
            if (_trayIcon.ContextMenuStrip != null)
                RefreshHistoryMenuItems(_trayIcon.ContextMenuStrip);
        }

        _todayItem!.Text = $"  Today:   ${_state.Today:F4}";
        _weekItem!.Text = $"  Week:    ${_state.Weekly:F4}";
        _monthItem!.Text = $"  Month:   ${_state.Monthly:F4}";
        _updatedItem!.Text = $"  Updated: {_state.Updated}";
    }

    private void OpenDashboard()
    {
        _history.GenerateDashboard(_state);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = _history.DashboardPath,
            UseShellExecute = true
        });
    }

    private void Exit()
    {
        _refreshTimer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Application.Exit();
    }

    private void SetTrayIcon(Icon icon)
    {
        Icon? previous = _trayIcon.Icon;
        _trayIcon.Icon = icon;
        previous?.Dispose();
    }

}
