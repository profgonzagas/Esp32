using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class ConfiguracoesViewModel : BaseViewModel
{
    private readonly ConfiguracaoService _configService;
    private readonly ESP32HttpService _httpService;
    private readonly MqttService _mqttService;
    
    private string _nomeDispositivo = "NecroSENSE ESP32";
    private string _enderecoIP = "192.168.1.128";
    private int _porta = 80;
    private string _enderecoMAC = "00:4B:12:53:4C:18";
    private bool _temaEscuro = true;
    
    // MQTT
    private string _mqttBroker = "15fdb3019b4f41baa2bd95c3caf3722b.s1.eu.hivemq.cloud";
    private int _mqttPorta = 8883;
    private string _mqttUsuario = "profgonzagas";
    private string _mqttSenha = "H@vemq2026";
    private bool _mqttConectado;
    
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
    
    public string MqttBroker
    {
        get => _mqttBroker;
        set => SetProperty(ref _mqttBroker, value);
    }
    
    public int MqttPorta
    {
        get => _mqttPorta;
        set => SetProperty(ref _mqttPorta, value);
    }
    
    public string MqttUsuario
    {
        get => _mqttUsuario;
        set => SetProperty(ref _mqttUsuario, value);
    }
    
    public string MqttSenha
    {
        get => _mqttSenha;
        set => SetProperty(ref _mqttSenha, value);
    }
    
    public bool MqttConectado
    {
        get => _mqttConectado;
        set => SetProperty(ref _mqttConectado, value);
    }
    
    public ICommand SalvarCommand { get; }
    public ICommand ResetarCommand { get; }
    public ICommand TestarConexaoCommand { get; }
    public ICommand TestarMQTTCommand { get; }
    
    public ConfiguracoesViewModel(ConfiguracaoService configService, ESP32HttpService httpService, MqttService mqttService)
    {
        _configService = configService;
        _httpService = httpService;
        _mqttService = mqttService;
        
        Titulo = "Configurações";
        
        SalvarCommand = new Command(Salvar);
        ResetarCommand = new Command(Resetar);
        TestarConexaoCommand = new Command(async () => await TestarConexaoAsync());
        TestarMQTTCommand = new Command(async () => await TestarMQTTAsync());
        
        _mqttService.OnStatusConexaoChanged += (s, c) =>
            MainThread.BeginInvokeOnMainThread(() => MqttConectado = c);
        
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
        
        // MQTT
        MqttBroker = Preferences.Get("mqtt_broker", _mqttBroker);
        MqttPorta = Preferences.Get("mqtt_porta", _mqttPorta);
        MqttUsuario = Preferences.Get("mqtt_usuario", _mqttUsuario);
        MqttSenha = Preferences.Get("mqtt_senha", _mqttSenha);
        MqttConectado = _mqttService.IsConectado;
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
        
        // Salvar config MQTT
        Preferences.Set("mqtt_broker", MqttBroker);
        Preferences.Set("mqtt_porta", MqttPorta);
        Preferences.Set("mqtt_usuario", MqttUsuario);
        Preferences.Set("mqtt_senha", MqttSenha);
        
        // Aplicar config MQTT
        _mqttService.ConfigurarBroker(MqttBroker, MqttPorta, MqttUsuario, MqttSenha);
        
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
    
    private async Task TestarMQTTAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            // Aplicar config antes de testar
            _mqttService.ConfigurarBroker(MqttBroker, MqttPorta, MqttUsuario, MqttSenha);
            
            if (_mqttService.IsConectado)
            {
                await _mqttService.DesconectarAsync();
                await Shell.Current.DisplayAlert("MQTT", "Desconectado do MQTT", "OK");
                return;
            }
            
            var conectado = await _mqttService.ConectarAsync();
            
            await Shell.Current.DisplayAlert(
                conectado ? "Sucesso" : "Falha",
                conectado 
                    ? $"Conectado ao HiveMQ Cloud!\nBroker: {MqttBroker}" 
                    : "Não foi possível conectar ao MQTT.\nVerifique broker, usuário e senha.",
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
