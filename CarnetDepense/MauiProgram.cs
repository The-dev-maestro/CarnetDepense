using Microsoft.Extensions.Logging;
using CarnetDepenses.Services;
using CarnetDepenses.ViewModels;

namespace CarnetDepense;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Phase A : Repository local JSON
        builder.Services.AddSingleton<IDepenseRepository, JsonDepenseRepository>();

        // Phase B : API Service avec faux serveur hors ligne
        builder.Services.AddSingleton(new HttpClient(new FakeApiHandler()));
        builder.Services.AddSingleton<IApiService, ApiService>();

        builder.Services.AddSingleton<DepensesViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
