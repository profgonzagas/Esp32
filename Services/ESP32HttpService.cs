using ESP32Controller.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ESP32Controller.Services;

public class ESP32HttpService
{
    private readonly HttpClient _httpClient;
    private DispositivoESP32? _dispositivo;
    private int _retentativas = 3;
    
    public bool IsConectado => _dispositivo?.ConectadoWiFi ?? false;
    public event EventHandler<string>? OnMensagemRecebida;
    public event EventHandler<bool>? OnStatusConexaoChanged;
    
    private void LogDebug(string msg)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine(msg);
            // Tentar escrever para um caminho acessível
            try
            {
                var logDir = "/sdcard/Download/";
                var logPath = Path.Combine(logDir, "esp32_debug.log");
                if (Directory.Exists(logDir) && logPath.Length > 0)
                {
                    File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - {msg}\n");
                }
            }
            catch { }
        }
        catch { }
    }
    
    public ESP32HttpService()
    {
        var handler = new HttpClientHandler();
        
        // Configurações de rede melhoradas
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        handler.AllowAutoRedirect = true;
        handler.UseCookies = false;
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20), // Aumentado para 20 segundos
            DefaultRequestVersion = HttpVersion.Version11 // HTTP/1.1 mais compatível
        };
        
        // Headers padrão
        _httpClient.DefaultRequestHeaders.Add("Connection", "close");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ESP32-Controller/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        
        LogDebug("[WiFi] HttpClient inicializado com timeout de 20s");
    }
    
    public void ConfigurarDispositivo(DispositivoESP32 dispositivo)
    {
        _dispositivo = dispositivo;
        LogDebug($"[WiFi] Dispositivo configurado: {dispositivo.UrlBase}");
    }
    
    public void Desconectar()
    {
        if (_dispositivo != null)
        {
            _dispositivo.ConectadoWiFi = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            LogDebug("[WiFi] Desconectado");
        }
    }
    
    public async Task<bool> TestarConexaoAsync()
    {
        if (_dispositivo == null) 
        {
            LogDebug("[WiFi] Dispositivo não configurado");
            return false;
        }
        
        try
        {
            var url = $"{_dispositivo.UrlBase}/status";
            LogDebug($"[WiFi] Testando conexão: {url}");
            
            // Tentar com retentativas
            for (int tentativa = 1; tentativa <= _retentativas; tentativa++)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                        
                        LogDebug($"[WiFi] Tentativa {tentativa}: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _dispositivo.ConectadoWiFi = true;
                            _dispositivo.UltimaConexao = DateTime.Now;
                            OnStatusConexaoChanged?.Invoke(this, true);
                            LogDebug("[WiFi] ✓ Conectado com sucesso");
                            return true;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine($"[WiFi] Tentativa {tentativa}: Timeout");
                    if (tentativa < _retentativas)
                        await Task.Delay(1000 * tentativa); // Backoff exponencial
                }
            }
            
            _dispositivo.ConectadoWiFi = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            System.Diagnostics.Debug.WriteLine("[WiFi] ✗ Falha após {_retentativas} tentativas");
            return false;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WiFi] Erro de conexão: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[WiFi] InnerException: {ex.InnerException?.Message}");
            _dispositivo.ConectadoWiFi = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WiFi] Erro inesperado: {ex.GetType().Name} - {ex.Message}");
            _dispositivo.ConectadoWiFi = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            return false;
        }
    }
    
    public async Task<string> EnviarComandoAsync(string endpoint, string metodo = "GET", string? payload = null)
    {
        if (_dispositivo == null) 
            return "Dispositivo não configurado";
        
        try
        {
            HttpResponseMessage response;
            var url = $"{_dispositivo.UrlBase}/{endpoint.TrimStart('/')}";
            
            System.Diagnostics.Debug.WriteLine($"[WiFi] Enviando {metodo} para: {url}");
            
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                if (metodo.ToUpper() == "POST" && payload != null)
                {
                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    response = await _httpClient.PostAsync(url, content, cts.Token);
                }
                else
                {
                    response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                }
            }
            
            var resultado = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"[WiFi] Resposta ({response.StatusCode}): {resultado.Substring(0, Math.Min(100, resultado.Length))}");
            
            OnMensagemRecebida?.Invoke(this, resultado);
            
            return resultado;
        }
        catch (TaskCanceledException)
        {
            var erro = "Timeout: ESP32 não respondeu em 10 segundos";
            System.Diagnostics.Debug.WriteLine($"[WiFi] {erro}");
            return erro;
        }
        catch (HttpRequestException ex)
        {
            var erro = $"Erro de conexão: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[WiFi] {erro}");
            return erro;
        }
        catch (Exception ex)
        {
            var erro = $"Erro: {ex.GetType().Name} - {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[WiFi] {erro}");
            return erro;
        }
    }
    
    public async Task<T?> EnviarComandoAsync<T>(string endpoint) where T : class
    {
        try
        {
            if (_dispositivo == null) return null;
            
            var url = $"{_dispositivo.UrlBase}/{endpoint.TrimStart('/')}";
            return await _httpClient.GetFromJsonAsync<T>(url);
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<List<LeituraSensor>> ObterSensoresAsync()
    {
        try
        {
            var json = await EnviarComandoAsync("sensores");
            if (string.IsNullOrEmpty(json)) return new List<LeituraSensor>();
            
            return JsonSerializer.Deserialize<List<LeituraSensor>>(json) ?? new List<LeituraSensor>();
        }
        catch
        {
            return new List<LeituraSensor>();
        }
    }
    
    // Comandos comuns pré-definidos
    public Task<string> LigarLedAsync() => EnviarComandoAsync("led/on");
    public Task<string> DesligarLedAsync() => EnviarComandoAsync("led/off");
    public Task<string> ToggleLedAsync() => EnviarComandoAsync("led/toggle");
    public Task<string> LigarReleAsync(int numero = 1) => EnviarComandoAsync($"rele/{numero}/on");
    public Task<string> DesligarReleAsync(int numero = 1) => EnviarComandoAsync($"rele/{numero}/off");
    public Task<string> ToggleRele1Async() => EnviarComandoAsync("rele/1/toggle");
    public Task<string> ToggleRele2Async() => EnviarComandoAsync("rele/2/toggle");
    public Task<string> ObterTemperaturaAsync() => EnviarComandoAsync("temperatura");
    public Task<string> ObterUmidadeAsync() => EnviarComandoAsync("umidade");
    public Task<string> ObterStatusAsync() => EnviarComandoAsync("status");
    
    // Comandos dos sensores
    public async Task<DadosSensores?> ObterDadosSensoresAsync()
    {
        try
        {
            // Usar o endpoint /status que retorna JSON estruturado com ámbos os sensores
            var json = await EnviarComandoAsync("status");
            if (string.IsNullOrEmpty(json)) return null;
            
            // Deserializar o JSON da resposta /status
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var dados = new DadosSensores();
            
            // Extrair dados BME280
            if (root.TryGetProperty("sensores", out var sensoresElement))
            {
                if (sensoresElement.TryGetProperty("bme280", out var bme280Element))
                {
                    if (bme280Element.TryGetProperty("temperatura", out var tempBME))
                        dados.Temperatura = (float)tempBME.GetDouble();
                    
                    if (bme280Element.TryGetProperty("umidade", out var umidBME))
                        dados.Umidade = (float)umidBME.GetDouble();
                    
                    if (bme280Element.TryGetProperty("pressao", out var pressaoBME))
                        dados.Pressao = (float)pressaoBME.GetDouble();
                }
                
                // Extrair dados DHT22
                if (sensoresElement.TryGetProperty("dht22", out var dht22Element))
                {
                    if (dht22Element.TryGetProperty("temperatura", out var tempDHT))
                        dados.TemperaturaDHT22 = (float)tempDHT.GetDouble();
                    
                    if (dht22Element.TryGetProperty("umidade", out var umidDHT))
                        dados.UmidadeDHT22 = (float)umidDHT.GetDouble();
                }
                
                // Extrair dados UV
                if (sensoresElement.TryGetProperty("uv", out var uvElement))
                {
                    if (uvElement.TryGetProperty("nivel", out var nivelUV))
                        dados.NivelUV = nivelUV.GetInt32();
                    
                    if (uvElement.TryGetProperty("indice", out var indiceUV))
                        dados.IndiceUV = (float)indiceUV.GetDouble();
                }

                // Extrair dados LDR HW-072
                if (sensoresElement.TryGetProperty("ldr", out var ldrElement))
                {
                    if (ldrElement.TryGetProperty("raw", out var ldrRaw))
                        dados.LdrRaw = ldrRaw.GetInt32();

                    if (ldrElement.TryGetProperty("mV", out var ldrMv))
                        dados.LdrMv = ldrMv.GetInt32();

                    if (ldrElement.TryGetProperty("luminosidade_pct", out var ldrPct))
                        dados.LdrPercentual = (float)ldrPct.GetDouble();

                    if (ldrElement.TryGetProperty("descricao", out var ldrDesc))
                        dados.LdrDescricao = ldrDesc.GetString() ?? "N/A";
                }
            }
            
            dados.UltimaAtualizacao = DateTime.Now;
            return dados;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WiFi] Erro ao obter sensores: {ex.Message}");
            return null;
        }
    }
    
    public async Task<string> EnviarPWMAsync(int pino, int valor)
    {
        return await EnviarComandoAsync($"pwm/{pino}/{valor}");
    }
    
    public async Task<string> DefinirPinoAsync(int pino, bool estado)
    {
        return await EnviarComandoAsync($"gpio/{pino}/{(estado ? "high" : "low")}");
    }
}
