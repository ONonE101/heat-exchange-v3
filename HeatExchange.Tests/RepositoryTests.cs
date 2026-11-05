using HeatExchange.Web.Models;
using HeatExchange.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace HeatExchange.Tests;

public sealed class RepositoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "HeatExchangeTests", Guid.NewGuid().ToString("N"));
    private readonly HeatExchangeCalculator calculator = new();
    private CalculationRepository Repository() => new(new TestEnvironment { ContentRootPath = directory }, new ConfigurationBuilder().Build());

    [Fact]
    public async Task SavedVariantSurvivesRepositoryRecreation()
    {
        var input = new HeatExchangeInput();
        var saved = await Repository().SaveAsync("Вариант 3", input, calculator.Calculate(input));
        var loaded = await Repository().GetAsync(saved.Id);
        Assert.NotNull(loaded);
        Assert.Equal(saved.Name, loaded.Name);
        Assert.Equal(saved.Result.MaterialOutlet, loaded.Result.MaterialOutlet);
        Assert.Equal(51, loaded.Result.Points.Count);
    }

    [Fact]
    public async Task ConcurrentSavesProduceSeparateCompleteFiles()
    {
        var repo = Repository();var input = new HeatExchangeInput();var result = calculator.Calculate(input);
        await Task.WhenAll(Enumerable.Range(1, 10).Select(i => repo.SaveAsync($"Вариант {i}", input, result)));
        var all = await repo.ListAsync();
        Assert.Equal(10, all.Count);
        Assert.Equal(10, all.Select(x => x.Id).Distinct().Count());
        Assert.All(all, x => Assert.Equal(51, x.Result.Points.Count));
    }

    [Fact]
    public async Task DeleteDoesNotAffectOtherVariants()
    {
        var repo = Repository();var input = new HeatExchangeInput();var result = calculator.Calculate(input);
        var a = await repo.SaveAsync("Первый", input, result);var b = await repo.SaveAsync("Второй", input, result);
        Assert.True(await repo.DeleteAsync(a.Id));Assert.Null(await repo.GetAsync(a.Id));
        Assert.NotNull(await repo.GetAsync(b.Id));Assert.Single(await repo.ListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyNameIsRejected(string name)
    {
        var input = new HeatExchangeInput();
        await Assert.ThrowsAsync<ArgumentException>(() => Repository().SaveAsync(name, input, calculator.Calculate(input)));
    }

    public void Dispose()
    {
        string resolved = Path.GetFullPath(directory), allowed = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "HeatExchangeTests")) + Path.DirectorySeparatorChar;
        if (resolved.StartsWith(allowed, StringComparison.OrdinalIgnoreCase) && Directory.Exists(resolved)) Directory.Delete(resolved, true);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
