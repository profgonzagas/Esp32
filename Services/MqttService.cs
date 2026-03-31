using ESP32Controller.Models;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace ESP32Controller.Services;

public class MqttService : IDisposable
{
    private IMqttClient? _mqttClient;
    private MqttClientOptions? _options;
    private bool _disposed;
    
    // Configurações padrão HiveMQ Cloud
    private string _broker = "15fdb3019b4f41baa2bd95c3caf3722b.s1.eu.hivemq.cloud";
    private int _port = 8883;
    private string _username = "profgonzagas";
    private string _password = "H@vemq2026";
    private string _clientId = "MauiApp_NecroSENSE";
    
    // Tópicos
    private const string TOPIC_SENSORES = "necrosense/sensores";
    private const string TOPIC_STATUS = "necrosense/status";
    private const string TOPIC_COMANDOS = "necrosense/comandos";
    private const string TOPIC_RESPOSTA = "necrosense/resposta";
    
    public bool IsConectado => _mqttClient?.IsConnected ?? false;
    
    // Eventos
    public event EventHandler<DadosSensores>? OnDadosSensoresRecebidos;
    public event EventHandler<string>? OnStatusRecebido;
    public event EventHandler<string>? OnRespostaRecebida;
    public event EventHandler<bool>? OnStatusConexaoChanged;
    public event EventHandler<string>? OnMensagemLog;
    
    private void Log(string msg)
    {
        System.Diagnostics.Debug.WriteLine($"[MQTT] {msg}");
        OnMensagemLog?.Invoke(this, msg);
    }
    
    public void ConfigurarBroker(string broker, int port, string username, string password)
    {
        _broker = broker;
        _port = port;
        _username = username;
        _password = password;
        Log($"Broker configurado: {broker}:{port}");
    }
    
    public async Task<bool> ConectarAsync()
    {
        try
        {
            if (_mqttClient?.IsConnected == true)
            {
                Log("Já conectado");
                return true;
            }
            
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();
            
            _options = new MqttClientOptionsBuilder()
                .WithClientId(_clientId + "_" + Guid.NewGuid().ToString("N")[..8])
                .WithTcpServer(_broker, _port)
                .WithCredentials(_username, _password)
                .WithTlsOptions(o =>
                {
                    o.UseTls();
                    o.WithAllowUntrustedCertificates(true);
                    o.WithIgnoreCertificateChainErrors(true);
                })
                .WithCleanSession(true)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .Build();
            
            // Configurar handlers
            _mqttClient.ApplicationMessageReceivedAsync += OnMensagemRecebidaAsync;
            _mqttClient.DisconnectedAsync += OnDesconectadoAsync;
            _mqttClient.ConnectedAsync += OnConectadoAsync;
            
            Log($"Conectando a {_broker}:{_port}...");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await _mqttClient.ConnectAsync(_options, cts.Token);
            
            return _mqttClient.IsConnected;
        }
        catch (Exception ex)
        {
            Log($"Erro ao conectar: {ex.Message}");
            OnStatusConexaoChanged?.Invoke(this, false);
            return false;
        }
    }
    
    private Task OnConectadoAsync(MqttClientConnectedEventArgs args)
    {
        Log("Conectado ao HiveMQ Cloud!");
        OnStatusConexaoChanged?.Invoke(this, true);
        
        // Subscrever tópicos
        _ = SubscreverTopicosAsync();
        
        return Task.CompletedTask;
    }
    
    private async Task OnDesconectadoAsync(MqttClientDisconnectedEventArgs args)
    {
        Log($"Desconectado: {args.Reason}");
        OnStatusConexaoChanged?.Invoke(this, false);
        
        // Auto-reconexão após 5 segundos
        if (!_disposed)
        {
            await Task.Delay(5000);
            try
            {
                if (_options != null && !_disposed)
                {
                    Log("Tentando reconexão...");
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await _mqttClient!.ConnectAsync(_options, cts.Token);
                }
            }
            catch (Exception ex)
            {
                Log($"Falha na reconexão: {ex.Message}");
            }
        }
    }
    
    private async Task SubscreverTopicosAsync()
    {
        if (_mqttClient?.IsConnected != true) return;
        
        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(TOPIC_SENSORES, MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter(TOPIC_STATUS, MqttQualityOfServiceLevel.AtLeastOnce)
            .WithTopicFilter(TOPIC_RESPOSTA, MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        
        await _mqttClient.SubscribeAsync(subscribeOptions);
        Log("Subscrito em: sensores, status, resposta");
    }
    
    private Task OnMensagemRecebidaAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
        
        Log($"Recebido [{topic}]: {payload[..Math.Min(payload.Length, 100)]}");
        
        try
        {
            if (topic == TOPIC_SENSORES)
            {
                var dados = ParseDadosSensores(payload);
                if (dados != null)
                {
                    OnDadosSensoresRecebidos?.Invoke(this, dados);
                }
            }
            else if (topic == TOPIC_STATUS)
            {
                OnStatusRecebido?.Invoke(this, payload);
            }
            else if (topic == TOPIC_RESPOSTA)
            {
                OnRespostaRecebida?.Invoke(this, payload);
            }
        }
        catch (Exception ex)
        {
            Log($"Erro ao processar mensagem: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Envia comando ao ESP32 via MQTT
    /// </summary>
    public async Task<bool> EnviarComandoAsync(string comando)
    {
        if (_mqttClient?.IsConnected != true)
        {
            Log("Não conectado - comando não enviado");
            return false;
        }
        
        var json = $"{{\"comando\":\"{comando}\"}}";
        
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(TOPIC_COMANDOS)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();
        
        await _mqttClient.PublishAsync(message);
        Log($"Comando enviado: {comando}");
        return true;
    }
    
    /// <summary>
    /// Solicita leitura dos sensores via MQTT
    /// </summary>
    public Task<bool> SolicitarSensoresAsync() => EnviarComandoAsync("sensores");
    
    /// <summary>
    /// Solicita status do dispositivo via MQTT
    /// </summary>
    public Task<bool> SolicitarStatusAsync() => EnviarComandoAsync("status");
    
    public async Task DesconectarAsync()
    {
        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.DisconnectAsync();
            Log("Desconectado");
        }
        OnStatusConexaoChanged?.Invoke(this, false);
    }
    
    private DadosSensores? ParseDadosSensores(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var dados = new DadosSensores
            {
                UltimaAtualizacao = DateTime.Now
            };
            
            if (root.TryGetProperty("bme280", out var bme))
            {
                dados.Temperatura = bme.GetProperty("temperatura").GetSingle();
                dados.Umidade = bme.GetProperty("umidade").GetSingle();
                dados.Pressao = bme.GetProperty("pressao").GetSingle();
            }
            
            if (root.TryGetProperty("dht22", out var dht))
            {
                dados.TemperaturaDHT22 = dht.GetProperty("temperatura").GetSingle();
                dados.UmidadeDHT22 = dht.GetProperty("umidade").GetSingle();
            }
            
            if (root.TryGetProperty("uv", out var uv))
            {
                dados.NivelUV = uv.GetProperty("nivel").GetInt32();
                dados.IndiceUV = uv.GetProperty("indice").GetSingle();
            }
            
            return dados;
        }
        catch (Exception ex)
        {
            Log($"Erro ao parsear dados: {ex.Message}");
            return null;
        }
    }
    
    // Acessadores de configuração
    public string Broker => _broker;
    public int Port => _port;
    public string Username => _username;
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        if (_mqttClient?.IsConnected == true)
        {
            _mqttClient.DisconnectAsync().GetAwaiter().GetResult();
        }
        _mqttClient?.Dispose();
    }
}
