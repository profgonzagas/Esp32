# Implementação BME280 - Guia Rápido

## ✅ O que foi implementado

### 1. **Código Arduino (ESP32)**
- ✓ Substitui DHT22 por BME280
- ✓ Lê temperatura, umidade e **pressão**
- ✓ Novos endpoints HTTP:
  - `GET /pressao` - obtém apenas a pressão
  - `GET /sensores` - inclui pressão na array
  - `GET /status` - campo pressão adicionado

### 2. **App Mobile (MAUI)**
- ✓ Modelo `DadosSensores` com novo campo `Pressao`
- ✓ Propriedade formatada `PressaoFormatada`
- ✓ View atualizada com card do BME280
- ✓ Display de 3 valores: Temperatura, Umidade, Pressão

## 🔧 Próximos Passos

### Passo 1: Instalar Biblioteca Arduino
```
Arduino IDE:
1. Abrir: Sketch → Include Library → Manage Libraries
2. Procurar: "Adafruit BME280"
3. Instalar: Adafruit BME280 Library (versão mais recente)
4. Instalar também: Adafruit Unified Sensor (dependência)
```

### Passo 2: Conectar Sensor ao ESP32
```
BME280 → ESP32
VCC    → 3.3V
GND    → GND
SDA    → GPIO 21
SCL    → GPIO 22
```

### Passo 3: Upload do Código
1. Abrir: `ESP32_Arduino_Code\ESP32_WiFi_Controller_Complete.ino`
2. Verificar SSID e senha WiFi
3. Compilar: Sketch → Verify
4. Upload: Sketch → Upload

### Passo 4: Testar
```
Abrir Serial Monitor (115200 baud):
- Verificar "BME280 inicializado com sucesso!"
- Verificar leituras: Temperatura, Umidade, Pressão

Testar endpoints no navegador:
- http://192.168.X.X/sensores
- http://192.168.X.X/pressao
- http://192.168.X.X/temperatura
- http://192.168.X.X/umidade
```

### Passo 5: Testar App Mobile
1. Compilar projeto MAUI
2. Abrir "Sensores"
3. Verificar card BME280 com os 3 valores

## 📊 Valores Esperados

```
Temperatura:  -40°C a +85°C (precisão ±0.5°C)
Umidade:      0% a 100% (precisão ±3%)
Pressão:      300 a 1100 hPa (precisão ±1 hPa)

Valores típicos em interior:
Temperatura:  20°C a 25°C
Umidade:      40% a 60%
Pressão:      1010 a 1020 hPa
```

## 🆘 Se não funcionar

### Erro: "✗ BME280 não encontrado!"
1. Verificar cabos (SDA/SCL podem estar trocados)
2. Verificar voltagem (deve ser exatamente 3.3V)
3. Verificar endereço I2C (padrão 0x77, podem ser alguns modelos com 0x76)

**Solução rápida**: Editar linha no .ino:
```cpp
// Mudar de:
if (!bme280.begin(BME280_ADDRESS)) // 0x77

// Para:
if (!bme280.begin(0x76)) // Se o sensor usa este endereço
```

### Erro: "BME280_ADDRESS not declared"
- Adicionar a biblioteca Adafruit: `#include <Adafruit_BME280.h>`

### Leituras muito diferentes da realidade
- Sensor precisa ~30 segundos para calibração
- Pressão deve estar próxima a 1013 hPa ao nível do mar

## 📁 Arquivos Modificados

- ✓ `ESP32_Arduino_Code/ESP32_WiFi_Controller_Complete.ino`
- ✓ `Models/DadosSensores.cs`
- ✓ `Views/SensoresPage.xaml`
- ✓ `BME280_SETUP.md` (documentação completa)

## 💡 Features do BME280

- **Sensor digital**: Interface I2C/SPI
- **Alta precisão**: ±0.5°C para temperatura
- **Baixo consumo**: 3.4µA em standby
- **Rápido**: Leitura em ~1 segundo
- **Robusto**: Operação de -40°C a +85°C

---

**Status**: ✅ Implementação Completa  
**Data**: Fevereiro 2026  
**Próximo**: Upload para ESP32 e teste
