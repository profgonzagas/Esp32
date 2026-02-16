# 📤 Guia de Upload - NecroSENSE ESP32

## Arquivos para Transferir ao ESP32

### ✅ Principal (OBRIGATÓRIO)
- **`ESP32_WiFi_Controller_Complete.ino`** ← **UPLOAD ESTE ARQUIVO**
  - Código completo com todos os sensores integrados
  - BME280 + DHT22 + UV + SD Card
  - WiFi HTTP Server com todos os endpoints
  - Versão: 2.2

### 📚 Testes (Opcional - para debugging)
- `BME280_TEST.ino` - Testar BME280 isoladamente
- `SD_CARD_TEST.ino` - Testar SD Card isoladamente

### 📖 Referência (Não Upload - Consulta no PC)
Mantenha estes arquivos no seu PC para consulta:
- `DHT22_BME280_IMPLEMENTACAO.md` - Especificações dos sensores
- `SD_CARD_INTEGRACAO.md` - Detalhes do SD Card
- `CONEXAO_SD_CARD_VISUAL.md` - Diagrama de conexões
- `README_IMPLEMENTACAO.md` - Guia geral

---

## 🔧 Pré-requisitos

### 1. Arduino IDE Instalada
Baixe em: https://www.arduino.cc/en/software

### 2. Bibliotecas Necessárias
No Arduino IDE, vá para: **Sketch → Include Library → Manage Libraries**

Instale as seguintes:
- **ArduinoJson** (by Benoit Blanchon) - v6.19.4 ou superior
- **Adafruit_BME280** (by Adafruit) - v2.6.9 ou superior
- **DHT sensor library** (by Adafruit) - v1.4.4 ou superior
- **Adafruit Unified Sensor** (by Adafruit) - v1.1.5 ou superior *(dependência do BME280)*

**Nota**: As bibliotecas `WiFi.h`, `WebServer.h`, `Wire.h`, `SD.h`, `SPI.h` e `time.h` já vêm com o ESP32.

---

## 📋 Checklist Antes do Upload

- [ ] Todas as libraries instaladas
- [ ] Arduino IDE aberto com `ESP32_WiFi_Controller_Complete.ino`
- [ ] Porta USB conectada ao ESP32
- [ ] COM Port correto selecionado
- [ ] Board selecionada: **ESP32 Dev Module** (ou variante do seu ESP32)
- [ ] Baud Rate: **115200**
- [ ] WiFi SSID e Senha atualizados no código (veja abaixo)

---

## ⚙️ Configurar WiFi

**Localize estas linhas no código** (linhas 21-22):

```cpp
const char* ssid = "SEU_SSID";           // ← ALTERE AQUI
const char* password = "SUA_SENHA";      // ← ALTERE AQUI
```

**Substitua com suas credenciais WiFi:**
```cpp
const char* ssid = "MinhaRede";
const char* password = "Minha_Senha_123";
```

---

## 🔌 Conexões de Hardware

### BME280 (I2C)
```
ESP32 GPIO21 (SDA) ──→ BME280 SDA
ESP32 GPIO22 (SCL) ──→ BME280 SCL
ESP32 3.3V          ──→ BME280 VCC
ESP32 GND           ──→ BME280 GND
```

### DHT22 (Digital)
```
ESP32 GPIO4  ←──── DHT22 DATA (com resistor pull-up 4.7kΩ)
ESP32 3.3V   ──→ DHT22 +
ESP32 GND    ──→ DHT22 -
```

### GUVA-S12SD (Analógico)
```
ESP32 GPIO34 (ADC) ──→ GUVA-S12SD OUT
ESP32 3.3V         ──→ GUVA-S12SD VCC
ESP32 GND          ──→ GUVA-S12SD GND
```

### SD Card (SPI)
```
ESP32 GPIO18  (CLK)  ──→ SD Adapter CLK / SCK
ESP32 GPIO23  (MOSI) ──→ SD Adapter MOSI / DI / DIN / SI
ESP32 GPIO19  (MISO) ──→ SD Adapter MISO / DO / DOUT / SO
ESP32 GPIO5   (CS)   ──→ SD Adapter CS / CE
ESP32 3.3V           ──→ SD Adapter VCC
ESP32 GND            ──→ SD Adapter GND
```

### LED e Relés
```
ESP32 GPIO2  ──→ LED (com resistor 1kΩ) ──→ GND
ESP32 GPIO26 ──→ Relé 1
ESP32 GPIO27 ──→ Relé 2
```

---

## 📤 Passos de Upload

### 1. Abrir Arduino IDE
```
Arduino IDE → Arquivo → Abrir
→ Selecionar: ESP32_WiFi_Controller_Complete.ino
```

### 2. Selecionar Board e Porta
**Tools → Board:**
```
ESP32 → ESP32 Dev Module
(ou a variante específica do seu ESP32)
```

**Tools → Port:**
```
COM3 (ou a porta onde ESP32 está conectado)
```

**Tools → Upload Speed:**
```
115200
```

### 3. Compilar (Opcional - para verificar erros)
```
Sketch → Verify (Ctrl+R)
```

### 4. Upload do Código
```
Sketch → Upload (Ctrl+U)
```

Aguarde até ver a mensagem:
```
✓ Upload done.
```

### 5. Abrir Serial Monitor
```
Tools → Serial Monitor (Ctrl+Shift+M)
```

Observe a inicialização:
```
╔════════════════════════════════════╗
║   NecroSENSE - Controlador ESP32   ║
║   WiFi HTTP + BME280 + DHT22 + UV  ║
╚════════════════════════════════════╝

=== INICIALIZANDO BME280 ===
✓ BME280 inicializado com sucesso!

=== INICIALIZANDO DHT22 ===
✓ DHT22 inicializado com sucesso!

=== INICIALIZANDO SENSOR UV ===
✓ Sensor UV inicializado com sucesso!

=== INICIALIZANDO CARTÃO SD ===
✓ Cartão SD inicializado com sucesso!

=== INICIALIZANDO WiFi ===
✓ WiFi conectado!
SSID: MinhaRede
IP: 192.168.1.100
```

---

## ✅ Verificar Upload Bem-sucedido

### Serial Monitor (deve mostrar):
- `✓ BME280 inicializado`
- `✓ DHT22 inicializado`
- `✓ Sensor UV inicializado`
- `✓ Cartão SD inicializado`
- `✓ WiFi conectado`
- IP do ESP32 (ex: 192.168.1.100)

### Testar no Web Browser:
Na barra de endereços, acesse:
```
http://192.168.1.100/status
```

Você deve receber JSON similar a:
```json
{
  "status": "conectado",
  "dispositivo": "NecroSENSE ESP32",
  "versao": "2.2",
  "ip": "192.168.1.100",
  "ssid": "MinhaRede",
  "estado_led": false,
  "cartao_sd": "conectado",
  "sensores": {
    "bme280": {
      "temperatura": 24.50,
      "umidade": 45.20,
      "pressao": 1013.25
    },
    "dht22": {
      "temperatura": 24.30,
      "umidade": 46.10
    },
    "uv": {
      "nivel": 450,
      "indice": 7.25
    }
  },
  "uptime": 125
}
```

---

## 🐛 Troubleshooting

### "Falha na conexão WiFi"
- ❌ Verifique SSID e senha (`const char* ssid`, `const char* password`)
- ❌ Verifique se roteador WiFi está ligado
- ❌ Verifique se ESP32 está na mesma rede

### "Não foi possível encontrar o BME280"
- ❌ Verifique conexões I2C (GPIO 21, 22)
- ❌ Verifique voltagem (3.3V)
- ❌ Tente upload do `BME280_TEST.ino` para teste isolado

### "Cartão SD não inicializado"
- ❌ Verifique pinos SPI (GPIO 5, 18, 19, 23)
- ❌ Verifique voltagem do SD (deve ser 3.3V, NÃO 5V!)
- ❌ Cartão SD pode estar corrompido - formate em FAT32
- ❌ Tente upload do `SD_CARD_TEST.ino` para teste isolado

### "Erro ao compilar: biblioteca não encontrada"
- ❌ Abra **Sketch → Include Library → Manage Libraries**
- ❌ Procure e instale: ArduinoJson, Adafruit_BME280, DHT sensor library
- ❌ Reinicie Arduino IDE

### Porta COM não aparece
- ❌ Instale driver USB-to-Serial (CH340 ou similar)
- ❌ Reconecte o cabo USB
- ❌ Tente reboot do PC

---

## 📱 Próximo Passo: Conectar ao App MAUI

Após upload bem-sucedido:

1. Abra o App MAUI no celular
2. Na página de Configurações, insira o **IP do ESP32**:
   - Exemplo: `192.168.1.100`
3. Clique em **Conectar**
4. Navegue para **Dashboard** para ver dados em tempo real

---

## 📞 Documentação Adicional

Consulte no seu PC:
- `DHT22_BME280_IMPLEMENTACAO.md` - Especificações técnicas
- `SD_CARD_INTEGRACAO.md` - Endpoints de SD Card
- `CONEXAO_SD_CARD_VISUAL.md` - Diagrama visual de conexões

---

**Versão**: 2.2  
**Data**: 2024  
**Status**: ✅ Pronto para produção
