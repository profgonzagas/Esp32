/*
 * ESP32 GUVA-S12SD - Teste Isolado do Sensor UV
 *
 * Testa APENAS o sensor UV sem WiFi, BLE ou SD Card.
 * Usa a mesma calibração ADC e tabela de conversão do código principal.
 *
 * Conexão:
 *   GUVA-S12SD  →  ESP32
 *   VCC (3.3V)  →  3.3V
 *   GND         →  GND
 *   OUT         →  GPIO 32
 *
 * Bibliotecas necessárias: nenhuma extra (driver/adc.h é do ESP32 SDK)
 */

#include <Arduino.h>
#include <driver/adc.h>
#include <esp_adc_cal.h>

#define UV_PIN     32             // GPIO 32 = ADC1_CHANNEL_4
#define ADC_CH     ADC1_CHANNEL_4 // Mesmo canal do código principal

// Tabela oficial GUVA-S12SD (igual ao código principal)
const int TABELA_MV[] = {  50, 227, 318, 408, 503, 606, 696, 795, 881,  976, 1079, 1170 };
const int TABELA_UV[] = {   0,   1,   2,   3,   4,   5,   6,   7,   8,    9,   10,   11 };
const int TAB_SIZE    = 12;

esp_adc_cal_characteristics_t adc_chars;
bool calibrado = false;

void configurarADC() {
  adc1_config_width(ADC_WIDTH_BIT_12);
  adc1_config_channel_atten(ADC_CH, ADC_ATTEN_DB_12);

  esp_adc_cal_value_t cal_type = esp_adc_cal_characterize(
    ADC_UNIT_1, ADC_ATTEN_DB_12, ADC_WIDTH_BIT_12, 1100, &adc_chars);

  calibrado = (cal_type == ESP_ADC_CAL_VAL_EFUSE_TP ||
               cal_type == ESP_ADC_CAL_VAL_EFUSE_VREF);

  Serial.printf("[ADC] Calibração: %s\n",
    cal_type == ESP_ADC_CAL_VAL_EFUSE_TP   ? "Two Point (eFuse)" :
    cal_type == ESP_ADC_CAL_VAL_EFUSE_VREF ? "Vref (eFuse)" : "Default Vref");
}

float mVparaIndiceUV(float tensao_mV) {
  if (tensao_mV < TABELA_MV[0])          return 0.0;
  if (tensao_mV >= TABELA_MV[TAB_SIZE-1]) return 11.0;
  for (int i = 0; i < TAB_SIZE - 1; i++) {
    if (tensao_mV >= TABELA_MV[i] && tensao_mV < TABELA_MV[i+1]) {
      float frac = (float)(tensao_mV - TABELA_MV[i]) / (TABELA_MV[i+1] - TABELA_MV[i]);
      return TABELA_UV[i] + frac * (TABELA_UV[i+1] - TABELA_UV[i]);
    }
  }
  return 0.0;
}

void lerUV() {
  configurarADC();

  // Descartar primeiras leituras (estabilizar ADC)
  for (int d = 0; d < 20; d++) {
    adc1_get_raw(ADC_CH);
    delay(2);
  }

  // 1000 amostras com 2ms entre cada (mesmo padrão do código principal)
  const int NUM_AMOSTRAS = 1000;
  long soma_mV  = 0;
  int  soma_raw = 0;
  int  minRaw = 4095, maxRaw = 0;

  for (int i = 0; i < NUM_AMOSTRAS; i++) {
    int raw = adc1_get_raw(ADC_CH);
    uint32_t mV = esp_adc_cal_raw_to_voltage(raw, &adc_chars);
    soma_mV  += mV;
    soma_raw += raw;
    if (raw < minRaw) minRaw = raw;
    if (raw > maxRaw) maxRaw = raw;
    delay(2);
  }

  float media_raw = (float)soma_raw / NUM_AMOSTRAS;
  float tensao_mV = (float)soma_mV  / NUM_AMOSTRAS;

  // Raw ≈ 0 → calibração eFuse pode adicionar offset falso (~142mV)
  if (media_raw < 5.0) tensao_mV = 0.0;

  // Detectar pino flutuante: spread alto + tensão baixa = sensor desconectado
  int spread = maxRaw - minRaw;
  bool flutuando = (spread > 200 && media_raw < 500);
  if (flutuando) {
    tensao_mV = 0.0;
    media_raw = 0.0;
  }

  float indiceUV = mVparaIndiceUV(tensao_mV);
  if (indiceUV > 15) indiceUV = 15;

  // Classificação do risco
  const char* risco;
  if      (indiceUV < 3)  risco = "Baixo";
  else if (indiceUV < 6)  risco = "Moderado";
  else if (indiceUV < 8)  risco = "Alto";
  else if (indiceUV < 11) risco = "Muito Alto";
  else                    risco = "Extremo";

  Serial.println("╔══════════════════════════════════════╗");
  Serial.println("║      GUVA-S12SD  -  LEITURA UV       ║");
  Serial.println("╠══════════════════════════════════════╣");
  if (flutuando) {
    Serial.println("║  ⚠  PINO FLUTUANTE / SENSOR SEM SINAL ║");
    Serial.println("║     Verifique a conexão do sensor      ║");
  } else {
    Serial.printf ("║  ADC raw:    %5.0f  (min:%d max:%d)  \n", media_raw, minRaw, maxRaw);
    Serial.printf ("║  Tensão:     %6.1f mV (%s)       \n", tensao_mV, calibrado ? "eFuse" : "default");
    Serial.printf ("║  Índice UV:  %5.2f                    \n", indiceUV);
    Serial.printf ("║  Risco:      %-10s                \n", risco);
    Serial.printf ("║  Spread ADC: %d (amostras: %d)     \n", spread, NUM_AMOSTRAS);
  }
  Serial.println("╚══════════════════════════════════════╝");
  Serial.println();
}

void setup() {
  Serial.begin(115200);
  delay(200);

  Serial.println("\n╔════════════════════════════════════════╗");
  Serial.println("║  Teste UV - GUVA-S12SD + ESP32         ║");
  Serial.println("║  GPIO 32  |  ADC1_CH4  |  3.3V         ║");
  Serial.println("╚════════════════════════════════════════╝\n");

  // Leitura diagnóstica rápida com analogRead (comparação)
  int diagRaw = analogRead(UV_PIN);
  Serial.printf("[DIAG] analogRead(32) = %d\n\n", diagRaw);

  Serial.println("Iniciando leituras a cada 5 segundos...\n");
}

void loop() {
  lerUV();
  delay(5000);
}
