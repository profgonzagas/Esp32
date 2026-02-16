using ESP32Controller.Models;
using ESP32Controller.Services;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class SensoresViewModel : BaseViewModel
{
    private readonly ESP32HttpService _httpService;
    private readonly ESP32BleService _bleService;
    private bool _usarWiFi = false; // BLE por padrão (mais usado)
    private DadosSensores? _dados;
    private bool _atualizacaoAutomatica = false;
    private string _statusMensagem = "";
    private bool _bleResponseReceived;
    
    public bool UsarWiFi
    {
        get => _usarWiFi;
        set
        {
            SetProperty(ref _usarWiFi, value);
            OnPropertyChanged(nameof(ModoTexto));
        }
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
    
    public string ModoTexto => UsarWiFi ? "📶 WiFi" : "📱 BLE";
    
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
        
        // Subscrever evento BLE para receber dados dos sensores
        _bleService.OnMensagemRecebida += OnDadosBLERecebidos;
        
        // Iniciar com dados zerados
        Dados = new DadosSensores();
        
        // Auto-detectar modo
        if (_bleService.IsConectado)
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
            StatusMensagem = "⚠ Conecte via BLE ou WiFi primeiro";
        }
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
        // Formato do ESP32: "TEMP:25.5,UMID:60.0,PRESS:1013.2,TEMP2:24.8,UMID2:58.0,UV:2.1"
        // Ou formato GET_STATUS: "LED:1,R1:0,R2:0,T_BME:25.5,U_BME:60.0,P:1013.2,T_DHT:24.8,U_DHT:58.0,UV:2.1"
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
                case "TEMP":
                case "T_BME":
                    dados.Temperatura = valor;
                    break;
                case "UMID":
                case "U_BME":
                    dados.Umidade = valor;
                    break;
                case "PRESS":
                case "P":
                    dados.Pressao = valor;
                    break;
                case "TEMP2":
                case "T_DHT":
                    dados.TemperaturaDHT22 = valor;
                    break;
                case "UMID2":
                case "U_DHT":
                    dados.UmidadeDHT22 = valor;
                    break;
                case "UV":
                    dados.IndiceUV = valor;
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
            
            // Tentar modo selecionado primeiro, cair para outro se falhar
            if (!UsarWiFi || !_httpService.IsConectado)
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
        var enviado = await _bleService.SolicitarSensoresAsync();
        
        if (!enviado)
        {
            StatusMensagem = "❌ Falha ao enviar comando BLE";
            return false;
        }
        
        StatusMensagem = "📱 Aguardando resposta BLE...";
        
        // Esperar até 3 segundos pela resposta via notificação
        for (int i = 0; i < 30 && !_bleResponseReceived; i++)
        {
            await Task.Delay(100);
        }
        
        if (!_bleResponseReceived)
        {
            StatusMensagem = "⚠ Sem resposta do ESP32 via BLE (timeout 3s)";
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
