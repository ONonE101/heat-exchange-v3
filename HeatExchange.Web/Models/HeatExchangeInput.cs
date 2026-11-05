using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HeatExchange.Web.Models;

public sealed class HeatExchangeInput : IValidatableObject
{
    [BindRequired, InputRange(0.1, 50), Display(Name = "Высота слоя", Description = "м")]
    public double Height { get; set; } = 5;

    [BindRequired, InputRange(-100, 2000), Display(Name = "Температура окатышей на входе", Description = "°C")]
    public double MaterialInlet { get; set; } = 700;

    [BindRequired, InputRange(-100, 2000), Display(Name = "Температура воздуха на входе", Description = "°C")]
    public double GasInlet { get; set; } = 20;

    [BindRequired, InputRange(0.01, 20), Display(Name = "Скорость воздуха", Description = "м/с")]
    public double GasVelocity { get; set; } = 0.78;

    [BindRequired, InputRange(0.1, 10), Display(Name = "Теплоёмкость воздуха", Description = "кДж/(м³·К)")]
    public double GasHeatCapacity { get; set; } = 1.32;

    [BindRequired, InputRange(0.01, 1000), Display(Name = "Расход окатышей", Description = "кг/с")]
    public double MaterialFlow { get; set; } = 1.72;

    [BindRequired, InputRange(0.1, 10), Display(Name = "Теплоёмкость окатышей", Description = "кДж/(кг·К)")]
    public double MaterialHeatCapacity { get; set; } = 1.49;

    [BindRequired, InputRange(1, 100000), Display(Name = "Объёмный коэффициент теплоотдачи", Description = "Вт/(м³·К)")]
    public double VolumetricCoefficient { get; set; } = 2460;

    [BindRequired, InputRange(0.1, 20), Display(Name = "Диаметр аппарата", Description = "м")]
    public double Diameter { get; set; } = 2.2;

    [BindRequired, InputRange(10, 500), Display(Name = "Число интервалов")]
    public int Intervals { get; set; } = 50;

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var property in GetType().GetProperties().Where(p => p.PropertyType == typeof(double)))
            if (!double.IsFinite((double)property.GetValue(this)!))
                yield return new ValidationResult("Введите конечное число.", [property.Name]);
    }
}
