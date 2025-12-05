namespace ESP32Controller.Models;

public class LeituraSensor
{
    public string Nome { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Unidade { get; set; } = "";
    public string Icone { get; set; } = "📊";
    public DateTime DataLeitura { get; set; } = DateTime.Now;
}
