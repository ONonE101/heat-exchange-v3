using System.Globalization;

namespace HeatExchange.Web.Models;

public sealed record ChartSeries(string Name, string Color, Func<TemperaturePoint, double> Value);

public sealed class TemperatureChartModel
{
    public required HeatExchangeResult Result { get; init; }
    public required string Title { get; init; }
    public required string YLabel { get; init; }
    public required string Id { get; init; }
    public bool IsDifference { get; init; }
    public List<ChartSeries> Series => IsDifference
        ? [new("Разность t − T", "#D06B24", p => p.Difference)]
        : [new("Окатыши", "#2F6FA6", p => p.Material), new("Воздух", "#D06B24", p => p.Gas)];
    public double XMax => Result.Points[^1].Height;
    public double YMin => Math.Min(0, Math.Floor(Series.Min(s => Result.Points.Min(s.Value)) / TickSize) * TickSize);
    public double YMax => Math.Max(TickSize, Math.Ceiling(Series.Max(s => Result.Points.Max(s.Value)) / TickSize) * TickSize);
    public double TickSize
    {
        get
        {
            double span = Math.Max(1, Series.Max(s => Result.Points.Max(s.Value)) - Math.Min(0, Series.Min(s => Result.Points.Min(s.Value))));
            double raw = span / 5, power = Math.Pow(10, Math.Floor(Math.Log10(raw))), fraction = raw / power;
            return (fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10) * power;
        }
    }
    public string X(double x) => N(58 + 608 * x / XMax);
    public string Y(double y) => N(218 - 174 * (y - YMin) / (YMax - YMin));
    public string Path(ChartSeries series) => string.Join(" ", Result.Points.Select((p, i) => $"{(i == 0 ? "M" : "L")}{X(p.Height)},{Y(series.Value(p))}"));
    public IEnumerable<TemperaturePoint> Markers => Result.Points.Where((_, i) => i % Math.Max(1, (Result.Points.Count - 1) / 10) == 0 || i == Result.Points.Count - 1);
    public static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
