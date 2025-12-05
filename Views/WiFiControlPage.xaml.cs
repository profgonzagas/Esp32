using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class WiFiControlPage : ContentPage
{
    public WiFiControlPage(WiFiControlViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
