using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class GraficosPage : ContentPage
{
    public GraficosPage(GraficosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
