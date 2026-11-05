using System.Text.Json;
using HeatExchange.Web.Models;

namespace HeatExchange.Web.Services;

/// <summary>Локальное постоянное хранилище. Каждый вариант находится в отдельном JSON-файле.</summary>
public sealed class CalculationRepository(IWebHostEnvironment environment, IConfiguration configuration)
{
    private readonly string directory = configuration["Storage:Directory"] ?? Path.Combine(environment.ContentRootPath, "App_Data", "Calculations");
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<SavedCalculation> SaveAsync(string name, HeatExchangeInput input, HeatExchangeResult result, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            throw new ArgumentException("Название должно содержать от 1 до 80 символов.", nameof(name));
        var saved = new SavedCalculation(Guid.NewGuid(), name.Trim(), DateTimeOffset.UtcNow, input, result);
        await writeLock.WaitAsync(ct);
        string temp = Path.Combine(directory, saved.Id.ToString("N") + ".tmp");
        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(saved, JsonOptions), ct);
            File.Move(temp, FilePath(saved.Id));
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
            writeLock.Release();
        }
        return saved;
    }

    public async Task<SavedCalculation?> GetAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            string text = await File.ReadAllTextAsync(FilePath(id), ct);
            return JsonSerializer.Deserialize<SavedCalculation>(text, JsonOptions)
                ?? throw new InvalidDataException("Файл сохранённого расчёта пуст.");
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (JsonException error) { throw new InvalidDataException("Не удалось прочитать сохранённый расчёт.", error); }
    }

    public async Task<IReadOnlyList<SavedCalculation>> ListAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(directory)) return [];
        var result = new List<SavedCalculation>();
        foreach (string path in Directory.EnumerateFiles(directory, "*.json"))
            if (Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id))
            {
                var item = await GetAsync(id, ct);
                if (item is not null) result.Add(item);
            }
        return result.OrderByDescending(x => x.CreatedUtc).ToList();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await writeLock.WaitAsync(ct);
        try
        {
            string path = FilePath(id);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        finally { writeLock.Release(); }
    }

    private string FilePath(Guid id) => Path.Combine(directory, id.ToString("N") + ".json");
}
