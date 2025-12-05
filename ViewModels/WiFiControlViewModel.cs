using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class WiFiControlViewModel : BaseViewModel
{
    private readonly ESP32HttpService _httpService;
    private readonly ConfiguracaoService _configService;
    
    private string _enderecoIP = "192.168.1.100";
    private int _porta = 80;
    private string _endpointCustom = "";
    private string _payloadCustom = "";
    private string _metodoselecionado = "GET";
    private string _resposta = "";
    private bool _conectado;
    private int _valorPWM = 0;
    private int _pinoPWM = 2;
    private int _pinoGPIO = 2;
    
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
    
    public string EndpointCustom
    {
        get => _endpointCustom;
        set => SetProperty(ref _endpointCustom, value);
    }
    
    public string PayloadCustom
    {
        get => _payloadCustom;
        set => SetProperty(ref _payloadCustom, value);
    }
    
    public string MetodoSelecionado
    {
        get => _metodoselecionado;
        set => SetProperty(ref _metodoselecionado, value);
    }
    
    public string Resposta
    {
        get => _resposta;
        set => SetProperty(ref _resposta, value);
    }
    
    public bool Conectado
    {
        get => _conectado;
        set
        {
            SetProperty(ref _conectado, value);
            OnPropertyChanged(nameof(StatusTexto));
            OnPropertyChanged(nameof(StatusCor));
        }
    }
    
    public string StatusTexto => Conectado ? "🟢 Conectado" : "🔴 Desconectado";
    public string StatusCor => Conectado ? "#4CAF50" : "#F44336";
    
    public int ValorPWM
    {
        get => _valorPWM;
        set => SetProperty(ref _valorPWM, value);
    }
    
    public int PinoPWM
    {
        get => _pinoPWM;
        set => SetProperty(ref _pinoPWM, value);
    }
    
    public int PinoGPIO
    {
        get => _pinoGPIO;
        set => SetProperty(ref _pinoGPIO, value);
    }
    
    public List<string> Metodos { get; } = new() { "GET", "POST" };
    public List<int> PinosDisponiveis { get; } = new() { 2, 4, 5, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23, 25, 26, 27, 32, 33 };
    
    public ICommand ConectarCommand { get; }
    public ICommand EnviarComandoCommand { get; }
    public ICommand LigarLedCommand { get; }
    public ICommand DesligarLedCommand { get; }
    public ICommand ToggleLedCommand { get; }
    public ICommand EnviarPWMCommand { get; }
    public ICommand GPIOHighCommand { get; }
    public ICommand GPIOLowCommand { get; }
    public ICommand ObterStatusCommand { get; }
    
    public WiFiControlViewModel(ESP32HttpService httpService, ConfiguracaoService configService)
    {
        _httpService = httpService;
        _configService = configService;
        
        Titulo = "Controle WiFi";
        
        ConectarCommand = new Command(async () => await ConectarAsync());
        EnviarComandoCommand = new Command(async () => await EnviarComandoCustomAsync());
        LigarLedCommand = new Command(async () => await ExecutarAsync(_httpService.LigarLedAsync()));
        DesligarLedCommand = new Command(async () => await ExecutarAsync(_httpService.DesligarLedAsync()));
        ToggleLedCommand = new Command(async () => await ExecutarAsync(_httpService.ToggleLedAsync()));
        EnviarPWMCommand = new Command(async () => await EnviarPWMAsync());
        GPIOHighCommand = new Command(async () => await ExecutarAsync(_httpService.DefinirPinoAsync(PinoGPIO, true)));
        GPIOLowCommand = new Command(async () => await ExecutarAsync(_httpService.DefinirPinoAsync(PinoGPIO, false)));
        ObterStatusCommand = new Command(async () => await ExecutarAsync(_httpService.ObterStatusAsync()));
        
        _httpService.OnStatusConexaoChanged += (s, c) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Conectado = c);
        };
        
        _httpService.OnMensagemRecebida += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Resposta = msg);
        };
        
        CarregarConfiguracao();
    }
    
    private void CarregarConfiguracao()
    {
        var dispositivo = _configService.CarregarDispositivo();
        EnderecoIP = dispositivo.EnderecoIP;
        Porta = dispositivo.Porta;
    }
    
    private async Task ConectarAsync()
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
            _configService.SalvarDispositivo(dispositivo);
            
            var conectado = await _httpService.TestarConexaoAsync();
            Resposta = conectado ? "Conexão bem sucedida!" : "Falha na conexão. Verifique o IP e se o ESP32 está ligado.";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task EnviarComandoCustomAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(EndpointCustom)) return;
        
        IsBusy = true;
        
        try
        {
            Resposta = await _httpService.EnviarComandoAsync(EndpointCustom, MetodoSelecionado, PayloadCustom);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task EnviarPWMAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            Resposta = await _httpService.EnviarPWMAsync(PinoPWM, ValorPWM);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExecutarAsync(Task<string> task)
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            Resposta = await task;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
