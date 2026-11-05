namespace HeatExchange.Web.Models;

public sealed record TemperaturePoint(double Height, double Material, double Gas)
{
    public double Difference => Material - Gas;
}

public sealed record HeatExchangeResult(
    double Area,
    double GasVolumeFlow,
    double MaterialCapacityFlow,
    double GasCapacityFlow,
    double CapacityRatio,
    double RelativeHeight,
    double MaterialOutlet,
    double GasOutlet,
    double HeatPower,
    double Effectiveness,
    double BalanceResidual,
    List<TemperaturePoint> Points);
