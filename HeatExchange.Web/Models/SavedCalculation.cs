namespace HeatExchange.Web.Models;

public sealed record SavedCalculation(Guid Id, string Name, DateTimeOffset CreatedUtc,
    HeatExchangeInput Input, HeatExchangeResult Result, int ModelVersion = 1);
