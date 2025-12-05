/*
 * ESP32 Controller - Código BLE (Bluetooth Low Energy)
 * 
 * Este código cria um servidor BLE no ESP32 para receber comandos
 * do aplicativo ESP32Controller (MAUI) via Bluetooth
 * 
 * Comandos disponíveis (enviar via característica TX):
 * - LED_ON       - Liga o LED
 * - LED_OFF      - Desliga o LED
 * - LED_TOGGLE   - Alterna o LED
 * - GET_STATUS   - Retorna status do dispositivo
 * - GET_SENSORS  - Retorna leitura dos sensores
 * - RELE1_ON     - Liga relé 1
 * - RELE1_OFF    - Desliga relé 1
 * - RELE2_ON     - Liga relé 2
 * - RELE2_OFF    - Desliga relé 2
 */

#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>

// ==================== CONFIGURAÇÕES ====================
#define DEVICE_NAME "ESP32_BLE_001"

// UUIDs (mesmos do app MAUI)
#define SERVICE_UUID        "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define CHARACTERISTIC_TX   "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define CHARACTERISTIC_RX   "beb5483e-36e1-4688-b7f5-ea07361b26a9"

// Pinos
#define LED_PIN 2
#define RELE1_PIN 26
#define RELE2_PIN 27

// Variáveis BLE
BLEServer* pServer = NULL;
BLECharacteristic* pTxCharacteristic;
BLECharacteristic* pRxCharacteristic;
bool deviceConnected = false;
bool oldDeviceConnected = false;

// Estados
bool ledState = false;
bool rele1State = false;
bool rele2State = false;

// ==================== CALLBACKS BLE ====================
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
        deviceConnected = true;
        Serial.println("Cliente BLE conectado!");
    };

    void onDisconnect(BLEServer* pServer) {
        deviceConnected = false;
        Serial.println("Cliente BLE desconectado!");
    }
};

class MyCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
        String rxValue = pCharacteristic->getValue();
        
        if (rxValue.length() > 0) {
            Serial.print("Recebido: ");
            Serial.println(rxValue);
            
            processarComando(rxValue);
        }
    }
};

// ==================== SETUP ====================
void setup() {
    Serial.begin(115200);
    
    // Configurar pinos
    pinMode(LED_PIN, OUTPUT);
    pinMode(RELE1_PIN, OUTPUT);
    pinMode(RELE2_PIN, OUTPUT);
    
    digitalWrite(LED_PIN, LOW);
    digitalWrite(RELE1_PIN, LOW);
    digitalWrite(RELE2_PIN, LOW);
    
    // Inicializar BLE
    inicializarBLE();
    
    Serial.println("ESP32 BLE pronto!");
    Serial.print("Nome do dispositivo: ");
    Serial.println(DEVICE_NAME);
}

void loop() {
    // Reconectar se desconectado
    if (!deviceConnected && oldDeviceConnected) {
        delay(500);
        pServer->startAdvertising();
        Serial.println("Aguardando conexão BLE...");
        oldDeviceConnected = deviceConnected;
    }
    
    if (deviceConnected && !oldDeviceConnected) {
        oldDeviceConnected = deviceConnected;
    }
    
    delay(10);
}

// ==================== INICIALIZAÇÃO BLE ====================
void inicializarBLE() {
    // Criar dispositivo BLE
    BLEDevice::init(DEVICE_NAME);
    
    // Criar servidor BLE
    pServer = BLEDevice::createServer();
    pServer->setCallbacks(new MyServerCallbacks());
    
    // Criar serviço BLE
    BLEService *pService = pServer->createService(SERVICE_UUID);
    
    // Criar característica TX (ESP32 -> App)
    pTxCharacteristic = pService->createCharacteristic(
        CHARACTERISTIC_TX,
        BLECharacteristic::PROPERTY_READ |
        BLECharacteristic::PROPERTY_NOTIFY
    );
    pTxCharacteristic->addDescriptor(new BLE2902());
    
    // Criar característica RX (App -> ESP32)
    pRxCharacteristic = pService->createCharacteristic(
        CHARACTERISTIC_RX,
        BLECharacteristic::PROPERTY_WRITE |
        BLECharacteristic::PROPERTY_WRITE_NR
    );
    pRxCharacteristic->setCallbacks(new MyCallbacks());
    
    // Iniciar serviço
    pService->start();
    
    // Iniciar advertising
    BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
    pAdvertising->addServiceUUID(SERVICE_UUID);
    pAdvertising->setScanResponse(true);
    pAdvertising->setMinPreferred(0x06);
    pAdvertising->setMinPreferred(0x12);
    BLEDevice::startAdvertising();
    
    Serial.println("BLE iniciado, aguardando conexões...");
}

// ==================== PROCESSAR COMANDOS ====================
void processarComando(String comando) {
    comando.trim();
    comando.toUpperCase();
    
    String resposta = "";
    
    if (comando == "LED_ON") {
        ledState = true;
        digitalWrite(LED_PIN, HIGH);
        resposta = "LED ligado";
    }
    else if (comando == "LED_OFF") {
        ledState = false;
        digitalWrite(LED_PIN, LOW);
        resposta = "LED desligado";
    }
    else if (comando == "LED_TOGGLE") {
        ledState = !ledState;
        digitalWrite(LED_PIN, ledState ? HIGH : LOW);
        resposta = ledState ? "LED ligado" : "LED desligado";
    }
    else if (comando == "RELE1_ON") {
        rele1State = true;
        digitalWrite(RELE1_PIN, HIGH);
        resposta = "Rele 1 ligado";
    }
    else if (comando == "RELE1_OFF") {
        rele1State = false;
        digitalWrite(RELE1_PIN, LOW);
        resposta = "Rele 1 desligado";
    }
    else if (comando == "RELE2_ON") {
        rele2State = true;
        digitalWrite(RELE2_PIN, HIGH);
        resposta = "Rele 2 ligado";
    }
    else if (comando == "RELE2_OFF") {
        rele2State = false;
        digitalWrite(RELE2_PIN, LOW);
        resposta = "Rele 2 desligado";
    }
    else if (comando == "GET_STATUS") {
        resposta = "LED:" + String(ledState) + 
                   ",R1:" + String(rele1State) + 
                   ",R2:" + String(rele2State);
    }
    else if (comando == "GET_SENSORS") {
        // Substitua por leituras reais dos seus sensores
        resposta = "TEMP:25.5,UMID:60";
    }
    else {
        resposta = "Comando desconhecido: " + comando;
    }
    
    // Enviar resposta via BLE
    enviarResposta(resposta);
}

void enviarResposta(String mensagem) {
    if (deviceConnected) {
        pTxCharacteristic->setValue(mensagem.c_str());
        pTxCharacteristic->notify();
        Serial.print("Enviado: ");
        Serial.println(mensagem);
    }
}
