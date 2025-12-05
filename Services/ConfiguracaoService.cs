using ESP32Controller.Models;
using System.Text.Json;

namespace ESP32Controller.Services;

public class ConfiguracaoService
{
    private const string DISPOSITIVO_KEY = "dispositivo_esp32";
    private const string COMANDOS_KEY = "comandos_customizados";
    
    public DispositivoESP32 CarregarDispositivo()
    {
        try
        {
            var json = Preferences.Get(DISPOSITIVO_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<DispositivoESP32>(json) ?? new DispositivoESP32();
            }
        }
        catch
        {
            // Retornar padrão se houver erro
        }
        
        return new DispositivoESP32();
    }
    
    public void SalvarDispositivo(DispositivoESP32 dispositivo)
    {
        var json = JsonSerializer.Serialize(dispositivo);
        Preferences.Set(DISPOSITIVO_KEY, json);
    }
    
    public List<ComandoESP32> CarregarComandos()
    {
        try
        {
            var json = Preferences.Get(COMANDOS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<List<ComandoESP32>>(json) ?? CriarComandosPadrao();
            }
        }
        catch
        {
            // Retornar padrão se houver erro
        }
        
        return CriarComandosPadrao();
    }
    
    public void SalvarComandos(List<ComandoESP32> comandos)
    {
        var json = JsonSerializer.Serialize(comandos);
        Preferences.Set(COMANDOS_KEY, json);
    }
    
    public void AdicionarComando(ComandoESP32 comando)
    {
        var comandos = CarregarComandos();
        comandos.Add(comando);
        SalvarComandos(comandos);
    }
    
    public void RemoverComando(string id)
    {
        var comandos = CarregarComandos();
        comandos.RemoveAll(c => c.Id == id);
        SalvarComandos(comandos);
    }
    
    private List<ComandoESP32> CriarComandosPadrao()
    {
        return new List<ComandoESP32>
        {
            new ComandoESP32
            {
                Nome = "LED",
                Endpoint = "led/toggle",
                Icone = "💡",
                Cor = "#FFD700",
                IsToggle = true
            },
            new ComandoESP32
            {
                Nome = "Relé 1",
                Endpoint = "rele/1/toggle",
                Icone = "🔌",
                Cor = "#4CAF50",
                IsToggle = true
            },
            new ComandoESP32
            {
                Nome = "Relé 2",
                Endpoint = "rele/2/toggle",
                Icone = "🔌",
                Cor = "#2196F3",
                IsToggle = true
            },
            new ComandoESP32
            {
                Nome = "Status",
                Endpoint = "status",
                Icone = "📊",
                Cor = "#9C27B0",
                IsToggle = false
            },
            new ComandoESP32
            {
                Nome = "Reiniciar",
                Endpoint = "restart",
                Icone = "🔄",
                Cor = "#FF5722",
                IsToggle = false
            }
        };
    }
    
    public void ResetarParaPadrao()
    {
        SalvarComandos(CriarComandosPadrao());
        SalvarDispositivo(new DispositivoESP32());
    }
}
