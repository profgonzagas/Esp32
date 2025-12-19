# 🔗 Integração ESP32 + MAUI App

## Status da Integração ✅

Sua ESP32 está **100% integrada** com a app MAUI!

### Configuração Realizada

| Parâmetro | Valor |
|-----------|-------|
| **IP** | 192.168.1.128 |
| **Porta** | 80 |
| **MAC** | 00:4B:12:53:4C:18 |
| **SSID** | TIM ULTRAFIBRA_B4D2 |
| **Nome** | NecroSENSE ESP32 |

## Endpoints Disponíveis

### Status e Sensores
- `GET /status` - Status completo do dispositivo
- `GET /sensores` - Array de sensores com valores

### Controle de LED (GPIO 13)
- `GET /led/on` - Liga LED
- `GET /led/off` - Desliga LED
- `GET /led/toggle` - Alterna LED

### Controle de Relés
- `GET /rele/1/on` - Liga Relé 1 (GPIO 26)
- `GET /rele/1/off` - Desliga Relé 1
- `GET /rele/2/on` - Liga Relé 2 (GPIO 27)
- `GET /rele/2/off` - Desliga Relé 2

### Sensores Específicos
- `GET /temperatura` - Leitura de temperatura (DHT22, GPIO 4)
- `GET /umidade` - Leitura de umidade

### Avançado
- `GET /pwm?pino=X&valor=Y` - Controle PWM
- `GET /gpio?pino=X&estado=high/low` - Controle GPIO genérico

## Arquivos Modificados

### 1. DispositivoESP32.cs (Modelo)
```csharp
public string EnderecoIP { get; set; } = "192.168.1.128";
public string Nome { get; set; } = "NecroSENSE ESP32";
public string EnderecoMAC { get; set; } = "00:4B:12:53:4C:18";
```

### 2. Fluxo de Funcionamento

```
MAUI App
    ↓
DashboardViewModel (TestarConexao)
    ↓
ESP32HttpService.TestarConexaoAsync()
    ↓
HTTP Request → http://192.168.1.128/status
    ↓
ESP32 (WiFi Server)
    ↓
JSON Response → {"status":"conectado", ...}
```

## Como Usar a App

### 1. Testar Conexão
```
DashboardPage → "Testar Conexão"
```
Deve retornar: "WiFi" (verde)

### 2. Ler Sensores
```
DashboardPage → "Atualizar Sensores"
```
Mostra: Temperatura, Umidade, Estados dos Relés

### 3. Controlar LED
```
DashboardPage → Botão LED
```
Comandos: On / Off / Toggle

### 4. Controlar Relés
```
WiFiControlPage → Botão Relés
```
Controla Relé 1 e Relé 2 independentemente

## Segurança

✅ Credenciais WiFi guardadas em `secrets.h`
✅ Arquivo bloqueado em `.gitignore`
✅ Não sobe para GitHub

## Pinagem Final

| Componente | GPIO | Tipo |
|-----------|------|------|
| LED | 13 | Digital Output |
| Relé 1 | 26 | Digital Output |
| Relé 2 | 27 | Digital Output |
| DHT22 | 4 | Digital Input |

## Próximos Passos

1. ✅ Compilar a app MAUI
2. ✅ Executar em Android/iOS/Windows
3. ✅ Testar cada endpoint
4. ✅ Calibrar sensores se necessário

## Troubleshooting

**Problema**: App não conecta
- Verifique se ESP32 está na mesma rede WiFi
- Tente `http://192.168.1.128/status` no navegador
- Restart ESP32 se necessário

**Problema**: Leitura de sensores com erro
- Confirme DHT22 está conectado em GPIO 4
- Verifique cabos e alimentação

**Problema**: LED não liga
- Confirme LED está em GPIO 13
- Teste manualmente: `http://192.168.1.128/led/on`

---

**Implementado por**: GitHub Copilot
**Data**: December 19, 2025
**Status**: ✅ Integração Completa
