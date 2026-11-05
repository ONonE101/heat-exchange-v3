using System.Globalization;
using System.Text;
using HeatExchange.Web.Models;
using HeatExchange.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HeatExchange.Web.Controllers;

public sealed class CalculatorController(HeatExchangeCalculator calculator, CalculationRepository repository) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(Guid? load, bool saved = false, CancellationToken ct = default)
    {
        if (load is null)
        {
            var input = new HeatExchangeInput();
            return View(new CalculatorPageModel { Input = input, Result = calculator.Calculate(input) });
        }
        var item = await repository.GetAsync(load.Value, ct);
        if (item is null) return NotFound("Расчёт не найден.");
        return View(new CalculatorPageModel { Input = item.Input, Result = item.Result, Name = item.Name,
            SavedId = item.Id, Notice = saved ? "Расчёт сохранён. Его можно открыть на странице сохранённых вариантов." : "Открыт сохранённый вариант. Измените данные и выполните новый расчёт." });
    }

    [HttpPost("calculate")]
    public IActionResult Calculate(HeatExchangeInput input, string? name)
    {
        var page = new CalculatorPageModel { Input = input, Name = name ?? "Вариант 3" };
        if (ModelState.IsValid) page.Result = calculator.Calculate(input);
        return View("Index", page);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(HeatExchangeInput input, string? name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            ModelState.AddModelError("Name", "Введите название расчёта: от 1 до 80 символов.");
        var page = new CalculatorPageModel { Input = input, Name = name ?? "" };
        if (!ModelState.IsValid) return View("Index", page);
        var result = calculator.Calculate(input);
        try
        {
            var item = await repository.SaveAsync(name!, input, result, ct);
            return RedirectToAction(nameof(Index), new { load = item.Id, saved = true });
        }
        catch (IOException)
        {
            ModelState.AddModelError("", "Не удалось сохранить расчёт. Проверьте доступ к папке App_Data.");
            page.Result = result;
            return View("Index", page);
        }
    }

    [HttpGet("calculations")]
    public async Task<IActionResult> Saved(CancellationToken ct) => View(await repository.ListAsync(ct));

    [HttpPost("calculations/{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (!await repository.DeleteAsync(id, ct)) return NotFound("Расчёт не найден.");
        return RedirectToAction(nameof(Saved));
    }

    [HttpPost("export")]
    public IActionResult Export(HeatExchangeInput input, string? name)
    {
        if (!ModelState.IsValid) return View("Index", new CalculatorPageModel { Input = input, Name = name ?? "Вариант 3" });
        return Csv(calculator.Calculate(input));
    }

    [HttpGet("calculations/{id:guid}/csv")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var item = await repository.GetAsync(id, ct);
        return item is null ? NotFound("Расчёт не найден.") : Csv(item.Result);
    }

    private FileContentResult Csv(HeatExchangeResult result)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        var csv = new StringBuilder("y, м;Окатыши, °C;Воздух, °C;Разность, К\r\n");
        foreach (var p in result.Points)
            csv.AppendLine(string.Join(';', new[] { p.Height, p.Material, p.Gas, p.Difference }.Select(v => v.ToString("0.000000", culture))));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "В-3_Шарапов_Демьян_С_НМТ-343901.csv");
    }
}
