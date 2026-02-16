/*
 * NecroSENSE - Código WiFi HTTP Completo para ESP32
 * 
 * Sensores Integrados:
 * - BME280: Temperatura, Umidade, Pressão (I2C)
 * - DHT22: Temperatura, Umidade (Digital)
 * - GUVA-S12SD: Radiação UV (Analógico)
 * - Cartão SD: Logging de dados (SPI)
 * 
 * Este código cria um servidor HTTP no ESP32 com todos os sensores
 * integrados e salvamento de dados em cartão SD
 */

#include <WiFi.h>
#include <WebServer.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <Adafruit_BME280.h>
#include <DHT.h>
#include <SD.h>
#include <SPI.h>
#include <time.h>

// ==================== CONFIGURAÇÕES DE REDE ====================
const char* ssid = "SEU_SSID";           // Altere para seu WiFi
const char* password = "SUA_SENHA";      // Altere para sua senha WiFi
const int PORT = 80;

// ==================== CONFIGURAÇÕES DE HARDWARE ====================
#define LED_PIN 2           // LED em GPIO 2
#define RELE1_PIN 26        // Relé 1 em GPIO 26
#define RELE2_PIN 27        // Relé 2 em GPIO 27
#define SDA_PIN 21          // I2C SDA para BME280
#define SCL_PIN 22          // I2C SCL para BME280
#define BME280_ADDRESS 0x77 // Endereço I2C do BME280 (padrão é 0x77)
#define DHT22_PIN 4         // DHT22 em GPIO 4
#define DHTTYPE DHT22       // Usar DHT22
#define UV_PIN 34           // GUVA-S12SD em GPIO 34 (Analógico)
// Pinos do SD Card (SPI)
#define SD_CS_PIN 5         // Chip Select em GPIO 5
#define SD_CLK_PIN 18       // Clock em GPIO 18 (SCK)
#define SD_MOSI_PIN 23      // MOSI/DI/DIN em GPIO 23
#define SD_MISO_PIN 19      // MISO/DO/DOUT em GPIO 19

// ==================== OBJETOS GLOBAIS ====================
WebServer server(PORT);
Adafruit_BME280 bme280;
DHT dht22(DHT22_PIN, DHTTYPE);

// Estados dos periféricos
struct Estado {
  bool led = false;
  bool rele1 = false;
  bool rele2 = false;
  // BME280
  float temperatura_bme280 = 0.0;
  float umidade_bme280 = 0.0;
  float pressao = 0.0;
  // DHT22
  float temperatura_dht22 = 0.0;
  float umidade_dht22 = 0.0;
  // UV - GUVA-S12SD
  int nivelUV = 0;           // 0-1023 (valor analógico)
  float indiceUV = 0.0;      // Índice UV calculado (0-15)
  unsigned long ultimaLeituraUV = 0;
  // SD Card
  bool cartaoSDconectado = false;
  unsigned long ultimaGravagemSD = 0;
  const unsigned long INTERVALO_GRAVACAO_SD = 30000; // Grava a cada 30 segundos
  
  unsigned long ultimaLeituraTemp = 0;
  unsigned long ultimaLeituraDHT22 = 0;
  const unsigned long INTERVALO_LEITURA = 30000; // 30 segundos
  const unsigned long INTERVALO_DHT22 = 30000;   // DHT22 requer ~30 segundos entre leituras
  const unsigned long INTERVALO_UV = 30000;       // UV a cada 30 segundos
};

Estado estado;
unsigned long tempoConexao = 0;
const unsigned long TIMEOUT_CONEXAO = 20000; // Timeout de 20 segundos

// ==================== FUNCÇÕES AUXILIARES ====================

/**
 * Inicializa o sensor UV GUVA-S12SD
 */
void inicializarSensorUV() {
  Serial.println("\n=== INICIALIZANDO SENSOR UV ===");
  pinMode(UV_PIN, INPUT);
  delay(100);
  Serial.println("✓ Sensor UV inicializado com sucesso!");
  Serial.println("  Pino: GPIO 34 (Analógico)");
  Serial.println("  Faixa: 0-1023 (ADC 12-bit)");
}

/**
 * Inicializa o sensor DHT22
 */
void inicializarDHT22() {
  Serial.println("\n=== INICIALIZANDO DHT22 ===");
  dht22.begin();
  delay(500);
  
  // Fazer leitura de teste
  float testTemp = dht22.readTemperature();
  float testUmid = dht22.readHumidity();
  
  if (!isnan(testTemp) && !isnan(testUmid)) {
    estado.temperatura_dht22 = testTemp;
    estado.umidade_dht22 = testUmid;
    Serial.println("✓ DHT22 inicializado com sucesso!");
    Serial.print("  Pino: GPIO 4");
    Serial.print(" | Temp: ");
    Serial.print(testTemp, 1);
    Serial.print("°C | Umid: ");
    Serial.print(testUmid, 1);
    Serial.println("%");
  } else {
    Serial.println("⚠ DHT22 inicializado mas primeira leitura falhou");
    Serial.println("  Verifique:");
    Serial.println("  - Conexão no GPIO 4");
    Serial.println("  - Resistor pull-up 4.7kΩ entre GPIO 4 e 3.3V");
    Serial.println("  - Voltagem 3.3V");
    // Inicializa com valores padrão
    estado.temperatura_dht22 = 0.0;
    estado.umidade_dht22 = 0.0;
  }
}

/**
 * Inicializa o sensor BME280
 */
void inicializarBME280() {
  Serial.println("\n=== INICIALIZANDO BME280 ===");
  
  // Inicializar I2C
  Wire.begin(SDA_PIN, SCL_PIN);
  delay(100);
  
  // Tentar conectar ao BME280
  if (!bme280.begin(BME280_ADDRESS)) {
    Serial.println("✗ Não foi possível encontrar o BME280!");
    Serial.println("Verifique a conexão do sensor:");
    Serial.println("  SDA -> GPIO 21");
    Serial.println("  SCL -> GPIO 22");
    Serial.println("  VCC -> 3.3V");
    Serial.println("  GND -> GND");
    while (1) delay(10);
  }
  
  Serial.println("✓ BME280 inicializado com sucesso!");
  
  // Configurar o sensor para modo normal
  bme280.setSampling(Adafruit_BME280::MODE_NORMAL,
                     Adafruit_BME280::SAMPLING_X2,  // temperatura
                     Adafruit_BME280::SAMPLING_X16, // pressão
                     Adafruit_BME280::SAMPLING_X2,  // umidade
                     Adafruit_BME280::FILTER_X16,
                     Adafruit_BME280::STANDBY_MS_0_5);
  
  // Fazer leitura de teste
  delay(100);
  float testTemp = bme280.readTemperature();
  float testUmid = bme280.readHumidity();
  float testPress = bme280.readPressure() / 100.0F;
  
  Serial.print("  Teste - T: ");
  Serial.print(testTemp, 1);
  Serial.print("°C | U: ");
  Serial.print(testUmid, 1);
  Serial.print("% | P: ");
  Serial.print(testPress, 1);
  Serial.println("hPa");
  
  Serial.println("✓ Sensor configurado em modo normal");
}

/**
 * Inicializa o cartão SD
 */
void inicializarCartaoSD() {
  Serial.println("\n=== INICIALIZANDO CARTÃO SD ===");
  
  // Configurar pinos SPI
  SPI.begin(SD_CLK_PIN, SD_MISO_PIN, SD_MOSI_PIN, SD_CS_PIN);
  
  // Inicializar SD
  if (!SD.begin(SD_CS_PIN)) {
    Serial.println("✗ Falha ao inicializar cartão SD!");
    Serial.println("Verifique as conexões:");
    Serial.println("  CS   -> GPIO 5");
    Serial.println("  CLK  -> GPIO 18 (SCK)");
    Serial.println("  MOSI -> GPIO 23 (DI/DIN)");
    Serial.println("  MISO -> GPIO 19 (DO/DOUT)");
    Serial.println("  VCC  -> 3.3V");
    Serial.println("  GND  -> GND");
    estado.cartaoSDconectado = false;
    return;
  }
  
  estado.cartaoSDconectado = true;
  Serial.println("✓ Cartão SD inicializado com sucesso!");
  
  uint64_t cardSize = SD.cardSize() / (1024 * 1024);
  Serial.print("Tamanho do cartão: ");
  Serial.print(cardSize);
  Serial.println(" MB");
  
  // Criar arquivo de cabeçalho
  criarArquivoCSV();
}

/**
 * Cria arquivo CSV com cabeçalho
 */
void criarArquivoCSV() {
  if (!estado.cartaoSDconectado) return;
  
  String nomeArquivo = "/dados_" + String(millis() / 1000) + ".csv";
  
  if (!SD.exists(nomeArquivo)) {
    File arquivo = SD.open(nomeArquivo, FILE_WRITE);
    if (arquivo) {
      arquivo.println("Timestamp,Temp_BME280,Umid_BME280,Press,Temp_DHT22,Umid_DHT22,UV_Index");
      arquivo.close();
      Serial.print("✓ Arquivo criado: ");
      Serial.println(nomeArquivo);
    }
  }
}

/**
 * Grava dados dos sensores no SD Card
 */
void gravarDadosSD() {
  if (!estado.cartaoSDconectado) return;
  
  unsigned long agora = millis();
  if (agora - estado.ultimaGravagemSD < estado.INTERVALO_GRAVACAO_SD) {
    return;
  }
  
  estado.ultimaGravagemSD = agora;
  
  // Procurar arquivo mais recente
  File raiz = SD.open("/");
  File arquivo;
  String nomeArquivo = "";
  
  arquivo = raiz.openNextFile();
  while (arquivo) {
    String nome = arquivo.name();
    if (nome.endsWith(".csv")) {
      nomeArquivo = "/" + nome;
    }
    arquivo.close();
    arquivo = raiz.openNextFile();
  }
  raiz.close();
  
  if (nomeArquivo == "") {
    criarArquivoCSV();
    return;
  }
  
  // Abrir arquivo para adicionar dados
  arquivo = SD.open(nomeArquivo, FILE_APPEND);
  if (arquivo) {
    // Construir linha CSV
    String linha = "";
    linha += agora;
    linha += ",";
    linha += String(estado.temperatura_bme280, 2);
    linha += ",";
    linha += String(estado.umidade_bme280, 2);
    linha += ",";
    linha += String(estado.pressao, 2);
    linha += ",";
    linha += String(estado.temperatura_dht22, 2);
    linha += ",";
    linha += String(estado.umidade_dht22, 2);
    linha += ",";
    linha += String(estado.indiceUV, 1);
    
    arquivo.println(linha);
    arquivo.close();
    
    Serial.println("[SD_CARD] Dados gravados com sucesso");
  } else {
    Serial.println("[SD_CARD] Erro ao abrir arquivo");
    estado.cartaoSDconectado = false;
  }
}

/**
 * Inicializa a conexão WiFi
 */
void inicializarWiFi() {
  Serial.println("\n\n=== INICIALIZANDO WiFi ===");
  Serial.print("Conectando a WiFi: ");
  Serial.println(ssid);
  
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  
  tempoConexao = millis();
  int tentativas = 0;
  const int MAX_TENTATIVAS = 40; // 20 segundos com delay de 500ms
  
  while (WiFi.status() != WL_CONNECTED && tentativas < MAX_TENTATIVAS) {
    delay(500);
    Serial.print(".");
    tentativas++;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\n✓ WiFi conectado!");
    Serial.print("SSID: ");
    Serial.println(WiFi.SSID());
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
    Serial.print("Força do sinal: ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");
  } else {
    Serial.println("\n✗ Falha na conexão WiFi!");
    Serial.println("Verifique SSID e senha");
  }
}

/**
 * Configurar rotas do servidor HTTP
 */
void configurarRotas() {
  Serial.println("\n=== CONFIGURANDO ROTAS HTTP ===");
  
  // Rota raiz
  server.on("/", HTTP_GET, []() {
    enviarJSON("{\"status\":\"ok\",\"dispositivo\":\"NecroSENSE ESP32\",\"versao\":\"2.2\"}");
  });
  
  // Status geral
  server.on("/status", HTTP_GET, handleStatus);
  
  // Leitura de sensores
  server.on("/sensores", HTTP_GET, handleSensores);
  
  // Controle de LED
  server.on("/led/on", HTTP_GET, []() {
    ligarLED();
    enviarJSON("{\"status\":\"ok\",\"comando\":\"LED_ON\",\"estado\":true}");
  });
  
  server.on("/led/off", HTTP_GET, []() {
    desligarLED();
    enviarJSON("{\"status\":\"ok\",\"comando\":\"LED_OFF\",\"estado\":false}");
  });
  
  server.on("/led/toggle", HTTP_GET, []() {
    estado.led = !estado.led;
    digitalWrite(LED_PIN, estado.led ? HIGH : LOW);
    enviarJSON("{\"status\":\"ok\",\"comando\":\"LED_TOGGLE\",\"estado\":" + String(estado.led ? "true" : "false") + "}");
  });
  
  // Controle de Relés
  server.on("/rele/1/on", HTTP_GET, []() {
    ligarRele(1);
    enviarJSON("{\"status\":\"ok\",\"comando\":\"RELE1_ON\",\"rele\":1,\"estado\":true}");
  });
  
  server.on("/rele/1/off", HTTP_GET, []() {
    desligarRele(1);
    enviarJSON("{\"status\":\"ok\",\"comando\":\"RELE1_OFF\",\"rele\":1,\"estado\":false}");
  });
  
  server.on("/rele/2/on", HTTP_GET, []() {
    ligarRele(2);
    enviarJSON("{\"status\":\"ok\",\"comando\":\"RELE2_ON\",\"rele\":2,\"estado\":true}");
  });
  
  server.on("/rele/2/off", HTTP_GET, []() {
    desligarRele(2);
    enviarJSON("{\"status\":\"ok\",\"comando\":\"RELE2_OFF\",\"rele\":2,\"estado\":false}");
  });
  
  // Leitura de temperatura BME280
  server.on("/temperatura", HTTP_GET, []() {
    lerSensoresBME280();
    String response = "{\"status\":\"ok\",\"sensor\":\"BME280\",\"temperatura\":" + String(estado.temperatura_bme280, 2) + ",\"unidade\":\"°C\"}";
    enviarJSON(response);
  });
  
  // Leitura de umidade BME280
  server.on("/umidade", HTTP_GET, []() {
    lerSensoresBME280();
    String response = "{\"status\":\"ok\",\"sensor\":\"BME280\",\"umidade\":\" + String(estado.umidade_bme280, 2) + ",\"unidade\":\"%\"}";
    enviarJSON(response);
  });
  
  // Leitura de pressão
  server.on("/pressao", HTTP_GET, []() {
    lerSensoresBME280();
    String response = "{\"status\":\"ok\",\"pressao\":\" + String(estado.pressao, 2) + ",\"unidade\":\"hPa\"}";
    enviarJSON(response);
  });
  
  // Leitura de temperatura DHT22
  server.on("/temperatura/dht22", HTTP_GET, []() {
    lerSensoresDHT22();
    String response = "{\"status\":\"ok\",\"sensor\":\"DHT22\",\"temperatura\":\" + String(estado.temperatura_dht22, 2) + ",\"unidade\":\"°C\"}";
    enviarJSON(response);
  });
  
  // Leitura de umidade DHT22
  server.on("/umidade/dht22", HTTP_GET, []() {
    lerSensoresDHT22();
    String response = "{\"status\":\"ok\",\"sensor\":\"DHT22\",\"umidade\":\" + String(estado.umidade_dht22, 2) + ",\"unidade\":\"%\"}";
    enviarJSON(response);
  });  
  // Leitura de UV
  server.on("/uv", HTTP_GET, []() {
    lerSensorUV();
    String response = "{\"status\":\"ok\",\"sensor\":\"GUVA-S12SD\",\"nivel_uv\":" + String(estado.nivelUV) + ",\"indice_uv\":" + String(estado.indiceUV, 2) + ",\"unidade\":\"UV\"}";
    enviarJSON(response);
  });  
  // PWM
  server.on("/pwm", HTTP_GET, handlePWM);
  
  // GPIO genérico
  server.on("/gpio", HTTP_GET, handleGPIO);
  
  // ===== ROTAS DO CARTÃO SD =====
  // Obter status do SD Card
  server.on("/sd/status", HTTP_GET, []() {
    String status = estado.cartaoSDconectado ? "conectado" : "desconectado";
    enviarJSON("{\"status\":\"ok\",\"cartao_sd\":\"" + status + "\"}");
  });
  
  // Listar arquivos do SD Card
  server.on("/sd/listar", HTTP_GET, []() {
    if (!estado.cartaoSDconectado) {
      enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Cartão SD não conectado\"}");
      return;
    }
    
    String json = "{\"status\":\"ok\",\"arquivos\":[";
    File raiz = SD.open("/");
    File arquivo = raiz.openNextFile();
    bool primeiro = true;
    
    while (arquivo) {
      if (!arquivo.isDirectory()) {
        if (!primeiro) json += ",";
        json += "\"" + String(arquivo.name()) + "\"";
        primeiro = false;
      }
      arquivo.close();
      arquivo = raiz.openNextFile();
    }
    raiz.close();
    json += "]}";
    enviarJSON(json);
  });
  
  // Deletar arquivo do SD Card
  server.on("/sd/deletar", HTTP_GET, []() {
    if (!estado.cartaoSDconectado) {
      enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Cartão SD não conectado\"}");
      return;
    }
    
    String nomeArquivo = server.arg("arquivo");
    if (nomeArquivo == "") {
      enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Nome do arquivo não fornecido\"}");
      return;
    }
    
    if (SD.remove("/" + nomeArquivo)) {
      enviarJSON("{\"status\":\"ok\",\"mensagem\":\"Arquivo deletado com sucesso\"}");
    } else {
      enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Falha ao deletar arquivo\"}");
      estado.cartaoSDconectado = false;
    }
  });
  
  // Tratamento de rotas não encontradas
  server.onNotFound([]() {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Rota não encontrada\"}");
  });
  
  Serial.println("✓ Rotas configuradas com sucesso");
}

/**
 * Envia resposta JSON
 */
void enviarJSON(String json) {
  server.sendHeader("Content-Type", "application/json; charset=utf-8");
  server.sendHeader("Access-Control-Allow-Origin", "*");
  server.send(200, "application/json", json);
  
  Serial.print("← Resposta: ");
  Serial.println(json);
}

/**
 * Handler para status completo
 */
void handleStatus() {
  lerSensoresBME280();
  lerSensoresDHT22();
  lerSensorUV();
  
  String json = "{";
  json += "\"status\":\"conectado\",";
  json += "\"dispositivo\":\"NecroSENSE ESP32\",";
  json += "\"versao\":\"2.2\",";
  json += "\"ip\":\"" + WiFi.localIP().toString() + "\",";
  json += "\"ssid\":\"" + String(WiFi.SSID()) + "\",";
  json += "\"rssi\":" + String(WiFi.RSSI()) + ",";
  json += "\"estado_led\":" + String(estado.led ? "true" : "false") + ",";
  json += "\"estado_rele1\":" + String(estado.rele1 ? "true" : "false") + ",";
  json += "\"estado_rele2\":" + String(estado.rele2 ? "true" : "false") + ",";
  json += "\"cartao_sd\":\"" + String(estado.cartaoSDconectado ? "conectado" : "desconectado") + "\",";
  json += "\"sensores\":{";
  json += "\"bme280\":{\"temperatura\":" + String(estado.temperatura_bme280, 2) + ",\"umidade\":" + String(estado.umidade_bme280, 2) + ",\"pressao\":" + String(estado.pressao, 2) + "},";
  json += "\"dht22\":{\"temperatura\":" + String(estado.temperatura_dht22, 2) + ",\"umidade\":" + String(estado.umidade_dht22, 2) + "},";
  json += "\"uv\":{\"nivel\":" + String(estado.nivelUV) + ",\"indice\":" + String(estado.indiceUV, 2) + "}";
  json += "},";
  json += "\"uptime\":" + String(millis() / 1000);
  json += "}";
  
  enviarJSON(json);
}

/**
 * Handler para leitura de sensores
 */
void handleSensores() {
  lerSensoresBME280();
  lerSensoresDHT22();
  lerSensorUV();
  
  String json = "[";
  json += "{\"nome\":\"Temperatura (BME280)\",\"valor\":" + String(estado.temperatura_bme280, 2) + ",\"unidade\":\"°C\",\"icone\":\"🌡\"},";
  json += "{\"nome\":\"Umidade (BME280)\",\"valor\":" + String(estado.umidade_bme280, 2) + ",\"unidade\":\"%\",\"icone\":\"💧\"},";
  json += "{\"nome\":\"Pressão (BME280)\",\"valor\":" + String(estado.pressao, 2) + ",\"unidade\":\"hPa\",\"icone\":\"🔽\"},";
  json += "{\"nome\":\"Temperatura (DHT22)\",\"valor\":" + String(estado.temperatura_dht22, 2) + ",\"unidade\":\"°C\",\"icone\":\"🌡\"},";
  json += "{\"nome\":\"Umidade (DHT22)\",\"valor\":" + String(estado.umidade_dht22, 2) + ",\"unidade\":\"%\",\"icone\":\"💧\"},";
  json += "{\"nome\":\"Índice UV\",\"valor\":" + String(estado.indiceUV, 2) + ",\"unidade\":\"UV\",\"icone\":\"☀️\"},";
  json += "{\"nome\":\"LED\",\"valor\":" + String(estado.led ? "1" : "0") + ",\"unidade\":\"bool\",\"icone\":\"💡\"},";
  json += "{\"nome\":\"Relé 1\",\"valor\":" + String(estado.rele1 ? "1" : "0") + ",\"unidade\":\"bool\",\"icone\":\"🔌\"},";
  json += "{\"nome\":\"Relé 2\",\"valor\":" + String(estado.rele2 ? "1" : "0") + ",\"unidade\":\"bool\",\"icone\":\"🔌\"}";
  json += "]";
  
  enviarJSON(json);
}

/**
 * Handler para PWM
 */
void handlePWM() {
  if (server.args() < 2) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Parâmetros insuficientes: pino e valor\"}");
    return;
  }
  
  int pino = server.arg(0).toInt();
  int valor = server.arg(1).toInt();
  
  if (valor < 0 || valor > 255) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Valor PWM deve estar entre 0 e 255\"}");
    return;
  }
  
  pinMode(pino, OUTPUT);
  analogWrite(pino, valor);
  
  String response = "{\"status\":\"ok\",\"comando\":\"PWM\",\"pino\":" + String(pino) + ",\"valor\":" + String(valor) + "}";
  enviarJSON(response);
}

/**
 * Handler para GPIO genérico
 */
void handleGPIO() {
  if (server.args() < 2) {
    enviarJSON("{\"status\":\"erro\",\"mensagem\":\"Parâmetros insuficientes: pino e estado\"}");
    return;
  }
  
  int pino = server.arg(0).toInt();
  String estado_str = server.arg(1);
  bool estado_gpio = (estado_str == "high" || estado_str == "1" || estado_str == "true");
  
  pinMode(pino, OUTPUT);
  digitalWrite(pino, estado_gpio ? HIGH : LOW);
  
  String response = "{\"status\":\"ok\",\"comando\":\"GPIO\",\"pino\":" + String(pino) + ",\"estado\":\"" + (estado_gpio ? "high" : "low") + "\"}";
  enviarJSON(response);
}

/**
 * Ligar LED
 */
void ligarLED() {
  estado.led = true;
  digitalWrite(LED_PIN, HIGH);
  Serial.println("💡 LED ligado");
}

/**
 * Desligar LED
 */
void desligarLED() {
  estado.led = false;
  digitalWrite(LED_PIN, LOW);
  Serial.println("💡 LED desligado");
}

/**
 * Ligar relé
 */
void ligarRele(int numero) {
  if (numero == 1) {
    estado.rele1 = true;
    digitalWrite(RELE1_PIN, HIGH);
    Serial.println("🔌 Relé 1 ligado");
  } else if (numero == 2) {
    estado.rele2 = true;
    digitalWrite(RELE2_PIN, HIGH);
    Serial.println("🔌 Relé 2 ligado");
  }
}

/**
 * Desligar relé
 */
void desligarRele(int numero) {
  if (numero == 1) {
    estado.rele1 = false;
    digitalWrite(RELE1_PIN, LOW);
    Serial.println("🔌 Relé 1 desligado");
  } else if (numero == 2) {
    estado.rele2 = false;
    digitalWrite(RELE2_PIN, LOW);
    Serial.println("🔌 Relé 2 desligado");
  }
}

/**
 * Leitura de temperatura, umidade e pressão do BME280
 */
void lerSensoresBME280() {
  if (millis() - estado.ultimaLeituraTemp >= estado.INTERVALO_LEITURA) {
    estado.temperatura_bme280 = bme280.readTemperature();
    estado.umidade_bme280 = bme280.readHumidity();
    estado.pressao = bme280.readPressure() / 100.0F; // Converter Pa para hPa
    
    estado.ultimaLeituraTemp = millis();
    
    Serial.print("[BME280] 🌡 Temperatura: ");
    Serial.print(estado.temperatura_bme280, 1);
    Serial.print(" °C | 💧 Umidade: ");
    Serial.print(estado.umidade_bme280, 1);
    Serial.print(" % | 🔽 Pressão: ");
    Serial.print(estado.pressao, 2);
    Serial.println(" hPa");
  }
}

/**
 * Leitura de temperatura e umidade do DHT22
 */
void lerSensoresDHT22() {
  if (millis() - estado.ultimaLeituraDHT22 >= estado.INTERVALO_DHT22) {
    float temp = dht22.readTemperature();
    float umid = dht22.readHumidity();
    
    // Verificar se as leituras são válidas
    if (!isnan(temp) && !isnan(umid)) {
      estado.temperatura_dht22 = temp;
      estado.umidade_dht22 = umid;
      
      Serial.print("[DHT22] ✓ Temperatura: ");
      Serial.print(estado.temperatura_dht22, 1);
      Serial.print(" °C | 💧 Umidade: ");
      Serial.print(estado.umidade_dht22, 1);
      Serial.println(" %");
    } else {
      // Se erro, tenta novamente em 2 segundos (não espera 30s)
      Serial.print("[DHT22] ⚠ Falha na leitura | Tentando novamente...");
      delay(2000);
      temp = dht22.readTemperature();
      umid = dht22.readHumidity();
      
      if (!isnan(temp) && !isnan(umid)) {
        estado.temperatura_dht22 = temp;
        estado.umidade_dht22 = umid;
        Serial.print(" ✓ OK! T: ");
        Serial.print(estado.temperatura_dht22, 1);
        Serial.print("°C U: ");
        Serial.print(estado.umidade_dht22, 1);
        Serial.println("%");
      } else {
        Serial.println(" ✗ Erro persistente - Verifique conexão GPIO 4");
      }
    }
    
    estado.ultimaLeituraDHT22 = millis();
  }
}

/**
 * Leitura de radiação UV do sensor GUVA-S12SD
 */
void lerSensorUV() {
  if (millis() - estado.ultimaLeituraUV >= estado.INTERVALO_UV) {
    // Ler valor analógico do sensor (0-1023)
    estado.nivelUV = analogRead(UV_PIN);
    
    // Converter para índice UV (aproximação empírica)
    // GUVA-S12SD: ~1V = 1 mW/cm² (irradiância UV)
    // Tensão = (nivelUV / 1023) * 3.3V
    float tensao = (estado.nivelUV / 1023.0) * 3.3;
    float irradiancia = (tensao / 0.1) * 0.001; // em W/cm²
    estado.indiceUV = irradiancia * 40; // Conversão para índice UV (0-15)
    
    // Limitar a índice UV a máximo de 15
    if (estado.indiceUV > 15) estado.indiceUV = 15;
    
    Serial.print("[UV] ☀️ ADC: ");
    Serial.print(estado.nivelUV);
    Serial.print(" | Tensão: ");
    Serial.print(tensao, 2);
    Serial.print("V | Índice UV: ");
    Serial.println(estado.indiceUV, 2);
    
    estado.ultimaLeituraUV = millis();
  }
}

/**
 * Verificar status da conexão WiFi
 */
void verificarConexaoWiFi() {
  static unsigned long ultimaVerificacao = 0;
  const unsigned long INTERVALO_VERIFICACAO = 5000; // 5 segundos
  
  if (millis() - ultimaVerificacao >= INTERVALO_VERIFICACAO) {
    ultimaVerificacao = millis();
    
    if (WiFi.status() != WL_CONNECTED) {
      Serial.println("\n✗ Conexão WiFi perdida! Reconectando...");
      WiFi.reconnect();
    }
  }
}

// ==================== SETUP ====================
void setup() {
  Serial.begin(115200);
  delay(100);
  
  // Configurar pinos
  pinMode(LED_PIN, OUTPUT);
  pinMode(RELE1_PIN, OUTPUT);
  pinMode(RELE2_PIN, OUTPUT);
  
  // Estado inicial LOW (desligado)
  digitalWrite(LED_PIN, LOW);
  digitalWrite(RELE1_PIN, LOW);
  digitalWrite(RELE2_PIN, LOW);
  
  Serial.println("\n\n╔════════════════════════════════════╗");
  Serial.println("║   NecroSENSE - Controlador ESP32   ║");
  Serial.println("║   WiFi HTTP + BME280 + DHT22 + UV    ║");
  Serial.println("╚════════════════════════════════════════╝");
  
  // Inicializar sensores
  inicializarBME280();
  inicializarDHT22();
  inicializarSensorUV();
  
  // Inicializar SD Card
  inicializarCartaoSD();
  
  // Conectar WiFi
  inicializarWiFi();
  
  // Configurar rotas
  configurarRotas();
  
  // Iniciar servidor
  server.begin();
  Serial.println("\n✓ Servidor HTTP iniciado na porta 80");
  Serial.print("Acesse em: http://");
  Serial.print(WiFi.localIP());
  Serial.println("/");
  
  Serial.println("\n=== ENDPOINTS DISPONÍVEIS ===");
  Serial.println("GET /status              - Status do dispositivo");
  Serial.println("GET /sensores            - Leitura dos sensores (BME280 + DHT22)");
  Serial.println("GET /led/on              - Liga LED");
  Serial.println("GET /led/off             - Desliga LED");
  Serial.println("GET /led/toggle          - Alterna LED");
  Serial.println("GET /rele/1/on           - Liga Relé 1");
  Serial.println("GET /rele/1/off          - Desliga Relé 1");
  Serial.println("GET /rele/2/on           - Liga Relé 2");
  Serial.println("GET /rele/2/off          - Desliga Relé 2");
  Serial.println("\n--- BME280 (Pressão, Umidade, Temperatura) ---");
  Serial.println("GET /temperatura         - Lê temperatura (BME280)");
  Serial.println("GET /umidade             - Lê umidade (BME280)");
  Serial.println("GET /pressao             - Lê pressão");
  Serial.println("\n--- DHT22 (Umidade, Temperatura) ---");
  Serial.println("GET /temperatura/dht22   - Lê temperatura (DHT22)");
  Serial.println("GET /umidade/dht22       - Lê umidade (DHT22)");
  Serial.println("\n--- UV (GUVA-S12SD) ---");
  Serial.println("GET /uv                  - Lê radiação UV (índice + valor)");
  Serial.println("\n--- SD Card ---");
  Serial.println("GET /sd/status           - Status do cartão SD");
  Serial.println("GET /sd/listar           - Lista arquivo do SD");
  Serial.println("GET /sd/deletar          - Deleta arquivo do SD");
  Serial.println("\n--- Controle ---");
  Serial.println("GET /pwm                 - Controle PWM");
  Serial.println("GET /gpio                - Controle GPIO genérico");
}

// ==================== LOOP ====================
void loop() {
  // Verificar conexão WiFi
  verificarConexaoWiFi();
  
  // Processar requisições HTTP
  server.handleClient();
  
  // Atualizar leituras periódicas
  static unsigned long ultimaLeitura = 0;
  static int tentativasDHT22 = 0;
  
  if (millis() - ultimaLeitura >= 1000) {
    ultimaLeitura = millis();
    lerSensoresBME280();
    lerSensorUV();
    
    // DHT22: tentar ler a cada 1 segundo até conseguir, depois a cada 30 segundos
    if (estado.temperatura_dht22 == 0.0 || estado.umidade_dht22 == 0.0) {
      // Ainda não conseguiu leitura válida, tenta mais frequentemente
      if (tentativasDHT22 < 30) {
        lerSensoresDHT22();
        tentativasDHT22++;
      }
    } else {
      // Já tem leitura válida, respeita intervalo de 30s
      if (millis() - estado.ultimaLeituraDHT22 >= estado.INTERVALO_DHT22) {
        lerSensoresDHT22();
      }
    }
  }
  
  // Gravar dados no SD Card a cada 30 segundos
  gravarDadosSD();
  
}
