using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (BindingContext is DashboardViewModel vm)
        {
            await vm.TestarConexaoAsync();
        }
    }
}
