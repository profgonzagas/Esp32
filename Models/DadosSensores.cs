namespace ESP32Controller.Models;

public class DadosSensores
{
    public float Temperatura { get; set; }
    public float Umidade { get; set; }
    public int NivelGas { get; set; }
    public bool ChamaDetectada { get; set; }
    public int NivelSom { get; set; }
    public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;
    
    // Propriedades calculadas
    public bool AlertaGas => NivelGas > 1000;
    public string NivelGasTexto => AlertaGas ? "⚠ ALERTA" : "Normal";
    public string ChamaTexto => ChamaDetectada ? "🔥 DETECTADA" : "Normal";
    public string TemperaturaFormatada => $"{Temperatura:F1}°C";
    public string UmidadeFormatada => $"{Umidade:F1}%";
    public string NivelSomTexto => NivelSom > 2000 ? "Alto" : NivelSom > 1000 ? "Médio" : "Baixo";
}
