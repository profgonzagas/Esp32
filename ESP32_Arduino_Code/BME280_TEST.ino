/*
 * ESP32 BME280 - Teste Simples
 * 
 * Use este código para testar se o sensor BME280 está funcionando corretamente
 * antes de usar no código completo.
 * 
 * Bibliotecas necessárias:
 * - Adafruit BME280 Library
 * - Adafruit Unified Sensor
 */

#include <Wire.h>
#include <Adafruit_BME280.h>
#include <Adafruit_Sensor.h>

// Definir pinos I2C
#define SDA_PIN 21
#define SCL_PIN 22
#define BME280_ADDRESS 0x77

// Criar objeto do sensor
Adafruit_BME280 bme280;

void setup() {
  // Inicializar Serial
  Serial.begin(115200);
  delay(100);
  
  Serial.println("\n\n╔════════════════════════════════════╗");
  Serial.println("║   Teste BME280 - ESP32             ║");
  Serial.println("║   Sensor de Alta Precisão 3.3V     ║");
  Serial.println("╚════════════════════════════════════╝");
  debug_info();
  
  // Inicializar I2C
  Serial.println("\n[1/3] Inicializando I2C...");
  Wire.begin(SDA_PIN, SCL_PIN);
  delay(100);
  Serial.println("✓ I2C inicializado");
  Serial.printf("      SDA: GPIO %d\n", SDA_PIN);
  Serial.printf("      SCL: GPIO %d\n", SCL_PIN);
  
  // Tentar conectar ao sensor
  Serial.println("\n[2/3] Conectando ao BME280...");
  if (!bme280.begin(BME280_ADDRESS)) {
    Serial.println("✗ ERRO: Não foi possível encontrar o BME280!");
    Serial.println("\nVerifique:");
    Serial.println("  1. Conexão dos fios:");
    Serial.println("     - VCC → 3.3V");
    Serial.println("     - GND → GND");
    Serial.println("     - SDA → GPIO 21");
    Serial.println("     - SCL → GPIO 22");
    Serial.println("  2. Se a biblioteca Adafruit BME280 está instalada");
    Serial.println("  3. Se o endereço I2C é 0x77 (tente 0x76 se não funcionar)");
    Serial.println("\nAbortando...");
    while (1) delay(100);
  }
  
  Serial.println("✓ BME280 encontrado!");
  Serial.printf("  Endereço: 0x%02X\n", BME280_ADDRESS);
  
  // Configurar sensor
  Serial.println("\n[3/3] Configurando sensor...");
  bme280.setSampling(Adafruit_BME280::MODE_NORMAL,
                     Adafruit_BME280::SAMPLING_X2,  // temp
                     Adafruit_BME280::SAMPLING_X16, // pressure
                     Adafruit_BME280::SAMPLING_X2,  // humidity
                     Adafruit_BME280::FILTER_X16,
                     Adafruit_BME280::STANDBY_MS_0_5);
  
  Serial.println("✓ Sensor configurado em modo NORMAL");
  Serial.println("\n=== LEITURAS COMEÇANDO ===\n");
  delay(1000);
}

void loop() {
  // Ler valores do sensor
  float temperatura = bme280.readTemperature();
  float pressao = bme280.readPressure() / 100.0F; // Converter Pa para hPa
  float umidade = bme280.readHumidity();
  float altitude = bme280.readAltitude(1013.25); // Em relação ao nível do mar
  
  // Exibir valores
  Serial.println("╔════════ BME280 LEITURA ════════╗");
  Serial.printf("║ 🌡 Temperatura: %6.2f °C     ║\n", temperatura);
  Serial.printf("║ 💧 Umidade:     %6.2f %%       ║\n", umidade);
  Serial.printf("║ 🔽 Pressão:    %7.2f hPa    ║\n", pressao);
  Serial.printf("║ 📈 Altitude:   %7.2f m      ║\n", altitude);
  Serial.println("╚════════════════════════════════╝");
  
  // Análise dos valores
  Serial.println("📊 Análise:");
  
  // Temperatura
  if (temperatura < 0) Serial.print("   ⛄ Muito frio");
  else if (temperatura < 15) Serial.print("   ❄️  Frio");
  else if (temperatura < 25) Serial.print("   ✓ Confortável");
  else if (temperatura < 35) Serial.print("   ☀️  Quente");
  else Serial.print("   🔥 Muito quente");
  Serial.printf(" (%.1f°C)\n", temperatura);
  
  // Umidade
  if (umidade < 20) Serial.print("   🏜  Muito seco");
  else if (umidade < 40) Serial.print("   ⬇️  Seco");
  else if (umidade < 60) Serial.print("   ✓ Normal");
  else if (umidade < 80) Serial.print("   ⬆️  Úmido");
  else Serial.print("   🌧  Muito úmido");
  Serial.printf(" (%.1f%%)\n", umidade);
  
  // Pressão
  if (pressao < 980) Serial.print("   🌩  Baixa (tempestade)");
  else if (pressao < 1000) Serial.print("   ⛅ Baixa");
  else if (pressao < 1020) Serial.print("   ✓ Normal");
  else Serial.print("   ☀️  Alta");
  Serial.printf(" (%.2f hPa)\n", pressao);
  
  Serial.println();
  
  // Teste de JSON (simular resposta HTTP)
  Serial.println("📡 Resposta JSON simulada:");
  Serial.print("{\"temperatura\":");
  Serial.print(temperatura, 2);
  Serial.print(",\"umidade\":");
  Serial.print(umidade, 2);
  Serial.print(",\"pressao\":");
  Serial.print(pressao, 2);
  Serial.println(",\"unidade\":\"C,%, hPa\"}");
  
  Serial.println("\n---\n");
  
  // Aguardar 2 segundos antes da próxima leitura
  delay(2000);
}

void debug_info() {
  Serial.println("\n📋 Informações de Debug:");
  Serial.printf("   Velocidade Serial: 115200 baud\n");
  Serial.printf("   SDA (I2C): GPIO %d\n", SDA_PIN);
  Serial.printf("   SCL (I2C): GPIO %d\n", SCL_PIN);
  Serial.printf("   Endereço Sensor: 0x%02X\n", BME280_ADDRESS);
  Serial.println("   Se não conectar, tente endereço 0x76 (edite BME280_ADDRESS)");
}

/*
 * TROUBLESHOOTING:
 * 
 * 1. Sensor não encontrado (0x77):
 *    - Trocar para BME280_ADDRESS 0x76
 *    
 * 2. Valores estranhos:
 *    - Aguardar ~30 segundos para calibração
 *    - Verificar voltagem (deve ser 3.3V)
 *    
 * 3. Erro de compilação "Adafruit_BME280.h not found":
 *    - Instalar: Arduino IDE → Sketch → Include Library → Manage Libraries
 *    - Procurar: "Adafruit BME280"
 *    - Instalar: "Adafruit BME280 Library"
 *    
 * 4. Sensores conectados errados:
 *    - VCC ← Vermelho (3.3V)
 *    - GND ← Preto
 *    - SDA ← Azul (GPIO 21)
 *    - SCL ← Amarelo (GPIO 22)
 */
