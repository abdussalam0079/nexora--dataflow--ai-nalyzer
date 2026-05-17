using DataFlow.Core.Enums;
using DataFlow.Core.Interfaces;
using DataFlow.Core.Navigation;
using DataFlow.Core.Themes;
using DataFlow.UI.Controls;
using DataFlow.UI.Services;
using LiveChartsCore.SkiaSharpView.WinForms;
using Microsoft.Extensions.DependencyInjection;

namespace DataFlow.UI.Views;

public sealed class RealtimeView : UserControl, INavigationAware
{
    private readonly IDataFlowApiClient _api;
    private readonly INavigationService _navigation;
    private readonly RealtimeStreamService _stream = new();
    private readonly CartesianChart _chart;
    private readonly ComboBox _streamType;
    private readonly Label _status;
    private int _projectId;

    public RealtimeView(IServiceProvider services)
    {
        _api = services.GetRequiredService<IDataFlowApiClient>();
        _navigation = services.GetRequiredService<INavigationService>();
        BackColor = Color.FromArgb(17, 19, 24);
        Dock = DockStyle.Fill;

        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(26, 29, 40), Padding = new Padding(12) };
        var back = new Button { Text = "← Back", FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke, AutoSize = true };
        back.Click += (_, _) => _navigation.Navigate(NavigationArgs.For(AppView.ProjectDetail, _projectId));
        _streamType = new ComboBox { Location = new Point(120, 12), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        var start = new Button { Text = "Start stream", Location = new Point(340, 10), Size = new Size(100, 30), FlatStyle = FlatStyle.Flat, BackColor = DesignTokens.Accent, ForeColor = Color.White };
        start.FlatAppearance.BorderSize = 0;
        start.Click += async (_, _) => await StartStreamAsync();
        var stop = new Button { Text = "Stop", Location = new Point(450, 10), Size = new Size(72, 30), FlatStyle = FlatStyle.Flat, ForeColor = Color.WhiteSmoke };
        stop.Click += async (_, _) => { await _stream.StopAsync(); _status.Text = "Stopped"; };
        header.Controls.AddRange([back, _streamType, start, stop]);

        _status = new Label { Dock = DockStyle.Top, Height = 24, ForeColor = DesignTokens.TextDim, Padding = new Padding(16, 0, 0, 0) };

        _chart = new CartesianChart { Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 19, 24) };

        Controls.Add(_chart);
        Controls.Add(_status);
        Controls.Add(header);
    }

    public void OnNavigatedTo(NavigationArgs args)
    {
        if (args.ProjectId is int pid) _projectId = pid;
        _ = LoadTypesAsync();
    }

    private async Task LoadTypesAsync()
    {
        try
        {
            var types = await _api.GetRealtimeStreamTypesAsync();
            _streamType.Items.Clear();
            foreach (var key in types.Keys)
                _streamType.Items.Add(key);
            if (_streamType.Items.Count > 0)
                _streamType.SelectedIndex = 0;
            _status.Text = "Select a stream type and click Start.";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
        }
    }

    private async Task StartStreamAsync()
    {
        if (_streamType.SelectedItem is not string type) return;
        _status.Text = $"Connecting to {type}…";
        try
        {
            await _stream.StartAsync(type, "ws://127.0.0.1:8000/api/v1/enterprise", _chart);
            _status.Text = $"Live — {type}";
        }
        catch (Exception ex)
        {
            _status.Text = $"Connection failed: {ex.Message}";
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _stream.Dispose();
        base.Dispose(disposing);
    }
}
