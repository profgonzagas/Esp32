using ESP32Controller.Models;
using System.Text;

namespace ESP32Controller.Services;

public class ESP32BleService
{
    private bool _isScanning;
    private bool _isConnected;
    private string _deviceId = "";
    
    public bool IsScanning => _isScanning;
    public bool IsConectado => _isConnected;
    
    public event EventHandler<string>? OnMensagemRecebida;
    public event EventHandler<bool>? OnStatusConexaoChanged;
    public event EventHandler<List<DispositivoBLE>>? OnDispositivosEncontrados;
    
    // UUIDs padrão para ESP32 BLE
    public static readonly Guid ServiceUUID = Guid.Parse("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
    public static readonly Guid CharacteristicTxUUID = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a8");
    public static readonly Guid CharacteristicRxUUID = Guid.Parse("beb5483e-36e1-4688-b7f5-ea07361b26a9");
    
    public ESP32BleService()
    {
    }
    
    public async Task<List<DispositivoBLE>> ScanearDispositivosAsync(int timeoutSeconds = 10)
    {
        var dispositivos = new List<DispositivoBLE>();
        _isScanning = true;
        
        try
        {
            // Simulação - Em produção usar Plugin.BLE
            await Task.Delay(2000);
            
            // Dispositivos de exemplo
            dispositivos.Add(new DispositivoBLE 
            { 
                Nome = "ESP32_BLE_001", 
                Id = "AA:BB:CC:DD:EE:FF",
                Rssi = -65
            });
            
            OnDispositivosEncontrados?.Invoke(this, dispositivos);
        }
        finally
        {
            _isScanning = false;
        }
        
        return dispositivos;
    }
    
    public async Task<bool> ConectarAsync(string deviceId)
    {
        try
        {
            _deviceId = deviceId;
            
            // Simulação de conexão
            await Task.Delay(1000);
            
            _isConnected = true;
            OnStatusConexaoChanged?.Invoke(this, true);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro BLE: {ex.Message}");
            _isConnected = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            return false;
        }
    }
    
    public async Task DesconectarAsync()
    {
        try
        {
            // Desconectar BLE
            await Task.Delay(100);
            
            _isConnected = false;
            _deviceId = "";
            OnStatusConexaoChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao desconectar BLE: {ex.Message}");
        }
    }
    
    public async Task<bool> EnviarDadosAsync(string dados)
    {
        if (!_isConnected) return false;
        
        try
        {
            var bytes = Encoding.UTF8.GetBytes(dados);
            
            // Simulação de envio
            await Task.Delay(100);
            
            System.Diagnostics.Debug.WriteLine($"BLE TX: {dados}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao enviar BLE: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> EnviarComandoAsync(string comando)
    {
        return await EnviarDadosAsync($"{comando}\n");
    }
    
    // Comandos comuns
    public Task<bool> LigarLedAsync() => EnviarComandoAsync("LED_ON");
    public Task<bool> DesligarLedAsync() => EnviarComandoAsync("LED_OFF");
    public Task<bool> ToggleLedAsync() => EnviarComandoAsync("LED_TOGGLE");
    public Task<bool> SolicitarStatusAsync() => EnviarComandoAsync("GET_STATUS");
    public Task<bool> SolicitarSensoresAsync() => EnviarComandoAsync("GET_SENSORS");
}

public class DispositivoBLE
{
    public string Nome { get; set; } = "";
    public string Id { get; set; } = "";
    public int Rssi { get; set; }
    
    public string SinalQualidade
    {
        get
        {
            if (Rssi > -50) return "Excelente";
            if (Rssi > -60) return "Muito Bom";
            if (Rssi > -70) return "Bom";
            if (Rssi > -80) return "Regular";
            return "Fraco";
        }
    }
    
    public string IconeSinal
    {
        get
        {
            if (Rssi > -50) return "📶";
            if (Rssi > -60) return "📶";
            if (Rssi > -70) return "📶";
            if (Rssi > -80) return "📶";
            return "📶";
        }
    }
}
