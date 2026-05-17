using System.Net.WebSockets;
using System.Text;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace DataFlow.UI.Services;

public sealed class RealtimeStreamService : IDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private readonly List<ObservablePoint> _points = [];
    private const int MaxPoints = 60;

    public event Action<CartesianChart>? ChartUpdated;

    public async Task StartAsync(string streamType, string baseWsUrl, CartesianChart chart, CancellationToken outerCt = default)
    {
        await StopAsync();
        _points.Clear();
        chart.Series = [];

        _cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        _socket = new ClientWebSocket();
        var uri = new Uri($"{baseWsUrl.TrimEnd('/')}/realtime/{streamType}?interval_ms=1000");
        await _socket.ConnectAsync(uri, _cts.Token);

        _ = Task.Run(async () =>
        {
            var buffer = new byte[8192];
            while (_socket?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var tick = JObject.Parse(json);
                var value = tick["value"]?.Value<double>() ?? tick["y"]?.Value<double>() ?? 0;
                _points.Add(new ObservablePoint(_points.Count, value));
                if (_points.Count > MaxPoints) _points.RemoveAt(0);
                for (var i = 0; i < _points.Count; i++) _points[i] = new ObservablePoint(i, _points[i].Y);

                chart.Invoke(() =>
                {
                    chart.Series =
                    [
                        new LineSeries<ObservablePoint>
                        {
                            Values = _points.ToArray(),
                            Fill = new SolidColorPaint(new SKColor(99, 102, 241, 40)),
                            Stroke = new SolidColorPaint(new SKColor(99, 102, 241), 2),
                            GeometryFill = null,
                            GeometryStroke = null
                        }
                    ];
                    ChartUpdated?.Invoke(chart);
                });
            }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        if (_socket?.State == WebSocketState.Open)
        {
            try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None); }
            catch { /* ignore */ }
        }
        _socket?.Dispose();
        _socket = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => _ = StopAsync();
}
