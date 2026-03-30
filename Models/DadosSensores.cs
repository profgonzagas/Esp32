namespace ESP32Controller.Models;

public class DadosSensores
{
    // BME280 (Sensor de Alta Precisão: Temperatura, Umidade, Pressão)
    public float Temperatura { get; set; }
    public float Umidade { get; set; }
    public float Pressao { get; set; }
    
    // DHT22 (Sensor Digital: Temperatura, Umidade)
    public float TemperaturaDHT22 { get; set; }
    public float UmidadeDHT22 { get; set; }
    
    // UV (GUVA-S12SD: Radiação UV)
    public int NivelUV { get; set; }
    public float IndiceUV { get; set; }

    // LDR (HW-072: Luminosidade)
    public int LdrRaw { get; set; }
    public int LdrMv { get; set; }
    public float LdrPercentual { get; set; }
    public string LdrDescricao { get; set; } = "N/A";
    
    // Outros sensores
    public int NivelGas { get; set; }
    public bool ChamaDetectada { get; set; }
    public int NivelSom { get; set; }
    public DateTime UltimaAtualizacao { get; set; } = DateTime.Now;
    
    // Propriedades calculadas - BME280
    public string TemperaturaFormatada => $"{Temperatura:F1}°C";
    public string UmidadeFormatada => $"{Umidade:F1}%";
    public string PressaoFormatada => $"{Pressao:F2} hPa";
    
    // Propriedades calculadas - DHT22
    public string TemperaturaDHT22Formatada => $"{TemperaturaDHT22:F1}°C";
    public string UmidadeDHT22Formatada => $"{UmidadeDHT22:F1}%";
    
    // Propriedades calculadas - UV
    public string IndiceUVFormatado => $"{IndiceUV:F1}";
    public string ClassificacaoUV => IndiceUV switch
    {
        < 3f => "🟢 Baixo",
        < 6f => "🟡 Moderado",
        < 8f => "🟠 Alto",
        < 11f => "🔴 Muito Alto",
        _ => "🟣 Extremo"
    };
    public string CorUV => IndiceUV switch
    {
        < 3f => "#4CAF50",    // Verde
        < 6f => "#FFC107",    // Amarelo
        < 8f => "#FF9800",    // Laranja
        < 11f => "#F44336",   // Vermelho
        _ => "#9C27B0"        // Roxo
    };
    
    // Propriedades calculadas - LDR
    public string LdrPercentualFormatado => $"{LdrPercentual:F1}%";
    public string CorLDR => LdrPercentual switch
    {
        < 10f  => "#607D8B",  // Cinza (escuro)
        < 30f  => "#795548",  // Marrom (pouca luz)
        < 60f  => "#FF9800",  // Laranja (moderado)
        < 85f  => "#FFC107",  // Âmbar (forte)
        _      => "#FFEB3B"   // Amarelo brilhante (muito forte)
    };

    // Outras propriedades
    public bool AlertaGas => NivelGas > 1000;
    public string NivelGasTexto => AlertaGas ? "⚠ ALERTA" : "Normal";
    public string ChamaTexto => ChamaDetectada ? "🔥 DETECTADA" : "Normal";
    public string NivelSomTexto => NivelSom > 2000 ? "Alto" : NivelSom > 1000 ? "Médio" : "Baixo";
}
