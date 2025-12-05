namespace ESP32Controller;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		
		// Definir tema escuro como padrão
		UserAppTheme = Preferences.Get("tema_escuro", true) ? AppTheme.Dark : AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}