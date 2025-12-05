using Microsoft.Extensions.Logging;
using ESP32Controller.Services;
using ESP32Controller.ViewModels;
using ESP32Controller.Views;

namespace ESP32Controller;

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

		// Registrar Services (Singleton)
		builder.Services.AddSingleton<ESP32HttpService>();
		builder.Services.AddSingleton<ESP32BleService>();
		builder.Services.AddSingleton<ConfiguracaoService>();
		
		// Registrar ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<WiFiControlViewModel>();
		builder.Services.AddTransient<BLEControlViewModel>();
		builder.Services.AddTransient<ConfiguracoesViewModel>();
		
		// Registrar Pages
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<WiFiControlPage>();
		builder.Services.AddTransient<BLEControlPage>();
		builder.Services.AddTransient<ConfiguracoesPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
