namespace DataFlow.UI.Helpers;

public sealed class ChartDrillDownEventArgs : EventArgs
{
    public required string Column { get; init; }
    public required string Value { get; init; }
    public string? WidgetId { get; init; }
}
