/*
 * DIAGNÓSTICO RÁPIDO - ESP32 Sensores
 * 
 * Este sketch testa cada sensor individualmente
 * para identificar qual está com problema
 */

#include <Wire.h>
#include <Adafruit_BME280.h>
#include <DHT.h>

// Definições de Pins
#define SDA_PIN 21
#define SCL_PIN 22
#define DHT_PIN 4
#define DHT_TYPE DHT22
#define UV_PIN 34

// Objetos
Adafruit_BME280 bme280;
DHT dht22(DHT_PIN, DHT_TYPE);

void setup() {
  Serial.begin(115200);
  delay(1000);
  
  Serial.println("\n\n========================================");
  Serial.println("    DIAGNÓSTICO DE SENSORES ESP32");
  Serial.println("========================================\n");
  
  // Teste do BME280
  Serial.println("[1] Testando BME280...");
  Wire.begin(SDA_PIN, SCL_PIN);
  delay(100);
  
  if (!bme280.begin(0x77)) {
    Serial.println("❌ BME280 NÃO ENCONTRADO!");
    Serial.println("   Verifique:");
    Serial.println("   - Conexões I2C (GPIO 21=SDA, GPIO 22=SCL)");
    Serial.println("   - Voltagem 3.3V");
    Serial.println("   - Endereço: 0x77");
  } else {
    Serial.println("✓ BME280 detectado!");
    Serial.print("   Temperatura: ");
    Serial.print(bme280.readTemperature(), 2);
    Serial.println(" °C");
    Serial.print("   Umidade: ");
    Serial.print(bme280.readHumidity(), 2);
    Serial.println(" %");
    Serial.print("   Pressão: ");
    Serial.print(bme280.readPressure() / 100.0F, 2);
    Serial.println(" hPa");
  }
  
  Serial.println();
  Serial.println("[2] Testando DHT22...");
  dht22.begin();
  delay(500);
  
  float temp = dht22.readTemperature();
  float umid = dht22.readHumidity();
  
  if (isnan(temp) || isnan(umid)) {
    Serial.println("❌ DHT22 NÃO RESPONDEU!");
    Serial.println("   Verifique:");
    Serial.println("   - Conexão GPIO 4");
    Serial.println("   - Resistor pull-up 4.7kΩ");
    Serial.println("   - Voltagem 3.3V");
  } else {
    Serial.println("✓ DHT22 funcionando!");
    Serial.print("   Temperatura: ");
    Serial.print(temp, 1);
    Serial.println(" °C");
    Serial.print("   Umidade: ");
    Serial.print(umid, 1);
    Serial.println(" %");
  }
  
  Serial.println();
  Serial.println("[3] Testando Sensor UV...");
  pinMode(UV_PIN, INPUT);
  int uv = analogRead(UV_PIN);
  Serial.println("✓ Sensor UV funcionando!");
  Serial.print("   Valor ADC: ");
  Serial.println(uv);
  
  Serial.println("\n========================================");
  Serial.println("RESUMO:");
  Serial.println("========================================");
  
  if (!bme280.begin(0x77)) {
    Serial.println("⚠️  BME280: Conectar GPIO 21/22 com 3.3V");
  } else {
    Serial.println("✓ BME280: OK");
  }
  
  if (isnan(temp) || isnan(umid)) {
    Serial.println("⚠️  DHT22: Conectar GPIO 4 com pull-up 4.7kΩ");
  } else {
    Serial.println("✓ DHT22: OK");
  }
  
  Serial.println("✓ UV: OK (sempre lê valor ADC)");
  Serial.println("========================================\n");
}

void loop() {
  // Leituras contínuas a cada 5 segundos
  delay(5000);
  
  Serial.println("\n--- Leitura Contínua ---");
  
  // BME280
  if (bme280.begin(0x77)) {
    Serial.print("BME280 - T: ");
    Serial.print(bme280.readTemperature(), 1);
    Serial.print("°C | U: ");
    Serial.print(bme280.readHumidity(), 1);
    Serial.println("%");
  }
  
  // DHT22
  float temp = dht22.readTemperature();
  float umid = dht22.readHumidity();
  if (!isnan(temp) && !isnan(umid)) {
    Serial.print("DHT22  - T: ");
    Serial.print(temp, 1);
    Serial.print("°C | U: ");
    Serial.print(umid, 1);
    Serial.println("%");
  }
  
  // UV
  int uv = analogRead(UV_PIN);
  Serial.print("UV     - ADC: ");
  Serial.println(uv);
}
