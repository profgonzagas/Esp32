using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class SensoresViewModel : BaseViewModel
{
    private readonly ESP32HttpService _httpService;
    private readonly ESP32BleService _bleService;
    private bool _usarWiFi = true;
    private DadosSensores? _dados;
    private bool _atualizacaoAutomatica = false;
    
    public bool UsarWiFi
    {
        get => _usarWiFi;
        set => SetProperty(ref _usarWiFi, value);
    }
    
    public DadosSensores? Dados
    {
        get => _dados;
        set
        {
            SetProperty(ref _dados, value);
            OnPropertyChanged(nameof(TemDados));
        }
    }
    
    public bool TemDados => Dados != null;
    
    public bool AtualizacaoAutomatica
    {
        get => _atualizacaoAutomatica;
        set
        {
            SetProperty(ref _atualizacaoAutomatica, value);
            if (value)
                _ = IniciarAtualizacaoAutomaticaAsync();
        }
    }
    
    public ICommand AtualizarCommand { get; }
    public ICommand AlternarModoCommand { get; }
    
    public SensoresViewModel(ESP32HttpService httpService, ESP32BleService bleService)
    {
        _httpService = httpService;
        _bleService = bleService;
        
        Titulo = "Sensores";
        
        AtualizarCommand = new Command(async () => await AtualizarDadosAsync());
        AlternarModoCommand = new Command(() => UsarWiFi = !UsarWiFi);
        
        // Iniciar com dados zerados
        Dados = new DadosSensores();
    }
    
    private async Task AtualizarDadosAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            if (UsarWiFi)
            {
                var dados = await _httpService.ObterDadosSensoresAsync();
                if (dados != null)
                {
                    Dados = dados;
                }
            }
            else
            {
                await _bleService.SolicitarSensoresAsync();
                // Dados virão via evento BLE
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar sensores: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task IniciarAtualizacaoAutomaticaAsync()
    {
        while (AtualizacaoAutomatica)
        {
            await AtualizarDadosAsync();
            await Task.Delay(3000); // Atualiza a cada 3 segundos
        }
    }
}
