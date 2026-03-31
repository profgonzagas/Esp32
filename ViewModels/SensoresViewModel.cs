using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class SensoresViewModel : BaseViewModel
{
    private readonly ESP32HttpService _httpService;
    private readonly ESP32BleService _bleService;
    private readonly MqttService _mqttService;
    private bool _usarWiFi = false; // BLE por padrão (mais usado)
    private bool _usarMQTT = false;
    private DadosSensores? _dados;
    private bool _atualizacaoAutomatica = false;
    private string _statusMensagem = "";
    private bool _bleResponseReceived;
    private bool _mqttConectado;
    
    public bool UsarWiFi
    {
        get => _usarWiFi;
        set
        {
            SetProperty(ref _usarWiFi, value);
            OnPropertyChanged(nameof(ModoTexto));
        }
    }
    
    public bool UsarMQTT
    {
        get => _usarMQTT;
        set
        {
            SetProperty(ref _usarMQTT, value);
            OnPropertyChanged(nameof(ModoTexto));
        }
    }
    
    public bool MqttConectado
    {
        get => _mqttConectado;
        set => SetProperty(ref _mqttConectado, value);
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
    
    public string StatusMensagem
    {
        get => _statusMensagem;
        set => SetProperty(ref _statusMensagem, value);
    }
    
    public string ModoTexto => UsarMQTT ? "🌐 MQTT" : UsarWiFi ? "📶 WiFi" : "📱 BLE";
    
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
    public ICommand ConectarMQTTCommand { get; }
    
    public SensoresViewModel(ESP32HttpService httpService, ESP32BleService bleService, MqttService mqttService)
    {
        _httpService = httpService;
        _bleService = bleService;
        _mqttService = mqttService;
        
        Titulo = "Sensores";
        
        AtualizarCommand = new Command(async () => await AtualizarDadosAsync());
        AlternarModoCommand = new Command(AlternarModo);
        ConectarMQTTCommand = new Command(async () => await ConectarMQTTAsync());
        
        // Subscrever evento BLE para receber dados dos sensores
        _bleService.OnMensagemRecebida += OnDadosBLERecebidos;
        
        // Subscrever evento MQTT para receber dados dos sensores
        _mqttService.OnDadosSensoresRecebidos += OnDadosMQTTRecebidos;
        _mqttService.OnStatusConexaoChanged += (s, conectado) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MqttConectado = conectado;
                if (conectado)
                    StatusMensagem = "🌐 MQTT conectado - dados em tempo real";
                else if (UsarMQTT)
                    StatusMensagem = "⚠ MQTT desconectado";
            });
        };
        
        // Iniciar com dados zerados
        Dados = new DadosSensores();
        
        // Auto-detectar modo
        if (_mqttService.IsConectado)
        {
            StatusMensagem = "🌐 MQTT conectado - dados em tempo real";
            _usarMQTT = true;
            _usarWiFi = false;
            _mqttConectado = true;
        }
        else if (_bleService.IsConectado)
        {
            StatusMensagem = "📱 BLE conectado - clique Atualizar";
            _usarWiFi = false;
        }
        else if (_httpService.IsConectado)
        {
            StatusMensagem = "📶 WiFi conectado - clique Atualizar";
            _usarWiFi = true;
        }
        else
        {
            StatusMensagem = "⚠ Conecte via BLE, WiFi ou MQTT";
        }
    }
    
    private void AlternarModo()
    {
        // Ciclar: BLE -> WiFi -> MQTT -> BLE
        if (!UsarWiFi && !UsarMQTT)
        {
            UsarWiFi = true;
            UsarMQTT = false;
        }
        else if (UsarWiFi && !UsarMQTT)
        {
            UsarWiFi = false;
            UsarMQTT = true;
        }
        else
        {
            UsarWiFi = false;
            UsarMQTT = false;
        }
    }
    
    private async Task ConectarMQTTAsync()
    {
        if (_mqttService.IsConectado)
        {
            await _mqttService.DesconectarAsync();
            StatusMensagem = "🌐 MQTT desconectado";
            return;
        }
        
        StatusMensagem = "🌐 Conectando ao MQTT...";
        IsBusy = true;
        
        try
        {
            var ok = await _mqttService.ConectarAsync();
            if (ok)
            {
                UsarMQTT = true;
                UsarWiFi = false;
                StatusMensagem = "🌐 MQTT conectado! Dados serão recebidos automaticamente";
            }
            else
            {
                StatusMensagem = "❌ Falha ao conectar MQTT - verifique configurações";
            }
        }
        catch (Exception ex)
        {
            StatusMensagem = $"❌ Erro MQTT: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OnDadosMQTTRecebidos(object? sender, DadosSensores dados)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Dados = dados;
            StatusMensagem = $"🌐 MQTT - Atualizado {dados.UltimaAtualizacao:HH:mm:ss}";
        });
    }
    
    private void OnDadosBLERecebidos(object? sender, string mensagem)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Sensores] BLE recebido: {mensagem}");
            
            var dados = ParseSensorData(mensagem);
            if (dados != null)
            {
                _bleResponseReceived = true;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Dados = dados;
                    StatusMensagem = "✅ Dados atualizados via BLE";
                    System.Diagnostics.Debug.WriteLine("[Sensores] Dados atualizados via BLE");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sensores] Erro ao processar BLE: {ex.Message}");
        }
    }
    
    private DadosSensores? ParseSensorData(string mensagem)
    {
        // Formato ESP32 BLE: "T:25.5,U:60.0,P:1013.2,T2:24.8,U2:58.0,UV:2.1"
        // Formato GET_STATUS: "L:1,R1:0,R2:0,T:25.5,U:60.0,P:1013.2,T2:24.8,U2:58.0,UV:2.1"
        // Formatos legados também aceitos: TEMP, UMID, PRESS, T_BME, U_BME, etc.
        if (string.IsNullOrWhiteSpace(mensagem))
            return null;
            
        var dados = new DadosSensores();
        var pares = mensagem.Split(',');
        
        foreach (var par in pares)
        {
            var partes = par.Split(':');
            if (partes.Length != 2) continue;
            
            var chave = partes[0].Trim().ToUpper();
            if (!float.TryParse(partes[1].Trim(), 
                System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out float valor))
                continue;
            
            switch (chave)
            {
                case "T":
                case "TEMP":
                case "T_BME":
                    dados.Temperatura = valor;
                    break;
                case "U":
                case "UMID":
                case "U_BME":
                    dados.Umidade = valor;
                    break;
                case "PRESS":
                case "P":
                    dados.Pressao = valor;
                    break;
                case "T2":
                case "TEMP2":
                case "T_DHT":
                    dados.TemperaturaDHT22 = valor;
                    break;
                case "U2":
                case "UMID2":
                case "U_DHT":
                    dados.UmidadeDHT22 = valor;
                    break;
                case "UV":
                    dados.IndiceUV = valor;
                    break;
                case "LDR":
                    dados.LdrPercentual = valor;
                    break;
            }
        }
        
        dados.UltimaAtualizacao = DateTime.Now;
        return dados;
    }
    
    private async Task AtualizarDadosAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        StatusMensagem = "🔄 Atualizando...";
        
        try
        {
            bool sucesso = false;
            
            // MQTT mode: solicitar sensores via MQTT
            if (UsarMQTT)
            {
                if (_mqttService.IsConectado)
                {
                    sucesso = await _mqttService.SolicitarSensoresAsync();
                    if (sucesso)
                    {
                        StatusMensagem = "🌐 Solicitação MQTT enviada...";
                        // Dados chegam via callback OnDadosMQTTRecebidos
                        await Task.Delay(2000); // Esperar resposta
                        sucesso = Dados?.UltimaAtualizacao > DateTime.Now.AddSeconds(-5);
                    }
                    else
                    {
                        StatusMensagem = "❌ Falha ao enviar via MQTT";
                    }
                }
                else
                {
                    StatusMensagem = "⚠ MQTT não conectado - clique 🌐 para conectar";
                }
            }
            // Tentar modo selecionado primeiro, cair para outro se falhar
            else if (!UsarWiFi || !_httpService.IsConectado)
            {
                // Tentar BLE
                if (_bleService.IsConectado)
                {
                    sucesso = await AtualizarViaBLEAsync();
                }
                else if (_httpService.IsConectado)
                {
                    sucesso = await AtualizarViaWiFiAsync();
                }
                else
                {
                    StatusMensagem = "❌ Conecte via BLE ou WiFi primeiro";
                }
            }
            else
            {
                // Tentar WiFi
                sucesso = await AtualizarViaWiFiAsync();
                
                // Fallback para BLE se WiFi falhou
                if (!sucesso && _bleService.IsConectado)
                {
                    StatusMensagem = "📶 WiFi falhou, tentando BLE...";
                    sucesso = await AtualizarViaBLEAsync();
                }
            }
            
            if (!sucesso && string.IsNullOrEmpty(StatusMensagem))
            {
                StatusMensagem = "❌ Não foi possível obter dados";
            }
        }
        catch (Exception ex)
        {
            StatusMensagem = $"❌ Erro: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Erro ao atualizar sensores: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task<bool> AtualizarViaBLEAsync()
    {
        _bleResponseReceived = false;
        
        StatusMensagem = "📱 Enviando comando BLE...";
        
        // Usar método com resposta via notificação
        var resposta = await _bleService.EnviarComandoComRespostaAsync("GET_SENSORS", 3000);
        
        if (resposta != null)
        {
            // Se resposta veio via leitura direta, processar aqui
            if (!_bleResponseReceived)
            {
                var dados = ParseSensorData(resposta);
                if (dados != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Dados = dados;
                        StatusMensagem = "✅ Dados atualizados via BLE";
                    });
                    return true;
                }
            }
            return _bleResponseReceived;
        }
        
        // Fallback: enviar e aguardar notificação
        StatusMensagem = "📱 Tentando via notificação...";
        var enviado = await _bleService.SolicitarSensoresAsync();
        
        if (!enviado)
        {
            StatusMensagem = "❌ Falha ao enviar comando BLE";
            return false;
        }
        
        // Esperar até 3 segundos pela resposta via notificação
        for (int i = 0; i < 30 && !_bleResponseReceived; i++)
        {
            await Task.Delay(100);
        }
        
        if (!_bleResponseReceived)
        {
            StatusMensagem = "⚠ ESP32 não respondeu (verifique Serial Monitor)";
            return false;
        }
        
        return true;
    }
    
    private async Task<bool> AtualizarViaWiFiAsync()
    {
        var dados = await _httpService.ObterDadosSensoresAsync();
        if (dados != null)
        {
            Dados = dados;
            StatusMensagem = "✅ Dados atualizados via WiFi";
            return true;
        }
        
        StatusMensagem = "⚠ Sem resposta WiFi";
        return false;
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
