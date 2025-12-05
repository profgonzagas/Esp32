# 🔌 ESP32 Controller

Aplicativo .NET MAUI para controlar ESP32 via **WiFi** (HTTP) ou **Bluetooth** (BLE).

## 📱 Screenshots

O app possui 4 telas principais:
- **Dashboard** - Visão geral e comandos rápidos
- **WiFi** - Controle via HTTP (LED, PWM, GPIO)
- **Bluetooth** - Controle via BLE
- **Configurações** - Configurar IP, tema, etc.

## 🚀 Como Usar

### 1. Configurar o ESP32

Carregue um dos códigos Arduino na pasta `ESP32_Arduino_Code/`:

#### WiFi (HTTP):
```cpp
// ESP32_WiFi_Controller.ino
// Altere as configurações:
const char* ssid = "SEU_WIFI_SSID";
const char* password = "SUA_SENHA_WIFI";
```

#### Bluetooth (BLE):
```cpp
// ESP32_BLE_Controller.ino
// Nome do dispositivo:
#define DEVICE_NAME "ESP32_BLE_001"
```

### 2. Configurar o App

1. Abra o app
2. Vá em **Configurações**
3. Digite o **IP do ESP32** (exibido no Serial Monitor)
4. Clique em **Testar Conexão**

### 3. Controlar!

Use os botões do Dashboard ou das páginas WiFi/Bluetooth para:
- Ligar/Desligar LED
- Controlar Relés
- Ajustar PWM
- Definir estados de GPIO
- Enviar comandos customizados

## 📡 Endpoints WiFi

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/status` | Status do dispositivo (JSON) |
| GET | `/led/on` | Liga LED |
| GET | `/led/off` | Desliga LED |
| GET | `/led/toggle` | Alterna LED |
| GET | `/rele/1/on` | Liga Relé 1 |
| GET | `/rele/1/off` | Desliga Relé 1 |
| GET | `/rele/2/on` | Liga Relé 2 |
| GET | `/rele/2/off` | Desliga Relé 2 |
| GET | `/gpio/{pino}/high` | Define pino HIGH |
| GET | `/gpio/{pino}/low` | Define pino LOW |
| GET | `/pwm/{pino}/{valor}` | Define PWM (0-255) |
| GET | `/sensores` | Leitura dos sensores (JSON) |
| GET | `/restart` | Reinicia ESP32 |

## 📶 Comandos BLE

| Comando | Descrição |
|---------|-----------|
| `LED_ON` | Liga LED |
| `LED_OFF` | Desliga LED |
| `LED_TOGGLE` | Alterna LED |
| `RELE1_ON` | Liga Relé 1 |
| `RELE1_OFF` | Desliga Relé 1 |
| `RELE2_ON` | Liga Relé 2 |
| `RELE2_OFF` | Desliga Relé 2 |
| `GET_STATUS` | Retorna status |
| `GET_SENSORS` | Leitura sensores |

## 🔧 UUIDs BLE

```
Service:        4fafc201-1fb5-459e-8fcc-c5c9c331914b
Characteristic TX: beb5483e-36e1-4688-b7f5-ea07361b26a8
Characteristic RX: beb5483e-36e1-4688-b7f5-ea07361b26a9
```

## 🛠️ Compilar o App

### Requisitos
- .NET 9.0 SDK
- Visual Studio 2022 ou VS Code
- Workload MAUI instalado

### Comandos
```bash
# Restaurar pacotes
dotnet restore

# Compilar para Android
dotnet build -f net9.0-android

# Executar no emulador/dispositivo
dotnet build -t:Run -f net9.0-android
```

## 📂 Estrutura do Projeto

```
ESP32Controller/
├── Models/
│   ├── DispositivoESP32.cs
│   ├── ComandoESP32.cs
│   └── LeituraSensor.cs
├── Services/
│   ├── ESP32HttpService.cs
│   ├── ESP32BleService.cs
│   └── ConfiguracaoService.cs
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── WiFiControlViewModel.cs
│   ├── BLEControlViewModel.cs
│   └── ConfiguracoesViewModel.cs
├── Views/
│   ├── DashboardPage.xaml
│   ├── WiFiControlPage.xaml
│   ├── BLEControlPage.xaml
│   └── ConfiguracoesPage.xaml
├── Converters/
│   └── ValueConverters.cs
└── ESP32_Arduino_Code/
    ├── ESP32_WiFi_Controller.ino
    └── ESP32_BLE_Controller.ino
```

## 🔌 Conexões de Hardware

### Pinos Padrão
| Componente | Pino ESP32 |
|------------|------------|
| LED | GPIO 2 (built-in) |
| Relé 1 | GPIO 26 |
| Relé 2 | GPIO 27 |

### Pinos PWM Disponíveis
2, 4, 5, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23, 25, 26, 27, 32, 33

## 📝 Licença

MIT License - Livre para uso pessoal e comercial.

## 🤝 Contribuições

Contribuições são bem-vindas! Abra uma issue ou PR.
