using System.Globalization;
using HeatExchange.Web.Infrastructure;
using HeatExchange.Web.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new FlexibleDoubleBinderProvider());
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(_ => "Заполните это поле.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(_ => "Введите число.");
});
builder.Services.AddSingleton<HeatExchangeCalculator>();
builder.Services.AddSingleton<CalculationRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("ru-RU").AddSupportedCultures("ru-RU").AddSupportedUICultures("ru-RU"));
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Calculator}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
