namespace ESP32Controller.Models;

public class DispositivoESP32
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = "ESP32";
    public string EnderecoIP { get; set; } = "192.168.1.100";
    public int Porta { get; set; } = 80;
    public string EnderecoMAC { get; set; } = "";
    public bool ConectadoWiFi { get; set; }
    public bool ConectadoBLE { get; set; }
    public DateTime UltimaConexao { get; set; }
    
    public string UrlBase => $"http://{EnderecoIP}:{Porta}";
}
