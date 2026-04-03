using ESP32Controller.Models;
using ESP32Controller.Services;
using Microcharts;
using SkiaSharp;
using System.Windows.Input;

namespace ESP32Controller.ViewModels;

public class GraficosViewModel : BaseViewModel
{
    private readonly MqttService _mqttService;
    private Chart? _graficoTemperatura;
    private Chart? _graficoUmidade;
    private Chart? _graficoPressao;
    private Chart? _graficoUV;
    private Chart? _graficoLuminosidade;
    private Chart? _graficoTempDHT;
    private string _statusMensagem = "";
    private bool _mqttConectado;
    private DadosSensores? _ultimoDado;

    public Chart? GraficoTemperatura
    {
        get => _graficoTemperatura;
        set => SetProperty(ref _graficoTemperatura, value);
    }

    public Chart? GraficoUmidade
    {
        get => _graficoUmidade;
        set => SetProperty(ref _graficoUmidade, value);
    }

    public Chart? GraficoPressao
    {
        get => _graficoPressao;
        set => SetProperty(ref _graficoPressao, value);
    }

    public Chart? GraficoUV
    {
        get => _graficoUV;
        set => SetProperty(ref _graficoUV, value);
    }

    public Chart? GraficoLuminosidade
    {
        get => _graficoLuminosidade;
        set => SetProperty(ref _graficoLuminosidade, value);
    }

    public Chart? GraficoTempDHT
    {
        get => _graficoTempDHT;
        set => SetProperty(ref _graficoTempDHT, value);
    }

    public string StatusMensagem
    {
        get => _statusMensagem;
        set => SetProperty(ref _statusMensagem, value);
    }

    public bool MqttConectado
    {
        get => _mqttConectado;
        set => SetProperty(ref _mqttConectado, value);
    }

    public DadosSensores? UltimoDado
    {
        get => _ultimoDado;
        set => SetProperty(ref _ultimoDado, value);
    }

    public ICommand ConectarMQTTCommand { get; }
    public ICommand AtualizarGraficosCommand { get; }

    public GraficosViewModel(MqttService mqttService)
    {
        _mqttService = mqttService;
        Titulo = "Gráficos";

        ConectarMQTTCommand = new Command(async () => await ConectarMQTTAsync());
        AtualizarGraficosCommand = new Command(AtualizarGraficos);

        _mqttService.OnDadosSensoresRecebidos += OnDadosRecebidos;
        _mqttService.OnStatusConexaoChanged += (s, c) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MqttConectado = c;
                StatusMensagem = c ? "🌐 MQTT conectado - gráficos em tempo real" : "⚠ MQTT desconectado";
            });
        };

        MqttConectado = _mqttService.IsConectado;
        if (MqttConectado)
            StatusMensagem = "🌐 MQTT conectado - aguardando dados...";
        else
            StatusMensagem = "Conecte ao MQTT para ver gráficos";

        // Carregar histórico existente
        AtualizarGraficos();
    }

    private void OnDadosRecebidos(object? sender, DadosSensores dados)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UltimoDado = dados;
            AtualizarGraficos();
            StatusMensagem = $"🌐 Atualizado {dados.UltimaAtualizacao:HH:mm:ss} ({_mqttService.Historico.Count} pontos)";
        });
    }

    private async Task ConectarMQTTAsync()
    {
        if (_mqttService.IsConectado)
        {
            await _mqttService.DesconectarAsync();
            return;
        }

        StatusMensagem = "Conectando ao MQTT...";
        IsBusy = true;
        try
        {
            var ok = await _mqttService.ConectarAsync();
            StatusMensagem = ok ? "🌐 Conectado! Aguardando dados..." : "Falha ao conectar";
        }
        catch (Exception ex)
        {
            StatusMensagem = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void AtualizarGraficos()
    {
        var historico = _mqttService.Historico;
        if (historico.Count == 0)
        {
            CriarGraficosVazios();
            return;
        }

        // Cores iguais ao dashboard web
        GraficoTemperatura = CriarGraficoArea(
            historico, d => d.Temperatura, "°C",
            SKColor.Parse("#E85D5D"));

        GraficoUmidade = CriarGraficoArea(
            historico, d => d.Umidade, "%",
            SKColor.Parse("#2E86DE"));

        GraficoPressao = CriarGraficoArea(
            historico, d => d.Pressao, "hPa",
            SKColor.Parse("#26D07C"));

        GraficoTempDHT = CriarGraficoArea(
            historico, d => d.TemperaturaDHT22, "°C",
            SKColor.Parse("#F7A41D"));

        GraficoUV = CriarGraficoArea(
            historico, d => d.IndiceUV, "UV",
            SKColor.Parse("#A371F7"));

        GraficoLuminosidade = CriarGraficoArea(
            historico, d => d.LdrPercentual, "%",
            SKColor.Parse("#F0C674"));
    }

    private static LineChart CriarGraficoArea(
        IReadOnlyList<DadosSensores> historico,
        Func<DadosSensores, float> selector,
        string sufixo,
        SKColor cor)
    {
        var entries = new List<ChartEntry>();
        int passo = Math.Max(1, historico.Count / 10);

        for (int i = 0; i < historico.Count; i++)
        {
            var d = historico[i];
            var valor = selector(d);
            entries.Add(new ChartEntry(valor)
            {
                Color = cor,
                Label = i % passo == 0 ? d.UltimaAtualizacao.ToString("HH:mm") : "",
                ValueLabel = i == historico.Count - 1 ? $"{valor:F1}{sufixo}" : ""
            });
        }

        float minVal = entries.Min(e => e.Value ?? 0);
        float maxVal = entries.Max(e => e.Value ?? 0);
        float margin = Math.Max((maxVal - minVal) * 0.1f, 1f);

        return new LineChart
        {
            Entries = entries,
            LineMode = LineMode.Spline,
            LineSize = 4,
            PointMode = PointMode.None,
            BackgroundColor = SKColors.Transparent,
            LabelColor = SKColor.Parse("#555555"),
            LabelTextSize = 22,
            AnimationDuration = TimeSpan.FromMilliseconds(300),
            MinValue = minVal - margin,
            MaxValue = maxVal + margin
        };
    }

    private void CriarGraficosVazios()
    {
        var entryVazio = new[] { new ChartEntry(0) { Color = SKColor.Parse("#333333"), Label = "Sem dados" } };
        var chart = new LineChart { Entries = entryVazio, BackgroundColor = SKColors.Transparent, LineMode = LineMode.Spline };
        GraficoTemperatura = chart;
        GraficoUmidade = chart;
        GraficoPressao = chart;
        GraficoTempDHT = chart;
        GraficoUV = chart;
        GraficoLuminosidade = chart;
    }
}
