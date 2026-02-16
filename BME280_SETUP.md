# 🌡️ Sensor BME280 - Setup e Implementação

## Sobre o Sensor

O **BME280** é um sensor de alta precisão 3.3V que mede:
- **Temperatura**: -40°C a +85°C (precisão ±0.5°C)
- **Umidade**: 0% a 100% (precisão ±3%)
- **Pressão**: 300 hPa a 1100 hPa (precisão ±1 hPa)

## 🔧 Conexão com ESP32

### Pinagem I2C

| BME280 | ESP32 | Cor do Fio (Recomendado) |
|--------|-------|--------------------------|
| VCC    | 3.3V  | Vermelho                 |
| GND    | GND   | Preto                    |
| SDA    | GPIO 21 | Azul                   |
| SCL    | GPIO 22 | Amarelo                 |

### Diagrama de Conexão

```
BME280
  |
  ├─ VCC ──────→ 3.3V (ESP32)
  ├─ GND ──────→ GND  (ESP32)
  ├─ SDA ──────→ GPIO 21 (I2C_SDA)
  └─ SCL ──────→ GPIO 22 (I2C_SCL)
```

## 📦 Bibliotecas Necessárias

No Arduino IDE, instale:

1. **Adafruit BME280 Library**
   - Link: https://github.com/adafruit/Adafruit_BME280_Library
   - Instalação via Arduino IDE (Sketch → Include Library → Manage Libraries)

2. **Adafruit Unified Sensor Library** (dependência)
   - Necessária para o BME280

```bash
# Alternativa via CLI
arduino-cli lib install "Adafruit BME280 Library"
arduino-cli lib install "Adafruit Unified Sensor"
```

## 🚀 Código Arduino

O código já foi atualizado em `ESP32_WiFi_Controller_Complete.ino`:

```cpp
// Inicializar I2C e sensor
Wire.begin(SDA_PIN, SCL_PIN);
Adafruit_BME280 bme280;

if (!bme280.begin(BME280_ADDRESS)) {
  Serial.println("✗ BME280 não encontrado!");
}

// Ler valores
float temperatura = bme280.readTemperature();   // em °C
float umidade = bme280.readHumidity();          // em %
float pressao = bme280.readPressure() / 100.0F; // em hPa
```

## 🌐 Endpoints HTTP

A app mobile acessa os dados via:

### GET /sensores
Retorna uma array com todos os sensores incluindo BME280:
```json
[
  {"nome":"Temperatura","valor":25.50,"unidade":"°C","icone":"🌡"},
  {"nome":"Umidade","valor":45.20,"unidade":"%","icone":"💧"},
  {"nome":"Pressão","valor":1013.25,"unidade":"hPa","icone":"🔽"},
  ...
]
```

### GET /temperatura
```json
{"status":"ok","temperatura":25.50,"unidade":"°C"}
```

### GET /umidade
```json
{"status":"ok","umidade":45.20,"unidade":"%"}
```

### GET /pressao
```json
{"status":"ok","pressao":1013.25,"unidade":"hPa"}
```

### GET /status
Retorna status completo incluindo leituras do BME280

## 📱 Integração na App MAUI

### Modelo (C#)
```csharp
public class DadosSensores
{
    public float Temperatura { get; set; }
    public float Umidade { get; set; }
    public float Pressao { get; set; }  // NOVO
    
    public string PressaoFormatada => $"{Pressao:F2} hPa";
}
```

### View XAML
A página de sensores agora exibe os 3 dados do BME280:
- Card com suporte a temperatura, umidade e pressão
- Painel de informações sobre a qualidade do sensor

## 🔍 Troubleshooting

### Problema: "✗ BME280 não encontrado!"

1. **Verificar conexão física**
   - Verificar se todos os fios estão bem conectados
   - Usar multímetro para testar voltagem (3.3V)

2. **Testar endereço I2C**
   ```cpp
   // I2C Scanner
   #include <Wire.h>
   void setup() {
     Wire.begin(21, 22);
     Serial.begin(115200);
     Serial.println("I2C Scanner");
   }
   
   void loop() {
     for (int i = 0; i < 128; i++) {
       Wire.beginTransmission(i);
       if (Wire.endTransmission() == 0)
         Serial.printf("Device found at 0x%02X\n", i);
     }
     delay(5000);
   }
   ```

3. **Verificar bibliotecas instaladas**
   - Abrir Sketch → Include Library → Manage Libraries
   - Procurar por "Adafruit BME280"

### Problema: Valores de leitura inconsistentes

1. **Estabilização do sensor**
   - BME280 precisa de ~2 segundos para estabilizar após reset
   - Implementado no setup com delay

2. **Calibração**
   - Para pressão, considerar pressão ao nível do mar
   - Para melhor precisão, usar múltiplas leituras

## 📊 Especificações Completas

| Parâmetro | Valor |
|-----------|-------|
| Tensão de Operação | 1.71V a 3.6V (3.3V recomendado) |
| Consumo Típico | 3.4 µA (standby) / 3.45 mA (normal) |
| Temperatura de Operação | -40°C a +85°C |
| Interface | I2C (até 3.4 MHz) / SPI |
| Tempo de Inicialização | ~2ms |
| Pressão / Umidade / Temperatura | 1 / 1 / 1 segundo |

## 📚 Referências

- [Adafruit BME280 Library Github](https://github.com/adafruit/Adafruit_BME280_Library)
- [Datasheet BME280](https://www.bosch-sensortec.com/products/environmental-sensors/humidity-sensors-bme280/)
- [Exemplo ESP32 I2C](https://github.com/espressif/esp-idf/tree/master/examples/peripherals/i2c)

## ✅ Checklist de Implementação

- [x] Bibliotecas instaladas (Adafruit BME280)
- [x] Código Arduino atualizado com BME280
- [x] Endpoints HTTP implementados
- [x] Modelo C# com campo Pressão
- [x] View XAML atualizada
- [ ] Testar conexão física do sensor
- [ ] Compilar e fazer upload para ESP32
- [ ] Testar endpoints no navegador
- [ ] Testar requisições na app mobile

---

**Última atualização**: Fevereiro 2026
**Status**: Implementação completa ✓
