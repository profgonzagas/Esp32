# Implementação: Sensores BME280 + DHT22 no ESP32

## 📋 Resumo
Código atualizado para suportar dois sensores climáticos no ESP32:
- **BME280**: Temperatura, Umidade e **Pressão Atmosférica** (I2C)
- **DHT22**: Temperatura e Umidade (Digital)

---

## 🔧 Hardware - Conexões no ESP32

### BME280 (I2C)
```
BME280  → ESP32
VCC     → 3.3V
GND     → GND
SDA     → GPIO 21
SCL     → GPIO 22
```

### DHT22 (Digital)
```
DHT22     → ESP32
VCC       → 3.3V
GND       → GND
DATA      → GPIO 4 (com resistor 4.7kΩ entre DATA e VCC)
```

### Esquema de Resistor para DHT22
```
        4.7kΩ
VCC ----[===]---- GPIO 4 (DATA)
                  DHT22 DATA
```

---

## 📦 Código Arduino - Bibliotecas Necessárias

Instale as seguintes bibliotecas no Arduino IDE:

1. **Adafruit_BME280** - Para sensor BME280
   - Arduino Library Manager → Procure por "Adafruit BME280"
   - Versão mínima: 2.6.0

2. **DHT Sensor Library** - Para sensor DHT22
   - Arduino Library Manager → Procure por "DHT Sensor Library"
   - Autor: Adafruit
   - Versão mínima: 1.4.0

3. **ArduinoJson** - Para processamento JSON
   - Arduino Library Manager → Procure por "ArduinoJson"
   - Versão mínima: 6.18.0

### Instalação das Bibliotecas
```
Sketch → Include Library → Manage Libraries
Pesquisar por cada uma das bibliotecas acima
Clicar em Install
```

---

## 📡 Endpoints HTTP Disponíveis

### Status e Sensores

#### GET /status
Retorna status completo do dispositivo com dados de ambos os sensores
```json
{
  "status": "conectado",
  "dispositivo": "NecroSENSE ESP32",
  "versao": "2.0",
  "ip": "192.168.1.100",
  "ssid": "SeuWiFi",
  "rssi": -45,
  "estado_led": false,
  "estado_rele1": false,
  "estado_rele2": false,
  "sensores": {
    "bme280": {
      "temperatura": 25.5,
      "umidade": 45.3,
      "pressao": 1013.25
    },
    "dht22": {
      "temperatura": 25.2,
      "umidade": 46.1
    }
  },
  "uptime": 3600
}
```

#### GET /sensores
Retorna array com leitura de todos os sensores
```json
[
  {"nome": "Temperatura (BME280)", "valor": 25.5, "unidade": "°C", "icone": "🌡"},
  {"nome": "Umidade (BME280)", "valor": 45.3, "unidade": "%", "icone": "💧"},
  {"nome": "Pressão (BME280)", "valor": 1013.25, "unidade": "hPa", "icone": "🔽"},
  {"nome": "Temperatura (DHT22)", "valor": 25.2, "unidade": "°C", "icone": "🌡"},
  {"nome": "Umidade (DHT22)", "valor": 46.1, "unidade": "%", "icone": "💧"},
  {"nome": "LED", "valor": 0, "unidade": "bool", "icone": "💡"},
  {"nome": "Relé 1", "valor": 0, "unidade": "bool", "icone": "🔌"},
  {"nome": "Relé 2", "valor": 0, "unidade": "bool", "icone": "🔌"}
]
```

### Sensores - BME280

#### GET /temperatura
Lê apenas temperatura do BME280
```json
{
  "status": "ok",
  "sensor": "BME280",
  "temperatura": 25.5,
  "unidade": "°C"
}
```

#### GET /umidade
Lê apenas umidade do BME280
```json
{
  "status": "ok",
  "sensor": "BME280",
  "umidade": 45.3,
  "unidade": "%"
}
```

#### GET /pressao
Lê pressão atmosférica (BME280)
```json
{
  "status": "ok",
  "pressao": 1013.25,
  "unidade": "hPa"
}
```

### Sensores - DHT22

#### GET /temperatura/dht22
Lê temperatura do DHT22
```json
{
  "status": "ok",
  "sensor": "DHT22",
  "temperatura": 25.2,
  "unidade": "°C"
}
```

#### GET /umidade/dht22
Lê umidade do DHT22
```json
{
  "status": "ok",
  "sensor": "DHT22",
  "umidade": 46.1,
  "unidade": "%"
}
```

### Controle

#### GET /led/on / /led/off / /led/toggle
Controla LED em GPIO 2

#### GET /rele/1/on ; /rele/1/off ; /rele/2/on ; /rele/2/off
Controla relés em GPIO 26 e 27

#### GET /pwm/:pino/:valor
Define PWM em um pino (0-255)

#### GET /gpio/:pino/:state
Define estado de um GPIO (high/low)

---

## 📱 Alterações na App MAUI

### Modelo DadosSensores.cs
Adicionadas propriedades para DHT22:
```csharp
// BME280
public float Temperatura { get; set; }
public float Umidade { get; set; }
public float Pressao { get; set; }

// DHT22
public float TemperaturaDHT22 { get; set; }
public float UmidadeDHT22 { get; set; }

// Propriedades formatadas
public string TemperaturaFormatada => $"{Temperatura:F1}°C";
public string TemperaturaDHT22Formatada => $"{TemperaturaDHT22:F1}°C";
public string UmidadeDHT22Formatada => $"{UmidadeDHT22:F1}%";
```

### Serviço ESP32HttpService.cs
Atualizado para deserializar corretamente:
- Usa endpoint `/status` para obter dados estruturados
- Extrai dados de ambos os sensores (BME280 e DHT22)
- Suporta novos endpoints `/temperatura/dht22` e `/umidade/dht22`

### Interface - SensoresPage.xaml
Nova card para DHT22 com:
- Exibição de temperatura e umidade
- Cor diferenciada (verde) para distinguir do BME280
- Informações sobre conexão e precisão

---

## 🔍 Diferenças: BME280 vs DHT22

| Característica | BME280 | DHT22 |
|---|---|---|
| **Temperatura** | ±1.0°C | ±0.5°C |
| **Umidade** | ±3% | ±2% |
| **Pressão** | Sim ✓ | Não |
| **Interface** | I2C | Digital |
| **Custo** | ~R$40-50 | ~R$20-30 |
| **Tempo de Leitura** | ~10ms | ~2s |
| **Precisão Geral** | Alta/Profissional | Média/Hobby |

**Recomendação**: Use ambos!
- **BME280**: Medições mais precisas, pressão atmosférica
- **DHT22**: Confirmação de leitura, redundância

---

## 🛠️ Troubleshooting

### BME280 não responde
```
✗ Não foi possível encontrar o BME280!

Verifique:
1. Conexões I2C (SDA=GPIO21, SCL=GPIO22)
2. Tensão de 3.3V
3. Resistores pull-up (geralmente já inclusos)
4. Endereço I2C correto (0x77)
```

### DHT22 com leituras erradas
```
[DHT22] ✗ Erro na leitura do DHT22

Verifique:
1. Resistor 4.7kΩ entre DATA e VCC
2. Fio DATA em GPIO 4
3. Alimentação em 3.3V (nunca 5V!)
4. Tempo mínimo de 2 segundos entre leituras
```

### WiFi não conecta
```
Altere em ESP32_WiFi_Controller_Complete.ino:
const char* ssid = "SEU_SSID";
const char* password = "SUA_SENHA";
```

---

## 📊 Exemplo de Uso via cURL

```bash
# Obter status completo
curl http://192.168.1.100/status

# Apenas temperatura BME280
curl http://192.168.1.100/temperatura

# Apenas umidade DHT22
curl http://192.168.1.100/umidade/dht22

# Todos os sensores
curl http://192.168.1.100/sensores
```

---

## 📈 Melhorias Futuras

- [ ] Logging de dados em SPIFFS/SD Card
- [ ] Dashboard com gráficos históricos
- [ ] Alertas por email/SMS
- [ ] Integração com Home Assistant
- [ ] Sincronização com nuvem (Firebase/ThingSpeak)
- [ ] Calibração automática de sensores

---

## ✅ Verificação Final

Após upload, verifique no Monitor Serial:
```
═══════════════════════════════════════════════
   NecroSENSE - Controlador ESP32
   WiFi HTTP + BME280 + DHT22
═══════════════════════════════════════════════

=== INICIALIZANDO BME280 ===
✓ BME280 inicializado com sucesso!

=== INICIALIZANDO DHT22 ===
✓ DHT22 inicializado com sucesso!
  Pino: GPIO 4

=== INICIALIZANDO WiFi ===
Conectando a WiFi: SeuWiFi
...........................
✓ WiFi conectado!
SSID: SeuWiFi
IP: 192.168.1.100

=== ENDPOINTS DISPONÍVEIS ===
GET /status
GET /sensores
...

[BME280] 🌡 Temperatura: 25.5 °C | 💧 Umidade: 45.3 % | 🔽 Pressão: 1013.25 hPa
[DHT22] 🌡 Temperatura: 25.2 °C | 💧 Umidade: 46.1 %
```

---

Pronto! Sistema com dois sensores climáticos funcionando no ESP32 e app MAUI! 🎉
