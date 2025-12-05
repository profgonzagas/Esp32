namespace ESP32Controller.Models;

public class ComandoESP32
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string Metodo { get; set; } = "GET"; // GET, POST
    public string Payload { get; set; } = "";
    public string Icone { get; set; } = "🔘";
    public string Cor { get; set; } = "#4CAF50";
    public bool IsToggle { get; set; } = false;
    public bool Estado { get; set; } = false;
}
