using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class ConfiguracoesViewModel : BaseViewModel
{
    private readonly ConfiguracaoService _configService;
    private readonly ESP32HttpService _httpService;
    
    private string _nomeDispositivo = "NecroSENSE ESP32";
    private string _enderecoIP = "192.168.1.128";
    private int _porta = 80;
    private string _enderecoMAC = "00:4B:12:53:4C:18";
    private bool _temaEscuro = true;
    
    public string NomeDispositivo
    {
        get => _nomeDispositivo;
        set => SetProperty(ref _nomeDispositivo, value);
    }
    
    public string EnderecoIP
    {
        get => _enderecoIP;
        set => SetProperty(ref _enderecoIP, value);
    }
    
    public int Porta
    {
        get => _porta;
        set => SetProperty(ref _porta, value);
    }
    
    public string EnderecoMAC
    {
        get => _enderecoMAC;
        set => SetProperty(ref _enderecoMAC, value);
    }
    
    public bool TemaEscuro
    {
        get => _temaEscuro;
        set
        {
            SetProperty(ref _temaEscuro, value);
            AplicarTema();
        }
    }
    
    public ICommand SalvarCommand { get; }
    public ICommand ResetarCommand { get; }
    public ICommand TestarConexaoCommand { get; }
    
    public ConfiguracoesViewModel(ConfiguracaoService configService, ESP32HttpService httpService)
    {
        _configService = configService;
        _httpService = httpService;
        
        Titulo = "Configurações";
        
        SalvarCommand = new Command(Salvar);
        ResetarCommand = new Command(Resetar);
        TestarConexaoCommand = new Command(async () => await TestarConexaoAsync());
        
        CarregarConfiguracao();
    }
    
    private void CarregarConfiguracao()
    {
        var dispositivo = _configService.CarregarDispositivo();
        NomeDispositivo = dispositivo.Nome;
        EnderecoIP = dispositivo.EnderecoIP;
        Porta = dispositivo.Porta;
        EnderecoMAC = dispositivo.EnderecoMAC;
        
        TemaEscuro = Preferences.Get("tema_escuro", true);
    }
    
    private void Salvar()
    {
        var dispositivo = new DispositivoESP32
        {
            Nome = NomeDispositivo,
            EnderecoIP = EnderecoIP,
            Porta = Porta,
            EnderecoMAC = EnderecoMAC
        };
        
        _configService.SalvarDispositivo(dispositivo);
        _httpService.ConfigurarDispositivo(dispositivo);
        
        Preferences.Set("tema_escuro", TemaEscuro);
        
        Shell.Current.DisplayAlert("Sucesso", "Configurações salvas!", "OK");
    }
    
    private void Resetar()
    {
        _configService.ResetarParaPadrao();
        CarregarConfiguracao();
        
        Shell.Current.DisplayAlert("Sucesso", "Configurações resetadas para o padrão!", "OK");
    }
    
    private async Task TestarConexaoAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            var dispositivo = new DispositivoESP32
            {
                EnderecoIP = EnderecoIP,
                Porta = Porta
            };
            
            _httpService.ConfigurarDispositivo(dispositivo);
            var conectado = await _httpService.TestarConexaoAsync();
            
            await Shell.Current.DisplayAlert(
                conectado ? "Sucesso" : "Falha",
                conectado ? $"Conectado a {EnderecoIP}:{Porta}" : "Não foi possível conectar ao ESP32",
                "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void AplicarTema()
    {
        if (Application.Current != null)
        {
            Application.Current.UserAppTheme = TemaEscuro ? AppTheme.Dark : AppTheme.Light;
        }
    }
}
