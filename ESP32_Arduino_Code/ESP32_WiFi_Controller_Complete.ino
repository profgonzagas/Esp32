/*
 * NecroSENSE - Código WiFi HTTP para ESP32
 * 
 * Este código cria um servidor HTTP no ESP32 para receber comandos
 * do aplicativo NecroSENSE (MAUI) via WiFi
 * 
 * Endpoints disponíveis:
 * - GET  /status          - Retorna status do dispositivo
 * - GET  /sensores        - Retorna leitura dos sensores
 * - GET  /led/on          - Liga o LED
 * - GET  /led/off         - Desliga o LED
 * - GET  /led/toggle      - Alterna o LED
 * - GET  /rele/1/on       - Liga relé 1
 * - GET  /rele/1/off      - Desliga relé 1
 * - GET  /rele/2/on       - Liga relé 2
 * - GET  /rele/2/off      - Desliga relé 2
 * - GET  /temperatura     - Leitura de temperatura
 * - GET  /umidade         - Leitura de umidade
 * - GET  /pwm/:pino/:valor - Define PWM em um pino
 * - GET  /gpio/:pino/:state - Define estado de um GPIO
 */

#include <WiFi.h>
#include <WebServer.h>
#include <ArduinoJson.h>
#include <DHT.h>
#include <time.h>

// ==================== CONFIGURAÇÕES DE REDE ====================
const char* ssid = "SEU_SSID";           // Altere para seu WiFi
const char* password = "SUA_SENHA";      // Altere para sua senha WiFi
const int PORT = 80;

// ==================== CONFIGURAÇÕES DE HARDWARE ====================
#define LED_PIN 2           // LED em GPIO 2
#define RELE1_PIN 26        // Relé 1 em GPIO 26
#define RELE2_PIN 27        // Relé 2 em GPIO 27
#define DHT_PIN 4           // Sensor DHT em GPIO 4
#define DHT_TYPE DHT22      // Sensor DHT22 (ou DHT11)

// ==================== OBJETOS GLOBAIS ====================
WebServer server(PORT);
DHT dht(DHT_PIN, DHT_TYPE);

// Estados dos periféricos
struct Estado {
  bool led = false;
  bool rele1 = false;
  bool rele2 = false;
  float temperatura = 0.0;
  float umidade = 0.0;
  unsigned long ultimaLeituraTemp = 0;
  const unsigned long INTERVALO_LEITURA = 2000; // 2 segundos
};

Estado estado;
unsigned long tempoConexao = 0;
const unsigned long TIMEOUT_CONEXAO = 20000; // Timeout de 20 segundos

// ==================== FUNCÇÕES AUXILIARES ====================

/**
 * Inicializa conexão WiFi
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
    enviarJSON("{\"status\":\"ok\",\"dispositivo\":\"NecroSENSE ESP32\",\"versao\":\"1.0\"}");
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
  
  // Leitura de temperatura
  server.on("/temperatura", HTTP_GET, []() {
    lerTemperatura();
    String response = "{\"status\":\"ok\",\"temperatura\":" + String(estado.temperatura, 2) + ",\"unidade\":\"°C\"}";
    enviarJSON(response);
  });
  
  // Leitura de umidade
  server.on("/umidade", HTTP_GET, []() {
    lerUmidade();
    String response = "{\"status\":\"ok\",\"umidade\":" + String(estado.umidade, 2) + ",\"unidade\":\"%\"}";
    enviarJSON(response);
  });
  
  // PWM
  server.on("/pwm", HTTP_GET, handlePWM);
  
  // GPIO genérico
  server.on("/gpio", HTTP_GET, handleGPIO);
  
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
  String json = "{";
  json += "\"status\":\"conectado\",";
  json += "\"dispositivo\":\"NecroSENSE ESP32\",";
  json += "\"versao\":\"1.0\",";
  json += "\"ip\":\"" + WiFi.localIP().toString() + "\",";
  json += "\"ssid\":\"" + String(WiFi.SSID()) + "\",";
  json += "\"rssi\":" + String(WiFi.RSSI()) + ",";
  json += "\"estado_led\":" + String(estado.led ? "true" : "false") + ",";
  json += "\"estado_rele1\":" + String(estado.rele1 ? "true" : "false") + ",";
  json += "\"estado_rele2\":" + String(estado.rele2 ? "true" : "false") + ",";
  json += "\"temperatura\":" + String(estado.temperatura, 2) + ",";
  json += "\"umidade\":" + String(estado.umidade, 2) + ",";
  json += "\"uptime\":" + String(millis() / 1000);
  json += "}";
  
  enviarJSON(json);
}

/**
 * Handler para leitura de sensores
 */
void handleSensores() {
  lerTemperatura();
  lerUmidade();
  
  String json = "[";
  json += "{\"nome\":\"Temperatura\",\"valor\":" + String(estado.temperatura, 2) + ",\"unidade\":\"°C\",\"icone\":\"🌡\"},";
  json += "{\"nome\":\"Umidade\",\"valor\":" + String(estado.umidade, 2) + ",\"unidade\":\"%\",\"icone\":\"💧\"},";
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
 * Leitura de temperatura
 */
void lerTemperatura() {
  if (millis() - estado.ultimaLeituraTemp >= estado.INTERVALO_LEITURA) {
    float temp = dht.readTemperature();
    if (!isnan(temp)) {
      estado.temperatura = temp;
      estado.ultimaLeituraTemp = millis();
      Serial.print("🌡 Temperatura: ");
      Serial.print(temp);
      Serial.println(" °C");
    } else {
      Serial.println("✗ Erro ao ler temperatura");
    }
  }
}

/**
 * Leitura de umidade
 */
void lerUmidade() {
  if (millis() - estado.ultimaLeituraTemp >= estado.INTERVALO_LEITURA) {
    float umid = dht.readHumidity();
    if (!isnan(umid)) {
      estado.umidade = umid;
      Serial.print("💧 Umidade: ");
      Serial.print(umid);
      Serial.println(" %");
    } else {
      Serial.println("✗ Erro ao ler umidade");
    }
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
  
  // Inicializar sensor DHT
  dht.begin();
  
  Serial.println("\n\n╔════════════════════════════════════╗");
  Serial.println("║   NecroSENSE - Controlador ESP32   ║");
  Serial.println("║          WiFi HTTP Server          ║");
  Serial.println("╚════════════════════════════════════╝");
  
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
  Serial.println("GET /status          - Status do dispositivo");
  Serial.println("GET /sensores        - Leitura dos sensores");
  Serial.println("GET /led/on          - Liga LED");
  Serial.println("GET /led/off         - Desliga LED");
  Serial.println("GET /led/toggle      - Alterna LED");
  Serial.println("GET /rele/1/on       - Liga Relé 1");
  Serial.println("GET /rele/1/off      - Desliga Relé 1");
  Serial.println("GET /rele/2/on       - Liga Relé 2");
  Serial.println("GET /rele/2/off      - Desliga Relé 2");
  Serial.println("GET /temperatura     - Lê temperatura");
  Serial.println("GET /umidade         - Lê umidade");
  Serial.println("GET /pwm             - Controle PWM");
  Serial.println("GET /gpio            - Controle GPIO genérico");
}

// ==================== LOOP ====================
void loop() {
  // Verificar conexão WiFi
  verificarConexaoWiFi();
  
  // Processar requisições HTTP
  server.handleClient();
  
  // Atualizar leituras periódicas
  if (millis() % 5000 == 0) { // A cada 5 segundos
    lerTemperatura();
    lerUmidade();
  }
  
  delay(10);
}
