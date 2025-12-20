using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly ESP32HttpService _httpService;
    private readonly ESP32BleService _bleService;
    private readonly ConfiguracaoService _configService;
    
    private DispositivoESP32 _dispositivo = new();
    private bool _conectadoWiFi;
    private bool _conectadoBLE;
    private string _statusMensagem = "Desconectado";
    private string _ultimaResposta = "";
    private ObservableCollection<LeituraSensor> _sensores = new();
    private ObservableCollection<ComandoESP32> _comandos = new();
    
    public DispositivoESP32 Dispositivo
    {
        get => _dispositivo;
        set => SetProperty(ref _dispositivo, value);
    }
    
    public bool ConectadoWiFi
    {
        get => _conectadoWiFi;
        set
        {
            SetProperty(ref _conectadoWiFi, value);
            OnPropertyChanged(nameof(StatusConexao));
            OnPropertyChanged(nameof(CorStatusWiFi));
        }
    }
    
    public bool ConectadoBLE
    {
        get => _conectadoBLE;
        set
        {
            SetProperty(ref _conectadoBLE, value);
            OnPropertyChanged(nameof(StatusConexao));
            OnPropertyChanged(nameof(CorStatusBLE));
        }
    }
    
    public string StatusMensagem
    {
        get => _statusMensagem;
        set => SetProperty(ref _statusMensagem, value);
    }
    
    public string UltimaResposta
    {
        get => _ultimaResposta;
        set => SetProperty(ref _ultimaResposta, value);
    }
    
    public string StatusConexao
    {
        get
        {
            if (ConectadoWiFi && ConectadoBLE) return "WiFi + BLE";
            if (ConectadoWiFi) return "WiFi";
            if (ConectadoBLE) return "BLE";
            return "Desconectado";
        }
    }
    
    public string CorStatusWiFi => ConectadoWiFi ? "#4CAF50" : "#757575";
    public string CorStatusBLE => ConectadoBLE ? "#2196F3" : "#757575";
    
    public ObservableCollection<LeituraSensor> Sensores
    {
        get => _sensores;
        set => SetProperty(ref _sensores, value);
    }
    
    public ObservableCollection<ComandoESP32> Comandos
    {
        get => _comandos;
        set => SetProperty(ref _comandos, value);
    }
    
    public ICommand TestarConexaoCommand { get; }
    public ICommand AtualizarSensoresCommand { get; }
    public ICommand ExecutarComandoCommand { get; }
    public ICommand ToggleLedCommand { get; }
    public ICommand ToggleRele1Command { get; }
    public ICommand ToggleRele2Command { get; }
    
    public DashboardViewModel(
        ESP32HttpService httpService, 
        ESP32BleService bleService,
        ConfiguracaoService configService)
    {
        _httpService = httpService;
        _bleService = bleService;
        _configService = configService;
        
        Titulo = "NecroSENSE/PCDF";
        
        TestarConexaoCommand = new Command(async () => await TestarConexaoAsync());
        AtualizarSensoresCommand = new Command(async () => await AtualizarSensoresAsync());
        ExecutarComandoCommand = new Command<ComandoESP32>(async (cmd) => await ExecutarComandoAsync(cmd));
        ToggleLedCommand = new Command(async () => await ExecutarAsync(_httpService.ToggleLedAsync()));
        ToggleRele1Command = new Command(async () => await ExecutarAsync(_httpService.ToggleRele1Async()));
        ToggleRele2Command = new Command(async () => await ExecutarAsync(_httpService.ToggleRele2Async()));
        
        // Eventos
        _httpService.OnStatusConexaoChanged += (s, conectado) =>
        {
            MainThread.BeginInvokeOnMainThread(() => ConectadoWiFi = conectado);
        };
        
        _httpService.OnMensagemRecebida += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() => UltimaResposta = msg);
        };
        
        _bleService.OnStatusConexaoChanged += (s, conectado) =>
        {
            MainThread.BeginInvokeOnMainThread(() => ConectadoBLE = conectado);
        };
        
        CarregarConfiguracao();
    }
    
    private void CarregarConfiguracao()
    {
        Dispositivo = _configService.CarregarDispositivo();
        _httpService.ConfigurarDispositivo(Dispositivo);
        
        var comandos = _configService.CarregarComandos();
        Comandos = new ObservableCollection<ComandoESP32>(comandos);
    }
    
    public async Task TestarConexaoAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        StatusMensagem = "Testando conexão...";
        
        try
        {
            var conectado = await _httpService.TestarConexaoAsync();
            
            if (conectado)
            {
                StatusMensagem = $"Conectado em {Dispositivo.EnderecoIP}";
                await AtualizarSensoresAsync();
            }
            else
            {
                StatusMensagem = "Falha na conexão";
            }
        }
        catch (Exception ex)
        {
            StatusMensagem = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    public async Task AtualizarSensoresAsync()
    {
        try
        {
            var sensores = await _httpService.ObterSensoresAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Sensores.Clear();
                foreach (var sensor in sensores)
                {
                    Sensores.Add(sensor);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar sensores: {ex.Message}");
        }
    }
    
    public async Task ExecutarComandoAsync(ComandoESP32 comando)
    {
        if (IsBusy || comando == null) return;
        
        IsBusy = true;
        
        try
        {
            var resposta = await _httpService.EnviarComandoAsync(comando.Endpoint, comando.Metodo, comando.Payload);
            UltimaResposta = resposta;
            
            if (comando.IsToggle)
            {
                comando.Estado = !comando.Estado;
            }
        }
        catch (Exception ex)
        {
            UltimaResposta = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExecutarAsync(Task<string> tarefa)
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            var resposta = await tarefa;
            UltimaResposta = resposta;
        }
        catch (Exception ex)
        {
            UltimaResposta = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
