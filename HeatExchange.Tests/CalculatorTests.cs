using System.ComponentModel.DataAnnotations;
using HeatExchange.Web.Models;
using HeatExchange.Web.Services;

namespace HeatExchange.Tests;

public sealed class CalculatorTests
{
    private readonly HeatExchangeCalculator calculator = new();

    [Fact]
    public void VariantThreeMatchesSpreadsheet()
    {
        var r = calculator.Calculate(new());
        Assert.Equal(20.432492642201396, r.MaterialOutlet, 8);
        Assert.Equal(464.98312729569477, r.GasOutlet, 8);
        Assert.Equal(1741595.607856566, r.HeatPower, 6);
        Assert.Equal(51, r.Points.Count);
        Assert.Equal(381.85703124575173, r.Points[5].Material, 8);
        Assert.True(Math.Abs(r.BalanceResidual) < 1e-7);
    }

    [Fact]
    public void ReferenceExampleIsReproduced()
    {
        var r = calculator.Calculate(new() { Height = 3, Diameter = 2, MaterialInlet = 600, GasInlet = 0, GasHeatCapacity = 1.31 });
        Assert.Equal(22.407122652261705, r.MaterialOutlet, 8);
        Assert.Equal(461.1272351441593, r.GasOutlet, 8);
    }

    [Fact]
    public void EqualCapacityFlowsHaveLinearProfiles()
    {
        var input = new HeatExchangeInput();
        input.MaterialFlow = input.GasVelocity * Math.PI * input.Diameter * input.Diameter / 4 * input.GasHeatCapacity / input.MaterialHeatCapacity;
        var r = calculator.Calculate(input);
        double middle = (input.MaterialInlet + r.MaterialOutlet) / 2;
        Assert.Equal(middle, r.Points[25].Material, 7);
        Assert.Equal(r.Points[0].Difference, r.Points[25].Difference, 7);
    }

    [Theory]
    [InlineData(0.01, 100000, 50)]
    [InlineData(1000, 100000, 50)]
    [InlineData(1.72, 1, 0.1)]
    [InlineData(2.064, 2460, 5)]
    public void ProfilesRemainFiniteAndRespectBoundaries(double flow, double coefficient, double height)
    {
        var input = new HeatExchangeInput { MaterialFlow = flow, VolumetricCoefficient = coefficient, Height = height };
        var r = calculator.Calculate(input);
        Assert.Equal(input.MaterialInlet, r.Points[0].Material);
        Assert.Equal(input.GasInlet, r.Points[^1].Gas);
        Assert.All(r.Points, p => {
            Assert.True(double.IsFinite(p.Material) && double.IsFinite(p.Gas));
            Assert.InRange(p.Material, input.GasInlet - 1e-6, input.MaterialInlet + 1e-6);
            Assert.InRange(p.Gas, input.GasInlet - 1e-6, input.MaterialInlet + 1e-6);
            Assert.True(p.Difference >= -1e-6);
        });
    }

    [Fact]
    public void EqualInletTemperaturesHaveNoHeatTransfer()
    {
        var r = calculator.Calculate(new() { MaterialInlet = 200, GasInlet = 200 });
        Assert.Equal(0, r.HeatPower);
        Assert.All(r.Points, p => { Assert.Equal(200, p.Material); Assert.Equal(200, p.Gas); });
    }

    [Fact]
    public void HeatingHasNegativeSignedHeatPower()
    {
        var r = calculator.Calculate(new() { MaterialInlet = 20, GasInlet = 700 });
        Assert.True(r.HeatPower < 0);
        Assert.All(r.Points, p => Assert.True(p.Difference <= 1e-8));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidFlowIsRejected(double flow) => Assert.Throws<ValidationException>(() => calculator.Calculate(new() { MaterialFlow = flow }));
}
