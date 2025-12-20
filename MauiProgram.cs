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
		
		// Adicionar handler global de exceções
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			System.Diagnostics.Debug.WriteLine($"[CRASH] UnhandledException: {args.ExceptionObject}");
		};
		
		System.Diagnostics.Debug.WriteLine("[MAUI] MauiProgram.CreateMauiApp iniciado");
		
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		try
		{
			System.Diagnostics.Debug.WriteLine("[MAUI] Registrando Services...");
			
			// Registrar Services (Singleton)
			builder.Services.AddSingleton<ESP32HttpService>();
			builder.Services.AddSingleton<ESP32BleService>();
			builder.Services.AddSingleton<ConfiguracaoService>();
			
			System.Diagnostics.Debug.WriteLine("[MAUI] Services registrados com sucesso");
			System.Diagnostics.Debug.WriteLine("[MAUI] Registrando ViewModels...");
			
			// Registrar ViewModels
			builder.Services.AddTransient<DashboardViewModel>();
			builder.Services.AddTransient<WiFiControlViewModel>();
			builder.Services.AddTransient<BLEControlViewModel>();
			builder.Services.AddTransient<ConfiguracoesViewModel>();
			
			System.Diagnostics.Debug.WriteLine("[MAUI] ViewModels registrados com sucesso");
			System.Diagnostics.Debug.WriteLine("[MAUI] Registrando Pages...");
			
			// Registrar Pages
			builder.Services.AddTransient<DashboardPage>();
			builder.Services.AddTransient<WiFiControlPage>();
			builder.Services.AddTransient<BLEControlPage>();
			builder.Services.AddTransient<ConfiguracoesPage>();
			
			System.Diagnostics.Debug.WriteLine("[MAUI] Pages registradas com sucesso");

#if DEBUG
			builder.Logging.AddDebug();
#endif

			System.Diagnostics.Debug.WriteLine("[MAUI] Construindo MauiApp...");
			var app = builder.Build();
			System.Diagnostics.Debug.WriteLine("[MAUI] MauiApp construído com sucesso!");
			
			return app;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CRASH] Erro ao criar MauiApp: {ex.GetType().Name}");
			System.Diagnostics.Debug.WriteLine($"[CRASH] Mensagem: {ex.Message}");
			System.Diagnostics.Debug.WriteLine($"[CRASH] Stack: {ex.StackTrace}");
			throw;
		}
	}
}
