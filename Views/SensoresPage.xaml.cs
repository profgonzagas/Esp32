using ESP32Controller.ViewModels;

namespace ESP32Controller.Views;

public partial class SensoresPage : ContentPage
{
    public SensoresPage(SensoresViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
    
    private void OnToggleAutoUpdate(object sender, EventArgs e)
    {
        if (BindingContext is SensoresViewModel vm)
        {
            vm.AtualizacaoAutomatica = !vm.AtualizacaoAutomatica;
            if (sender is Button button)
            {
                button.Text = vm.AtualizacaoAutomatica ? "⏸ Pausar" : "▶ Auto Atualizar";
            }
        }
    }
}
