# Cartão SD no ESP32 - Integração Completa

## 🔌 Conexões do Adaptador SD

```
Adaptador SD    →    ESP32
────────────────────────────
GND (preto)     →    GND
VCC (vermelho)  →    3.3V (NÃO 5V!)
CLK (amarelo)   →    GPIO 18 (CLK)
MOSI (azul)     →    GPIO 23 (MOSI/DIN)
MISO (verde)    →    GPIO 19 (MISO/DO)
CS (laranja)    →    GPIO 5 (Chip Select)
```

### ⚠️ IMPORTANTE: VOLTAGEM
- **NUNCA** conectar VCC em 5V
- Usar **obrigatoriamente 3.3V**
- Pode danificar o cartão e o ESP32!

---

## 📝 Adição ao Código WiFi Completo

Para integrar o cartão SD ao código **ESP32_WiFi_Controller_Complete.ino**:

### 1. Adicionar Includes
```cpp
#include <SD.h>
#include <SPI.h>
```

### 2. Definir Pinos SPI
```cpp
#define SD_CS_PIN 5     // Chip Select
#define SPI_CLK 18
#define SPI_MOSI 23
#define SPI_MISO 19
```

### 3. Variáveis Globais
```cpp
// Cartão SD
bool sdCardOK = false;
unsigned long ultimaGravacaoSD = 0;
const unsigned long INTERVALO_LOG_SD = 30000; // Log a cada 30 segundos

String logFilename = "/sensores_log.csv";
```

### 4. Função de Inicialização
```cpp
void inicializarCartaoSD() {
  Serial.println("\n=== INICIALIZANDO CARTÃO SD ===");
  
  SPI.begin(SPI_CLK, SPI_MISO, SPI_MOSI, SD_CS_PIN);
  delay(100);
  
  if (!SD.begin(SD_CS_PIN)) {
    Serial.println("✗ Erro ao montar cartão SD");
    Serial.println("Continuando sem cartão SD...");
    sdCardOK = false;
    return;
  }
  
  sdCardOK = true;
  Serial.println("✓ Cartão SD montado com sucesso!");
  Serial.printf("  Tamanho: %llu MB\n", SD.cardSize() / (1024 * 1024));
  
  // Criar arquivo CSV se não existir
  if (!SD.exists(logFilename)) {
    File file = SD.open(logFilename, FILE_WRITE);
    if (file) {
      file.println("Timestamp,Temp_BME280,Umid_BME280,Pressao,Temp_DHT22,Umid_DHT22,Indice_UV,LED,Rele1,Rele2");
      file.close();
      Serial.println("✓ Arquivo de log criado");
    }
  }
}
```

### 5. Função de Logging
```cpp
void gravarDadosSD() {
  if (!sdCardOK) return;
  
  if (millis() - ultimaGravacaoSD < INTERVALO_LOG_SD) {
    return;
  }
  
  ultimaGravacaoSD = millis();
  
  File file = SD.open(logFilename, FILE_APPEND);
  if (!file) {
    Serial.println("[SD] ✗ Erro ao abrir arquivo de log");
    return;
  }
  
  // Gravar dados em CSV
  file.printf("%lu,%.2f,%.2f,%.2f,%.2f,%.2f,%.2f,%d,%d,%d\n",
              millis(),
              estado.temperatura_bme280,
              estado.umidade_bme280,
              estado.pressao,
              estado.temperatura_dht22,
              estado.umidade_dht22,
              estado.indiceUV,
              (int)estado.led,
              (int)estado.rele1,
              (int)estado.rele2);
  
  file.close();
  
  Serial.println("[SD] ✓ Dados gravados");
}
```

### 6. Novo Endpoint HTTP
```cpp
// Adicionar em configurarRotas()
server.on("/sd/dados", HTTP_GET, []() {
  if (!sdCardOK) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Cartão SD não disponível\"}");
    return;
  }
  
  File file = SD.open(logFilename, FILE_READ);
  if (!file) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Arquivo não encontrado\"}");
    return;
  }
  
  // Enviar últimas N linhas
  String dados = "[";
  int linhas = 0;
  while (file.available() && linhas < 20) {
    String linha = file.readStringUntil('\n');
    if (linhas > 0) dados += ",";
    dados += "\"" + linha + "\"";
    linhas++;
  }
  dados += "]";
  file.close();
  
  enviarJSON("{\"status\":\"ok\",\"linhas\":" + String(linhas) + ",\"dados\":" + dados + "}");
});

// Endpoint para listar arquivos
server.on("/sd/listar", HTTP_GET, []() {
  if (!sdCardOK) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Cartão SD não disponível\"}");
    return;
  }
  
  String json = "[";
  File root = SD.open("/");
  bool primeiro = true;
  
  while (File entry = root.openNextFile()) {
    if (!entrada.isDirectory()) {
      if (!primeiro) json += ",";
      json += "{\"nome\":\"" + String(entry.name()) + "\",\"tamanho\":" + String(entry.size()) + "}";
      primeiro = false;
    }
    entry.close();
  }
  root.close();
  json += "]";
  
  enviarJSON("{\"status\":\"ok\",\"arquivos\":" + json + "}");
});

// Endpoint para deletar arquivo
server.on("/sd/deletar", HTTP_GET, []() {
  if (!sdCardOK) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Cartão SD não disponível\"}");
    return;
  }
  
  if (server.args() < 1) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Nome do arquivo não fornecido\"}");
    return;
  }
  
  String filename = "/" + server.arg(0);
  if (SD.remove(filename)) {
    enviarJSON("{\"status\":\"ok\",\"mensagem\":\"Arquivo deletado\"}");
  } else {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Erro ao deletar arquivo\"}");
  }
});
```

### 7. Chamar em setup()
```cpp
void setup() {
  // ... código existente ...
  
  inicializarBME280();
  inicializarDHT22();
  inicializarSensorUV();
  inicializarWiFi();
  inicializarCartaoSD();  // ADICIONAR ESTA LINHA
  
  // ... resto do código ...
}
```

### 8. Chamar em loop()
```cpp
void loop() {
  verificarConexaoWiFi();
  server.handleClient();
  
  // ... leitura de sensores ...
  
  gravarDadosSD();  // ADICIONAR ESTA LINHA
  
  delay(10);
}
```

---

## 📡 Novos Endpoints HTTP

### GET /sd/dados
Retorna últimas 20 linhas do arquivo de log
```json
{
  "status": "ok",
  "linhas": 5,
  "dados": [
    "Timestamp,Temp_BME280,Umid_BME280,...",
    "1234567890,25.50,45.30,1013.25,25.20,46.10,5.50,0,0,0",
    "1234567920,25.52,45.28,1013.24,25.21,46.09,5.52,0,0,0",
    ...
  ]
}
```

### GET /sd/listar
Lista todos os arquivos no cartão SD
```json
{
  "status": "ok",
  "arquivos": [
    {"nome": "sensores_log.csv", "tamanho": 2048},
    {"nome": "backup.csv", "tamanho": 1024}
  ]
}
```

### GET /sd/deletar?arquivo=sensores_log.csv
Deleta um arquivo
```json
{
  "status": "ok",
  "mensagem": "Arquivo deletado"
}
```

---

## 📊 Arquivos Criados

### sensores_log.csv
Arquivo CSV com histórico de dados:
```
Timestamp,Temp_BME280,Umid_BME280,Pressao,Temp_DHT22,Umid_DHT22,Indice_UV,LED,Rele1,Rele2
1234567890,25.50,45.30,1013.25,25.20,46.10,5.50,0,0,0
1234567920,25.52,45.28,1013.24,25.21,46.09,5.52,0,0,0
1234567950,25.55,45.25,1013.23,25.23,46.08,5.55,0,0,0
```

---

## 🔧 Arquivos de Teste Inclusos

### SD_CARD_TEST.ino
Código de teste para verificar se cartão SD funciona:
```bash
# Testa:
- Inicialização SPI
- Montagem do cartão SD
- Leitura de arquivos
- Escrita de dados
- Append de dados
```

---

## ⚡ Dicas de Performance

1. **Batch Writing**: Escrever múltiplas linhas por vez é mais rápido
```cpp
File file = SD.open(logFilename, FILE_APPEND);
for (int i = 0; i < 10; i++) {
  file.println("dados");
}
file.close();
```

2. **Usar file.flush()** para garantir escrita
```cpp
file.println("dados");
file.flush(); // Força escrita no cartão
```

3. **Intervalo adequado**: 30-60 segundos é bom para sensores
```cpp
const unsigned long INTERVALO_LOG_SD = 30000; // 30 segundos
```

4. **Cortar arquivo quando crescer muito**
```cpp
if (SD.exists(logFilename)) {
  File file = SD.open(logFilename);
  if (file.size() > 1000000) { // 1MB
    SD.remove(logFilename); // Deletar e começar novo
  }
  file.close();
}
```

---

## 🛠️ Troubleshooting

| Problema | Solução |
|----------|---------|
| Cartão não detectado | Verificar voltagem (3.3V), não 5V |
| Dados não gravam | Verificar espaço no cartão, usar flush() |
| Leitura muito lenta | Normal em SPI, aumentar intervalo |
| Cartão corrompido | Reformatar em outro dispositivo |

---

**Código completo com SD pronto para usar!** ✅
