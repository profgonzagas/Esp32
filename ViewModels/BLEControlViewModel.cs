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
    public ICommand EnviarComandoCommand { get; }
    public ICommand LigarLedCommand { get; }
    public ICommand DesligarLedCommand { get; }
    public ICommand ToggleLedCommand { get; }
    public ICommand SolicitarStatusCommand { get; }
    
    public BLEControlViewModel(ESP32BleService bleService)
    {
        _bleService = bleService;
        
        Titulo = "Controle Bluetooth";
        
        EscanearCommand = new Command(async () => await EscanearAsync());
        ConectarCommand = new Command(async () => await ConectarToggleAsync());
        EnviarComandoCommand = new Command(async () => await EnviarComandoCustomAsync());
        LigarLedCommand = new Command(async () => await ExecutarAsync(_bleService.LigarLedAsync()));
        DesligarLedCommand = new Command(async () => await ExecutarAsync(_bleService.DesligarLedAsync()));
        ToggleLedCommand = new Command(async () => await ExecutarAsync(_bleService.ToggleLedAsync()));
        SolicitarStatusCommand = new Command(async () => await ExecutarAsync(_bleService.SolicitarStatusAsync()));
        
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
        Resposta = "Escaneando dispositivos BLE...";
        
        try
        {
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
                Resposta = "Desconectado";
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
                    ? $"Conectado a {DispositivoSelecionado.Nome}" 
                    : "Falha na conexão";
            }
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
            var sucesso = await _bleService.EnviarComandoAsync(ComandoCustom);
            Resposta = sucesso ? $"Enviado: {ComandoCustom}" : "Falha ao enviar comando";
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
}
