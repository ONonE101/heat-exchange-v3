namespace HeatExchange.Web.Models;

public sealed class CalculatorPageModel
{
    public HeatExchangeInput Input { get; set; } = new();
    public HeatExchangeResult? Result { get; set; }
    public string Name { get; set; } = "Вариант 3";
    public string? Notice { get; set; }
    public Guid? SavedId { get; set; }
}
