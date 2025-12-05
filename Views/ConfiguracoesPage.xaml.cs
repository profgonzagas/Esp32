using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class ConfiguracoesPage : ContentPage
{
    public ConfiguracoesPage(ConfiguracoesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
