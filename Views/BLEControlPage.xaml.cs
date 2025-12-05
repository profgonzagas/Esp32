using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class BLEControlPage : ContentPage
{
    public BLEControlPage(BLEControlViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
