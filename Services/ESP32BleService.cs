using ESP32Controller.Models;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using System.Text;

namespace ESP32Controller.Services;

public class ESP32BleService
{
    private bool _isScanning;
    private bool _isConnected;
    private string _deviceId = "";
    private IDevice? _connectedDevice;
    private IService? _service;
    private ICharacteristic? _txCharacteristic;
    private ICharacteristic? _rxCharacteristic;
    private IBluetoothLE? _ble;
    private IAdapter? _adapter;
    
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
        try
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Inicializando BLE Service...");
            
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            
            if (_ble == null)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] ❌ BLE não disponível no dispositivo!");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[BLE] ✓ BLE disponível - Estado: {(_ble.IsOn ? "ON" : "OFF")}");
            
            if (_adapter != null)
            {
                // Subscrever eventos do adaptador
                _adapter.DeviceDiscovered += OnDeviceDiscovered;
                _adapter.DeviceConnected += OnDeviceConnected;
                _adapter.DeviceDisconnected += OnDeviceDisconnected;
                
                System.Diagnostics.Debug.WriteLine("[BLE] ✓ Adapter inicializado e eventos subscritos");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BLE] ❌ Adapter retornou NULL!");
            }
            
            System.Diagnostics.Debug.WriteLine("[BLE] Serviço BLE inicializado com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] ❌ ERRO ao inicializar: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[BLE] Mensagem: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BLE] Stack: {ex.StackTrace}");
        }
    }
    
    private void OnDeviceDiscovered(object? sender, DeviceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[BLE] Dispositivo encontrado: {e.Device.Name} ({e.Device.Id})");
    }
    
    private void OnDeviceConnected(object? sender, DeviceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[BLE] Conectado em: {e.Device.Name}");
    }
    
    private void OnDeviceDisconnected(object? sender, DeviceEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[BLE] Desconectado de: {e.Device.Name}");
        _isConnected = false;
        OnStatusConexaoChanged?.Invoke(this, false);
    }
    
    public async Task<List<DispositivoBLE>> ScanearDispositivosAsync(int timeoutSeconds = 10)
    {
        var dispositivos = new List<DispositivoBLE>();
        _isScanning = true;
        
        try
        {
            System.Diagnostics.Debug.WriteLine("[BLE] ===== INICIANDO SCAN =====");
            
            // 🔴 IMPORTANTE: Solicitar permissões ANTES de iniciar scan
            System.Diagnostics.Debug.WriteLine("[BLE] Solicitando permissões de Bluetooth...");
            bool permissionsGranted = await PermissionsService.RequestBluetoothPermissionsAsync();
            System.Diagnostics.Debug.WriteLine($"[BLE] Permissões: {(permissionsGranted ? "✓ ACEITAS" : "❌ NEGADAS")}");
            
            if (!permissionsGranted)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] ❌ Permissões de Bluetooth foram negadas!");
                OnDispositivosEncontrados?.Invoke(this, dispositivos);
                return dispositivos;
            }
            
            if (_ble == null || _adapter == null)
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] ❌ BLE={_ble} | Adapter={_adapter}");
                OnDispositivosEncontrados?.Invoke(this, dispositivos);
                return dispositivos;
            }
            
            System.Diagnostics.Debug.WriteLine($"[BLE] Estado do Bluetooth: {(_ble.IsOn ? "✓ ON" : "❌ OFF")}");
            
            // Verificar estado do Bluetooth
            if (!_ble.IsOn)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] ❌ Bluetooth está desligado - ative no celular!");
                OnDispositivosEncontrados?.Invoke(this, dispositivos);
                return dispositivos;
            }
            
            System.Diagnostics.Debug.WriteLine($"[BLE] Iniciando scan por {timeoutSeconds}s...");
            
            // Escanear por dispositivos
            await _adapter.StartScanningForDevicesAsync();
            System.Diagnostics.Debug.WriteLine("[BLE] Scanning iniciado");
            
            // Aguardar timeout
            await Task.Delay(timeoutSeconds * 1000);
            
            await _adapter.StopScanningForDevicesAsync();
            System.Diagnostics.Debug.WriteLine("[BLE] Scanning parado");
            
            // Converter para modelo da aplicação
            var count = _adapter.DiscoveredDevices.Count;
            System.Diagnostics.Debug.WriteLine($"[BLE] Total encontrado: {count} dispositivos");
            
            foreach (var device in _adapter.DiscoveredDevices)
            {
                dispositivos.Add(new DispositivoBLE
                {
                    Nome = device.Name ?? "Desconhecido",
                    Id = device.Id.ToString(),
                    Rssi = device.Rssi
                });
                
                System.Diagnostics.Debug.WriteLine($"[BLE]   → {device.Name} ({device.Id}) - RSSI: {device.Rssi} dBm");
            }
            
            OnDispositivosEncontrados?.Invoke(this, dispositivos);
            System.Diagnostics.Debug.WriteLine($"[BLE] Scan concluído - {dispositivos.Count} dispositivos retornados");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] ❌ ERRO ao escanear: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[BLE] Mensagem: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[BLE] Stack: {ex.StackTrace}");
        }
        finally
        {
            _isScanning = false;
            System.Diagnostics.Debug.WriteLine("[BLE] ===== FIM DO SCAN =====");
        }
        
        return dispositivos;
    }
    
    public async Task<bool> ConectarAsync(string deviceId)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Tentando conectar em: {deviceId}");
            
            if (_ble == null || _adapter == null)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] BLE ou Adapter não disponível");
                return false;
            }
            
            if (!_ble.IsOn)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] Bluetooth está desligado");
                return false;
            }
            
            // Encontrar dispositivo
            Guid deviceGuid;
            if (!Guid.TryParse(deviceId, out deviceGuid))
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] ID inválido: {deviceId}");
                return false;
            }
            
            var device = _adapter.DiscoveredDevices.FirstOrDefault(d => d.Id == deviceGuid);
            if (device == null)
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] Dispositivo não encontrado: {deviceId}");
                return false;
            }
            
            // Conectar
            await _adapter.ConnectToDeviceAsync(device);
            _connectedDevice = device;
            _deviceId = deviceId;
            
            System.Diagnostics.Debug.WriteLine($"[BLE] Conectado em: {device.Name}");
            
            // Obter serviço
            var services = await device.GetServicesAsync();
            _service = services.FirstOrDefault(s => s.Id == ServiceUUID);
            
            if (_service == null)
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] Serviço não encontrado: {ServiceUUID}");
                await _adapter.DisconnectDeviceAsync(device);
                return false;
            }
            
            // Obter características
            var characteristics = await _service.GetCharacteristicsAsync();
            _txCharacteristic = characteristics.FirstOrDefault(c => c.Id == CharacteristicTxUUID);
            _rxCharacteristic = characteristics.FirstOrDefault(c => c.Id == CharacteristicRxUUID);
            
            if (_txCharacteristic == null || _rxCharacteristic == null)
            {
                System.Diagnostics.Debug.WriteLine("[BLE] Características não encontradas");
                await _adapter.DisconnectDeviceAsync(device);
                return false;
            }
            
            // Subscrever notificações
            if (_rxCharacteristic.CanUpdate)
            {
                await _rxCharacteristic.StartUpdatesAsync();
                _rxCharacteristic.ValueUpdated += OnCharacteristicValueUpdated;
                System.Diagnostics.Debug.WriteLine("[BLE] Subscrições de notificação ativadas");
            }
            
            _isConnected = true;
            OnStatusConexaoChanged?.Invoke(this, true);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Erro ao conectar: {ex.Message}");
            _isConnected = false;
            OnStatusConexaoChanged?.Invoke(this, false);
            return false;
        }
    }
    
    private void OnCharacteristicValueUpdated(object? sender, CharacteristicUpdatedEventArgs e)
    {
        try
        {
            if (e.Characteristic.Value != null)
            {
                var mensagem = Encoding.UTF8.GetString(e.Characteristic.Value);
                System.Diagnostics.Debug.WriteLine($"[BLE] Dados recebidos: {mensagem}");
                OnMensagemRecebida?.Invoke(this, mensagem);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Erro ao processar notificação: {ex.Message}");
        }
    }
    
    public async Task DesconectarAsync()
    {
        try
        {
            if (_connectedDevice != null)
            {
                // Parar notificações da característica TX (a que tem notify)
                if (_txCharacteristic != null && _txCharacteristic.CanUpdate)
                {
                    await _txCharacteristic.StopUpdatesAsync();
                    _txCharacteristic.ValueUpdated -= OnCharacteristicValueUpdated;
                }
                
                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                System.Diagnostics.Debug.WriteLine("[BLE] Desconectado");
            }
            
            _isConnected = false;
            _deviceId = "";
            _connectedDevice = null;
            _service = null;
            _txCharacteristic = null;
            _rxCharacteristic = null;
            OnStatusConexaoChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Erro ao desconectar: {ex.Message}");
        }
    }
    
    public async Task<bool> EnviarDadosAsync(string dados)
    {
        if (!_isConnected || _rxCharacteristic == null) 
            return false;
        
        try
        {
            var bytes = Encoding.UTF8.GetBytes(dados);
            
            // Enviar dados via BLE (escrever na característica RX do ESP32)
            await _rxCharacteristic.WriteAsync(bytes);
            
            System.Diagnostics.Debug.WriteLine($"[BLE] TX→RX: {dados}");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Erro ao enviar: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> EnviarComandoAsync(string comando)
    {
        return await EnviarDadosAsync($"{comando}\n");
    }
    
    // Comandos comuns
    public Task<bool> LigarLedAsync() => EnviarComandoAsync("led:on");
    public Task<bool> DesligarLedAsync() => EnviarComandoAsync("led:off");
    public Task<bool> ToggleLedAsync() => EnviarComandoAsync("led:toggle");
    public Task<bool> SolicitarStatusAsync() => EnviarComandoAsync("status");
    public Task<bool> SolicitarSensoresAsync() => EnviarComandoAsync("sensores");
    public Task<bool> SolicitarTemperaturaAsync() => EnviarComandoAsync("temperatura");
    public Task<bool> SolicitarUmidadeAsync() => EnviarComandoAsync("umidade");
    public Task<bool> SolicitarGasAsync() => EnviarComandoAsync("gas");
    public Task<bool> SolicitarChamaAsync() => EnviarComandoAsync("chama");
    public Task<bool> SolicitarSomAsync() => EnviarComandoAsync("som");
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
    
    public string IconeSinal => "📶";
}
