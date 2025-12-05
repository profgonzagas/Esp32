/*
 * ESP32 Controller - Código Arduino
 * 
 * Este código cria um servidor web no ESP32 para receber comandos
 * do aplicativo ESP32Controller (MAUI)
 * 
 * Endpoints disponíveis:
 * - GET /status         - Retorna status do dispositivo
 * - GET /led/on         - Liga o LED
 * - GET /led/off        - Desliga o LED
 * - GET /led/toggle     - Alterna o LED
 * - GET /gpio/{pino}/high - Define pino HIGH
 * - GET /gpio/{pino}/low  - Define pino LOW
 * - GET /pwm/{pino}/{valor} - Define PWM (0-255)
 * - GET /rele/{num}/on  - Liga relé
 * - GET /rele/{num}/off - Desliga relé
 * - GET /rele/{num}/toggle - Alterna relé
 * - GET /temperatura    - Lê temperatura (se houver sensor)
 * - GET /sensores       - Retorna todos os sensores em JSON
 * - GET /restart        - Reinicia o ESP32
 */

#include <WiFi.h>
#include <WebServer.h>
#include <ArduinoJson.h>

// ==================== CONFIGURAÇÕES ====================
const char* ssid = "SEU_WIFI_SSID";        // Altere para sua rede WiFi
const char* password = "SUA_SENHA_WIFI";   // Altere para sua senha WiFi

// Pinos
#define LED_PIN 2       // LED built-in do ESP32
#define RELE1_PIN 26    // Pino do Relé 1
#define RELE2_PIN 27    // Pino do Relé 2

// Servidor Web na porta 80
WebServer server(80);

// Estados
bool ledState = false;
bool rele1State = false;
bool rele2State = false;

// ==================== FUNÇÕES DE SETUP ====================
void setup() {
    Serial.begin(115200);
    
    // Configurar pinos
    pinMode(LED_PIN, OUTPUT);
    pinMode(RELE1_PIN, OUTPUT);
    pinMode(RELE2_PIN, OUTPUT);
    
    digitalWrite(LED_PIN, LOW);
    digitalWrite(RELE1_PIN, LOW);
    digitalWrite(RELE2_PIN, LOW);
    
    // Conectar WiFi
    conectarWiFi();
    
    // Configurar rotas do servidor
    configurarRotas();
    
    // Iniciar servidor
    server.begin();
    Serial.println("Servidor HTTP iniciado!");
    Serial.print("IP: ");
    Serial.println(WiFi.localIP());
}

void loop() {
    server.handleClient();
}

// ==================== CONEXÃO WIFI ====================
void conectarWiFi() {
    Serial.print("Conectando a ");
    Serial.println(ssid);
    
    WiFi.begin(ssid, password);
    
    int tentativas = 0;
    while (WiFi.status() != WL_CONNECTED && tentativas < 30) {
        delay(500);
        Serial.print(".");
        tentativas++;
    }
    
    if (WiFi.status() == WL_CONNECTED) {
        Serial.println("\nWiFi conectado!");
        Serial.print("IP: ");
        Serial.println(WiFi.localIP());
    } else {
        Serial.println("\nFalha na conexão WiFi!");
    }
}

// ==================== CONFIGURAR ROTAS ====================
void configurarRotas() {
    // Rota principal - Status
    server.on("/", handleRoot);
    server.on("/status", handleStatus);
    
    // Rotas do LED
    server.on("/led/on", handleLedOn);
    server.on("/led/off", handleLedOff);
    server.on("/led/toggle", handleLedToggle);
    
    // Rotas dos Relés
    server.on("/rele/1/on", []() { handleRele(1, true); });
    server.on("/rele/1/off", []() { handleRele(1, false); });
    server.on("/rele/1/toggle", []() { handleReleToggle(1); });
    server.on("/rele/2/on", []() { handleRele(2, true); });
    server.on("/rele/2/off", []() { handleRele(2, false); });
    server.on("/rele/2/toggle", []() { handleReleToggle(2); });
    
    // Rotas de sensores
    server.on("/sensores", handleSensores);
    server.on("/temperatura", handleTemperatura);
    
    // Reiniciar
    server.on("/restart", handleRestart);
    
    // Rotas dinâmicas para GPIO e PWM
    server.onNotFound(handleNotFound);
    
    // Habilitar CORS
    server.enableCORS(true);
}

// ==================== HANDLERS ====================
void handleRoot() {
    String html = "<!DOCTYPE html><html><head><title>ESP32 Controller</title>";
    html += "<meta name='viewport' content='width=device-width, initial-scale=1'>";
    html += "<style>body{font-family:Arial;background:#1A1A2E;color:#fff;padding:20px;}";
    html += "h1{color:#4CAF50;}.btn{background:#4CAF50;color:#fff;padding:15px 30px;";
    html += "border:none;border-radius:8px;margin:5px;cursor:pointer;font-size:16px;}";
    html += ".btn:hover{background:#45a049;}.btn-off{background:#F44336;}";
    html += ".btn-off:hover{background:#d32f2f;}.status{padding:10px;background:#16213E;";
    html += "border-radius:8px;margin:10px 0;}</style></head><body>";
    html += "<h1>🔌 ESP32 Controller</h1>";
    html += "<div class='status'><strong>IP:</strong> " + WiFi.localIP().toString() + "</div>";
    html += "<div class='status'><strong>LED:</strong> " + String(ledState ? "Ligado" : "Desligado") + "</div>";
    html += "<div class='status'><strong>Relé 1:</strong> " + String(rele1State ? "Ligado" : "Desligado") + "</div>";
    html += "<div class='status'><strong>Relé 2:</strong> " + String(rele2State ? "Ligado" : "Desligado") + "</div>";
    html += "<h2>Controles</h2>";
    html += "<button class='btn' onclick=\"fetch('/led/toggle')\">💡 LED Toggle</button>";
    html += "<button class='btn' onclick=\"fetch('/rele/1/toggle')\">🔌 Relé 1</button>";
    html += "<button class='btn' onclick=\"fetch('/rele/2/toggle')\">🔌 Relé 2</button>";
    html += "<button class='btn btn-off' onclick=\"fetch('/restart')\">🔄 Reiniciar</button>";
    html += "<script>setInterval(()=>location.reload(),5000);</script>";
    html += "</body></html>";
    server.send(200, "text/html", html);
}

void handleStatus() {
    StaticJsonDocument<256> doc;
    doc["status"] = "online";
    doc["ip"] = WiFi.localIP().toString();
    doc["rssi"] = WiFi.RSSI();
    doc["led"] = ledState;
    doc["rele1"] = rele1State;
    doc["rele2"] = rele2State;
    doc["uptime"] = millis() / 1000;
    doc["freeHeap"] = ESP.getFreeHeap();
    
    String response;
    serializeJson(doc, response);
    server.send(200, "application/json", response);
}

void handleLedOn() {
    ledState = true;
    digitalWrite(LED_PIN, HIGH);
    server.send(200, "text/plain", "LED ligado");
}

void handleLedOff() {
    ledState = false;
    digitalWrite(LED_PIN, LOW);
    server.send(200, "text/plain", "LED desligado");
}

void handleLedToggle() {
    ledState = !ledState;
    digitalWrite(LED_PIN, ledState ? HIGH : LOW);
    server.send(200, "text/plain", ledState ? "LED ligado" : "LED desligado");
}

void handleRele(int numero, bool estado) {
    int pin = (numero == 1) ? RELE1_PIN : RELE2_PIN;
    
    if (numero == 1) {
        rele1State = estado;
    } else {
        rele2State = estado;
    }
    
    digitalWrite(pin, estado ? HIGH : LOW);
    server.send(200, "text/plain", "Rele " + String(numero) + (estado ? " ligado" : " desligado"));
}

void handleReleToggle(int numero) {
    bool* state = (numero == 1) ? &rele1State : &rele2State;
    *state = !(*state);
    handleRele(numero, *state);
}

void handleSensores() {
    StaticJsonDocument<512> doc;
    JsonArray sensores = doc.to<JsonArray>();
    
    // Exemplo de sensores (adicione seus sensores reais aqui)
    JsonObject temp = sensores.createNestedObject();
    temp["nome"] = "Temperatura";
    temp["valor"] = "25.5";
    temp["unidade"] = "°C";
    temp["icone"] = "🌡️";
    
    JsonObject umid = sensores.createNestedObject();
    umid["nome"] = "Umidade";
    umid["valor"] = "60";
    umid["unidade"] = "%";
    umid["icone"] = "💧";
    
    String response;
    serializeJson(doc, response);
    server.send(200, "application/json", response);
}

void handleTemperatura() {
    // Substitua por leitura real do sensor DHT11/DHT22/DS18B20
    float temperatura = 25.5;
    server.send(200, "text/plain", String(temperatura));
}

void handleRestart() {
    server.send(200, "text/plain", "Reiniciando...");
    delay(1000);
    ESP.restart();
}

void handleNotFound() {
    String uri = server.uri();
    
    // Tratar /gpio/{pino}/high ou /gpio/{pino}/low
    if (uri.startsWith("/gpio/")) {
        handleGPIO(uri);
        return;
    }
    
    // Tratar /pwm/{pino}/{valor}
    if (uri.startsWith("/pwm/")) {
        handlePWM(uri);
        return;
    }
    
    server.send(404, "text/plain", "Endpoint nao encontrado");
}

void handleGPIO(String uri) {
    // Formato: /gpio/{pino}/high ou /gpio/{pino}/low
    int firstSlash = uri.indexOf('/', 1);
    int secondSlash = uri.indexOf('/', firstSlash + 1);
    
    if (firstSlash == -1 || secondSlash == -1) {
        server.send(400, "text/plain", "Formato invalido. Use: /gpio/{pino}/high ou /gpio/{pino}/low");
        return;
    }
    
    int pino = uri.substring(firstSlash + 1, secondSlash).toInt();
    String estado = uri.substring(secondSlash + 1);
    
    pinMode(pino, OUTPUT);
    
    if (estado == "high") {
        digitalWrite(pino, HIGH);
        server.send(200, "text/plain", "GPIO " + String(pino) + " HIGH");
    } else if (estado == "low") {
        digitalWrite(pino, LOW);
        server.send(200, "text/plain", "GPIO " + String(pino) + " LOW");
    } else {
        server.send(400, "text/plain", "Estado invalido. Use: high ou low");
    }
}

void handlePWM(String uri) {
    // Formato: /pwm/{pino}/{valor}
    int firstSlash = uri.indexOf('/', 1);
    int secondSlash = uri.indexOf('/', firstSlash + 1);
    
    if (firstSlash == -1 || secondSlash == -1) {
        server.send(400, "text/plain", "Formato invalido. Use: /pwm/{pino}/{valor}");
        return;
    }
    
    int pino = uri.substring(firstSlash + 1, secondSlash).toInt();
    int valor = uri.substring(secondSlash + 1).toInt();
    
    // Limitar valor entre 0-255
    valor = constrain(valor, 0, 255);
    
    // Configurar PWM (ESP32)
    ledcAttach(pino, 5000, 8); // 5kHz, 8-bit
    ledcWrite(pino, valor);
    
    server.send(200, "text/plain", "PWM " + String(pino) + " = " + String(valor));
}
