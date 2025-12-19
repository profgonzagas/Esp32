# NecroSENSE - Guia de Implementação ESP32

## 📋 Resumo

Implementei um código completo para ESP32 com suporte a **WiFi + HTTP** que se integra perfeitamente com o aplicativo MAUI. O código fornece todos os endpoints que o aplicativo espera.

## 🔧 Componentes Necessários

### Hardware
- **ESP32** (DevKit)
- **LED** (com resistor de proteção ~330Ω) → GPIO 2
- **Relé 1** (com transistor/opto-isolador) → GPIO 26
- **Relé 2** (com transistor/opto-isolador) → GPIO 27
- **Sensor DHT22** (ou DHT11) → GPIO 4
- **Fonte de alimentação** 5V

### Dependências Arduino
Instale as seguintes bibliotecas no Arduino IDE:
1. **WebServer** (padrão ESP32)
2. **WiFi** (padrão ESP32)
3. **DHT** (Adafruit DHT sensor library)
4. **ArduinoJson** (para manipulação de JSON)

## 📝 Instalação

### 1. Configurar Arduino IDE
```
Arquivo → Preferências → URLs adicionais de gerenciadores de placas
Adicione: https://dl.espressif.com/dl/package_esp32_index.json
```

### 2. Instalar Placa ESP32
```
Ferramentas → Placa → Gerenciador de Placas
Procure por "ESP32" e instale "esp32 by Espressif Systems"
```

### 3. Instalar Bibliotecas
```
Sketch → Incluir biblioteca → Gerenciar bibliotecas
Procure por:
- DHT (instale Adafruit DHT Sensor Library)
- ArduinoJson (instale ArduinoJson)
```

### 4. Configurar Código
Abra `ESP32_WiFi_Controller_Complete.ino` e modifique:

```cpp
const char* ssid = "SEU_SSID";      // Nome da sua rede WiFi
const char* password = "SUA_SENHA";  // Senha da sua rede WiFi
```

### 5. Carregar Código
```
Ferramentas → Placa → ESP32 Dev Module
Ferramentas → Porta → [Selecione a porta COM]
Sketch → Fazer upload
```

## 🌐 Endpoints HTTP

Todos os endpoints retornam JSON.

### Status e Configuração
```
GET /
GET /status
```
Retorna status completo do dispositivo, incluindo IP, força do sinal, estado de todos os periféricos.

### Sensores
```
GET /sensores
GET /temperatura
GET /umidade
```
Retorna leituras dos sensores DHT22.

### Controle de LED
```
GET /led/on      → Liga LED
GET /led/off     → Desliga LED
GET /led/toggle  → Alterna LED
```

### Controle de Relés
```
GET /rele/1/on   → Liga Relé 1
GET /rele/1/off  → Desliga Relé 1
GET /rele/2/on   → Liga Relé 2
GET /rele/2/off  → Desliga Relé 2
```

### Controle Avançado
```
GET /pwm?pino=5&valor=128
GET /gpio?pino=12&estado=high
```

## 📱 Integração com App MAUI

O código implementado é totalmente compatível com:
- `ESP32HttpService` - Envia comandos HTTP
- `DashboardViewModel` - Recebe respostas em JSON
- Todos os endpoints esperados pelo app

### Configuração no App
Em `Services/ESP32HttpService.cs`, a URL base deve ser:
```
http://{IP_DO_ESP32}/
```

## 🔌 Diagrama de Conexão

```
ESP32 DevKit
├── GPIO 2  → LED → GND (com resistor 330Ω)
├── GPIO 26 → Relé 1 (via transistor NPN/MOSFET)
├── GPIO 27 → Relé 2 (via transistor NPN/MOSFET)
└── GPIO 4  → DHT22
    ├── Pino 1 (3V3) → 3V3
    ├── Pino 2 (DATA) → GPIO 4 (com resistor pull-up 4.7kΩ)
    ├── Pino 3 (NC)
    └── Pino 4 (GND) → GND
```

## ✅ Teste Rápido

Após carregar o código:

1. Abra o Monitor Serial (115200 baud)
2. Veja a conexão WiFi sendo estabelecida
3. Anote o IP do ESP32 (ex: 192.168.1.100)
4. Teste em um navegador: `http://192.168.1.100/status`

Deve retornar algo como:
```json
{
  "status": "conectado",
  "dispositivo": "NecroSENSE ESP32",
  "ip": "192.168.1.100",
  "rssi": -45,
  "estado_led": false,
  "temperatura": 28.5,
  "umidade": 65.2
}
```

## 🐛 Troubleshooting

### WiFi não conecta
- [ ] Verifique SSID e senha
- [ ] Verifique se o router suporta 2.4GHz (ESP32 não suporta 5GHz)
- [ ] Verifique se a distância está razoável

### Sensor DHT não lê
- [ ] Verifique conexões
- [ ] Confirme GPIO 4 está correto
- [ ] Verifique se tem resistor pull-up de 4.7kΩ

### App não conecta
- [ ] Verifique se está na mesma rede
- [ ] Confirme o IP do ESP32
- [ ] Teste com navegador primeiro

### Serial mostra erros
- [ ] Verifique baud rate em 115200
- [ ] Resete o ESP32 com o botão "EN"

## 📚 Referências

- [ESP32 Arduino Core](https://github.com/espressif/arduino-esp32)
- [DHT22 Datasheet](https://www.adafruit.com/datasheets/DHT22.pdf)
- [Arduino WebServer](https://github.com/espressif/arduino-esp32/tree/master/libraries/WebServer)

## 🎯 Próximos Passos

1. **Teste cada endpoint** com o navegador
2. **Teste no app MAUI** após confirmar conectividade
3. **Configure credenciais reais** de WiFi
4. **Implemente sensores adicionais** conforme necessário

---

**Desenvolvido para**: NecroSENSE / PCDF  
**Versão**: 1.0  
**Data**: Dezembro 2024
