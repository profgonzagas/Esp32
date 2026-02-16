using ESP32Controller.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class BLEControlViewModel : BaseViewModel
{
    private readonly ESP32BleService _bleService;
    
    private bool _conectado;
    private bool _escaneando;
    private string _resposta = "";
    private string _comandoCustom = "";
    private DispositivoBLE? _dispositivoSelecionado;
    private ObservableCollection<DispositivoBLE> _dispositivos = new();
    
    public bool Conectado
    {
        get => _conectado;
        set
        {
            SetProperty(ref _conectado, value);
            OnPropertyChanged(nameof(StatusTexto));
            OnPropertyChanged(nameof(StatusCor));
            OnPropertyChanged(nameof(BotaoConectarTexto));
        }
    }
    
    public bool Escaneando
    {
        get => _escaneando;
        set => SetProperty(ref _escaneando, value);
    }
    
    public string Resposta
    {
        get => _resposta;
        set => SetProperty(ref _resposta, value);
    }
    
    public string ComandoCustom
    {
        get => _comandoCustom;
        set => SetProperty(ref _comandoCustom, value);
    }
    
    public DispositivoBLE? DispositivoSelecionado
    {
        get => _dispositivoSelecionado;
        set => SetProperty(ref _dispositivoSelecionado, value);
    }
    
    public ObservableCollection<DispositivoBLE> Dispositivos
    {
        get => _dispositivos;
        set => SetProperty(ref _dispositivos, value);
    }
    
    public string StatusTexto => Conectado ? "🟢 Conectado via BLE" : "🔵 Desconectado";
    public string StatusCor => Conectado ? "#4CAF50" : "#2196F3";
    public string BotaoConectarTexto => Conectado ? "Desconectar" : "Conectar";
    
    public ICommand EscanearCommand { get; }
    public ICommand ConectarCommand { get; }
    public ICommand SelecionarDispositivoCommand { get; }
    public ICommand EnviarComandoCommand { get; }
    public ICommand LigarLedCommand { get; }
    public ICommand DesligarLedCommand { get; }
    public ICommand ToggleLedCommand { get; }
    public ICommand SolicitarStatusCommand { get; }
    public ICommand MostrarAjudaBLECommand { get; }
    
    public BLEControlViewModel(ESP32BleService bleService)
    {
        _bleService = bleService;
        
        Titulo = "Controle Bluetooth";
        
        EscanearCommand = new Command(async () => await EscanearAsync());
        ConectarCommand = new Command(async () => await ConectarToggleAsync());
        SelecionarDispositivoCommand = new Command<DispositivoBLE>(async (device) => await SelecionarDispositivoAsync(device));
        EnviarComandoCommand = new Command(async () => await EnviarComandoCustomAsync());
        LigarLedCommand = new Command(async () => await ExecutarAsync(_bleService.LigarLedAsync()));
        DesligarLedCommand = new Command(async () => await ExecutarAsync(_bleService.DesligarLedAsync()));
        ToggleLedCommand = new Command(async () => await ExecutarAsync(_bleService.ToggleLedAsync()));
        SolicitarStatusCommand = new Command(async () => await ExecutarAsync(_bleService.SolicitarStatusAsync()));
        MostrarAjudaBLECommand = new Command(() => MostrarAjudaBLEAsync());
        
        _bleService.OnStatusConexaoChanged += (s, c) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Conectado = c);
        };
        
        _bleService.OnMensagemRecebida += (s, msg) =>
        {
            MainThread.BeginInvokeOnMainThread(() => Resposta = msg);
        };
        
        _bleService.OnDispositivosEncontrados += (s, dispositivos) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Dispositivos.Clear();
                foreach (var d in dispositivos)
                {
                    Dispositivos.Add(d);
                }
            });
        };
    }
    
    private async Task EscanearAsync()
    {
        if (Escaneando) return;
        
        Escaneando = true;
        Resposta = "Verificando permissões...";
        
        try
        {
            // Pedir permissões BLE
            bool temPermissoes = await PermissionsService.RequestBluetoothPermissionsAsync();
            
            if (!temPermissoes)
            {
                Resposta = "Permissões de Bluetooth negadas. Ative em Configurações → Aplicativos → Permissões";
                return;
            }
            
            Resposta = "Escaneando dispositivos BLE...";
            var dispositivos = await _bleService.ScanearDispositivosAsync(10);
            
            if (dispositivos.Count == 0)
            {
                Resposta = "Nenhum dispositivo encontrado. Certifique-se que o ESP32 está com BLE ativo.";
            }
            else
            {
                Resposta = $"Encontrados {dispositivos.Count} dispositivo(s)";
            }
        }
        finally
        {
            Escaneando = false;
        }
    }
    
    private async Task ConectarToggleAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            if (Conectado)
            {
                await _bleService.DesconectarAsync();
                Conectado = false;
                Resposta = "🔌 Desconectado";
            }
            else
            {
                if (DispositivoSelecionado == null)
                {
                    Resposta = "Selecione um dispositivo primeiro";
                    return;
                }
                
                Resposta = $"Conectando a {DispositivoSelecionado.Nome}...";
                var conectado = await _bleService.ConectarAsync(DispositivoSelecionado.Id);
                
                Resposta = conectado 
                    ? $"✅ Conectado a {DispositivoSelecionado.Nome}" 
                    : "❌ Falha na conexão";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SelecionarDispositivoAsync(DispositivoBLE device)
    {
        if (device == null || IsBusy) return;
        
        IsBusy = true;
        
        try
        {
            // Se já conectado, desconectar primeiro
            if (Conectado)
            {
                await _bleService.DesconectarAsync();
                Conectado = false;
            }
            
            DispositivoSelecionado = device;
            Resposta = $"Conectando a {device.Nome}...";
            
            var conectado = await _bleService.ConectarAsync(device.Id);
            
            Resposta = conectado 
                ? $"✅ Conectado a {device.Nome}" 
                : $"❌ Falha ao conectar em {device.Nome}";
        }
        catch (Exception ex)
        {
            Resposta = $"❌ Erro: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[BLE] Erro ao selecionar dispositivo: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task EnviarComandoCustomAsync()
    {
        if (IsBusy || string.IsNullOrWhiteSpace(ComandoCustom)) return;
        
        IsBusy = true;
        
        try
        {
            Resposta = $"Enviando: {ComandoCustom}...";
            var resposta = await _bleService.EnviarComandoComRespostaAsync(ComandoCustom, 800);
            
            if (!string.IsNullOrEmpty(resposta))
            {
                Resposta = resposta;
            }
            else
            {
                Resposta = "⚠️ Sem resposta do ESP32";
            }
        }
        catch (Exception ex)
        {
            Resposta = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExecutarAsync(Task<bool> task)
    {
        if (IsBusy || !Conectado) return;
        
        IsBusy = true;
        
        try
        {
            var sucesso = await task;
            Resposta = sucesso ? "Comando enviado!" : "Falha ao enviar";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void MostrarAjudaBLEAsync()
    {
        var ajuda = """
            ℹ️ AJUDA - COMANDOS BLE
            
            📊 SENSORES:
            • GET_SENSORS - Ler todos os sensores (BME280+DHT22+UV)
            • GET_STATUS - Status completo (sensores + LED + relés)
            • DIAGNOSTICO - Verificar disponibilidade dos sensores
            
            💡 CONTROLE:
            • LED_ON - Ligar LED (GPIO 13)
            • LED_OFF - Desligar LED
            • LED_TOGGLE - Alternar LED
            • RELE1_ON - Ligar Relé 1 (GPIO 26)
            • RELE1_OFF - Desligar Relé 1
            • RELE2_ON - Ligar Relé 2 (GPIO 27)
            • RELE2_OFF - Desligar Relé 2
            
            💾 CARTÃO SD:
            • SD_STATUS - Status do SD Card + lista arquivos
            • SD_GRAVAR - Forçar gravação imediata
            • SD_INIT - Reinicializar SD Card
            
            Exemplos:
            GET_SENSORS
            DIAGNOSTICO
            LED_TOGGLE
            SD_STATUS
            """;
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Application.Current?.Windows.Count > 0)
            {
                var window = Application.Current.Windows[0];
                if (window?.Page != null)
                {
                    await window.Page.DisplayAlert("ℹ️ Ajuda BLE", ajuda, "OK");
                }
            }
        });
    }
}
