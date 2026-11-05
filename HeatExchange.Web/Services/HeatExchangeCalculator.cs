using System.ComponentModel.DataAnnotations;
using HeatExchange.Web.Models;

namespace HeatExchange.Web.Services;

/// <summary>Аналитическое решение стационарного противоточного теплообмена.</summary>
public sealed class HeatExchangeCalculator
{
    public HeatExchangeResult Calculate(HeatExchangeInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);
        double area = Math.PI * input.Diameter * input.Diameter / 4;
        double gasFlow = input.GasVelocity * area;
        double wm = input.MaterialFlow * input.MaterialHeatCapacity * 1000;
        double wg = gasFlow * input.GasHeatCapacity * 1000;
        double ua = input.VolumetricCoefficient * area * input.Height;
        double cmin = Math.Min(wm, wg), cr = cmin / Math.Max(wm, wg), ntu = ua / cmin;
        double difference = 1 - cr;
        double numerator = -ExpMinusOne(-ntu * difference);
        double effectiveness = difference == 0 ? ntu / (1 + ntu) : numerator / (difference + cr * numerator);
        double power = effectiveness * cmin * (input.MaterialInlet - input.GasInlet);
        double materialOut = input.MaterialInlet - power / wm;
        double gasOut = input.GasInlet + power / wg;
        double transferPerMetre = input.VolumetricCoefficient * area;
        double rate = transferPerMetre * (1 / wg - 1 / wm);
        var points = new List<TemperaturePoint>(input.Intervals + 1);

        for (int i = 0; i <= input.Intervals; i++)
        {
            double y = input.Height * i / input.Intervals;
            double t, delta;
            // Выбираем границу, при которой показатель экспоненты неположителен.
            // Это исключает переполнение при m > 1 и больших коэффициентах теплоотдачи.
            if (rate <= 0)
            {
                double topDifference = input.MaterialInlet - gasOut;
                t = input.MaterialInlet - transferPerMetre / wm * topDifference * y * Phi(rate * y);
                delta = topDifference * Math.Exp(rate * y);
            }
            else
            {
                double distance = input.Height - y, bottomDifference = materialOut - input.GasInlet;
                t = materialOut + transferPerMetre / wm * bottomDifference * distance * Phi(-rate * distance);
                delta = bottomDifference * Math.Exp(-rate * distance);
            }
            double gas = t - delta;
            if (i == 0) { t = input.MaterialInlet; gas = gasOut; }
            if (i == input.Intervals) { t = materialOut; gas = input.GasInlet; }
            points.Add(new TemperaturePoint(y, t, gas));
        }

        double materialPower = wm * (input.MaterialInlet - materialOut);
        double gasPower = wg * (gasOut - input.GasInlet);
        return new HeatExchangeResult(area, gasFlow, wm, wg, wm / wg, ua / wm,
            materialOut, gasOut, power, effectiveness, materialPower - gasPower, points);
    }

    private static double ExpMinusOne(double x) => Math.Abs(x) < 1e-5
        ? x * (1 + x * (0.5 + x * (1.0 / 6 + x * (1.0 / 24 + x / 120))))
        : Math.Exp(x) - 1;

    private static double Phi(double x) => x == 0 ? 1 : ExpMinusOne(x) / x;
}
