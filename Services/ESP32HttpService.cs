using ESP32Controller.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ESP32Controller.Services;

public class ESP32HttpService
{
    private readonly HttpClient _httpClient;
    private DispositivoESP32? _dispositivo;
    
    public bool IsConectado => _dispositivo?.ConectadoWiFi ?? false;
    public event EventHandler<string>? OnMensagemRecebida;
    public event EventHandler<bool>? OnStatusConexaoChanged;
    
    public ESP32HttpService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
    
    public void ConfigurarDispositivo(DispositivoESP32 dispositivo)
    {
        _dispositivo = dispositivo;
    }
    
    public async Task<bool> TestarConexaoAsync()
    {
        if (_dispositivo == null) return false;
        
        try
        {
            var response = await _httpClient.GetAsync($"{_dispositivo.UrlBase}/status");
            _dispositivo.ConectadoWiFi = response.IsSuccessStatusCode;
            
            if (_dispositivo.ConectadoWiFi)
            {
                _dispositivo.UltimaConexao = DateTime.Now;
            }
            
            OnStatusConexaoChanged?.Invoke(this, _dispositivo.ConectadoWiFi);
            return _dispositivo.ConectadoWiFi;
        }
        catch
        {
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
            
            if (metodo.ToUpper() == "POST" && payload != null)
            {
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                response = await _httpClient.PostAsync(url, content);
            }
            else
            {
                response = await _httpClient.GetAsync(url);
            }
            
            var resultado = await response.Content.ReadAsStringAsync();
            OnMensagemRecebida?.Invoke(this, resultado);
            
            return resultado;
        }
        catch (TaskCanceledException)
        {
            return "Timeout: ESP32 não respondeu";
        }
        catch (HttpRequestException ex)
        {
            return $"Erro de conexão: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Erro: {ex.Message}";
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
    public Task<string> ObterTemperaturaAsync() => EnviarComandoAsync("temperatura");
    public Task<string> ObterUmidadeAsync() => EnviarComandoAsync("umidade");
    public Task<string> ObterStatusAsync() => EnviarComandoAsync("status");
    
    public async Task<string> EnviarPWMAsync(int pino, int valor)
    {
        return await EnviarComandoAsync($"pwm/{pino}/{valor}");
    }
    
    public async Task<string> DefinirPinoAsync(int pino, bool estado)
    {
        return await EnviarComandoAsync($"gpio/{pino}/{(estado ? "high" : "low")}");
    }
}
