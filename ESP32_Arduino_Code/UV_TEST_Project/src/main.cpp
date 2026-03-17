/*
 * ESP32 GUVA-S12SD - Teste Diagnostico UV
 *
 * Output 100% ASCII (sem UTF-8) para evitar lixo no Serial Monitor.
 *
 * Conexao:
 *   GUVA-S12SD  ->  ESP32
 *   VCC (3.3V)  ->  3.3V
 *   GND         ->  GND
 *   OUT         ->  GPIO 32
 */

#include <Arduino.h>
#include <driver/adc.h>
#include <esp_adc_cal.h>

#define UV_PIN  32
#define ADC_CH  ADC1_CHANNEL_4

// Tabela GUVA-S12SD oficial (mV -> UV index)
const int TABELA_MV[] = {  50, 227, 318, 408, 503, 606, 696, 795, 881,  976, 1079, 1170 };
const int TABELA_UV[] = {   0,   1,   2,   3,   4,   5,   6,   7,   8,    9,   10,   11 };
const int TAB_SIZE    = 12;

// Limiar: abaixo disso o raw e considerado ruido ADC (offset eFuse ~142mV com raw=0)
// Aumentado de 5 para 30 para evitar falsos positivos de offset ADC
#define RAW_ZERO_THRESHOLD 30

esp_adc_cal_characteristics_t adc_chars;
bool calibrado = false;
int leituraNum = 0;

void configurarADC() {
  adc1_config_width(ADC_WIDTH_BIT_12);
  adc1_config_channel_atten(ADC_CH, ADC_ATTEN_DB_12);
  esp_adc_cal_value_t cal_type = esp_adc_cal_characterize(
    ADC_UNIT_1, ADC_ATTEN_DB_12, ADC_WIDTH_BIT_12, 1100, &adc_chars);
  calibrado = (cal_type == ESP_ADC_CAL_VAL_EFUSE_TP ||
               cal_type == ESP_ADC_CAL_VAL_EFUSE_VREF);
}

float mVparaIndiceUV(float mV) {
  if (mV < TABELA_MV[0])            return 0.0;
  if (mV >= TABELA_MV[TAB_SIZE - 1]) return 11.0;
  for (int i = 0; i < TAB_SIZE - 1; i++) {
    if (mV >= TABELA_MV[i] && mV < TABELA_MV[i + 1]) {
      float frac = (float)(mV - TABELA_MV[i]) / (TABELA_MV[i + 1] - TABELA_MV[i]);
      return TABELA_UV[i] + frac * (TABELA_UV[i + 1] - TABELA_UV[i]);
    }
  }
  return 0.0;
}

void lerUV() {
  leituraNum++;
  configurarADC();

  // Descartar 20 leituras iniciais (estabilizar ADC)
  for (int d = 0; d < 20; d++) { adc1_get_raw(ADC_CH); delay(2); }

  const int N = 1000;
  long soma_mV = 0;
  int  soma_raw = 0, minRaw = 4095, maxRaw = 0;

  for (int i = 0; i < N; i++) {
    int raw = adc1_get_raw(ADC_CH);
    uint32_t mV = esp_adc_cal_raw_to_voltage(raw, &adc_chars);
    soma_mV  += mV;
    soma_raw += raw;
    if (raw < minRaw) minRaw = raw;
    if (raw > maxRaw) maxRaw = raw;
    delay(2);
  }

  float media_raw = (float)soma_raw / N;
  float tensao_mV = (float)soma_mV  / N;
  int   spread    = maxRaw - minRaw;

  // --- DIAGNOSTICO DE CAUSA ---

  // 1) Offset ADC: raw baixo mas eFuse adiciona ~142mV artificialmente
  bool offset_adc = (media_raw < RAW_ZERO_THRESHOLD);
  if (offset_adc) tensao_mV = 0.0;

  // Detectar pino flutuante:
  //   - spread > 3500: ADC oscila em quase toda a faixa (0-4095) = sinal caotico
  //   - spread > 200 + raw baixo: ruido com media baixa
  // Um sensor REAL nunca tem spread de 4095 (toda a faixa ADC)
  bool flutuando = (spread > 3500 || (!offset_adc && spread > 200 && media_raw < 500));
  if (flutuando) { tensao_mV = 0.0; media_raw = 0.0; }

  float indiceUV = mVparaIndiceUV(tensao_mV);
  if (indiceUV > 15) indiceUV = 15;

  // 3) Detectar possivel luz fluorescente/CFL (sinal real fraco)
  // CFL emite UV-A (315-400nm). GUVA-S12SD detecta 240-370nm.
  // Leitura tipica de CFL a 1m: raw 50-300, mV 100-400
  bool suspeita_lampada = (!offset_adc && !flutuando &&
                           media_raw > RAW_ZERO_THRESHOLD && media_raw < 600 &&
                           tensao_mV > 50 && tensao_mV < 500);

  // 4) Sinal solar real (alto)
  bool sinal_solar = (!offset_adc && !flutuando && tensao_mV >= 500);

  const char* risco =
    indiceUV < 3  ? "Baixo"     :
    indiceUV < 6  ? "Moderado"  :
    indiceUV < 8  ? "Alto"      :
    indiceUV < 11 ? "Muito Alto" : "Extremo";

  // --- OUTPUT ASCII PURO ---
  Serial.println("=======================================================");
  Serial.printf( "  LEITURA #%d\n", leituraNum);
  Serial.println("-------------------------------------------------------");
  Serial.printf( "  ADC raw    : %.0f  (min:%d  max:%d  spread:%d)\n",
                 media_raw, minRaw, maxRaw, spread);
  Serial.printf( "  Tensao     : %.1f mV  (cal:%s)\n",
                 tensao_mV, calibrado ? "eFuse" : "default");
  Serial.printf( "  Indice UV  : %.2f  [%s]\n", indiceUV, risco);

  // Linha de diagnostico
  Serial.println("  ---- DIAGNOSTICO ----");
  if (flutuando) {
    Serial.printf("  [!] PINO FLUTUANTE (spread=%d) - GPIO32 SEM SINAL ESTAVEL\n", spread);
    Serial.println("      spread>3500 = ADC oscilando em toda a faixa = sensor nao conectado");
    Serial.println("      Verifique: cabo OUT do GUVA-S12SD no GPIO 32?");
    Serial.println("      Verifique: VCC ligado em 3.3V (nao 5V)?");
    Serial.printf( "      analogRead(32) quick = %d\n", analogRead(UV_PIN));
  } else if (offset_adc) {
    Serial.println("  [!] OFFSET ADC - raw muito baixo, sinal zerado");
    Serial.printf( "      raw=%.0f < limiar=%d  -> tensao forcada para 0mV\n",
                   media_raw, RAW_ZERO_THRESHOLD);
    Serial.println("      CAUSA: ADC do ESP32 tem offset interno ~142mV");
    Serial.println("      Se o indice UV estava alto, era FALSO POSITIVO.");
  } else if (suspeita_lampada) {
    Serial.println("  [?] POSSIVEL LAMPADA FLUORESCENTE/CFL");
    Serial.printf( "      raw=%.0f  mV=%.1f -> UV index %.2f\n",
                   media_raw, tensao_mV, indiceUV);
    Serial.println("      CFL emite UV-A (315-400nm) que o GUVA-S12SD detecta.");
    Serial.println("      TESTE: cubra o sensor com a mao.");
    Serial.println("        - Se cair para 0 -> eh a lampada (sinal REAL, nao bug)");
    Serial.println("        - Se nao mudar   -> pode ser ruido ADC");
  } else if (sinal_solar) {
    Serial.println("  [!] SINAL ALTO - possivel luz solar direta ou UV forte");
    Serial.printf( "      mV=%.1f -> UV index %.2f\n", tensao_mV, indiceUV);
    Serial.println("      Verifique: sensor perto de janela com sol?");
  } else {
    Serial.printf( "      raw=%.0f  mV=%.1f  UV=%.2f (normal)\n",
                   media_raw, tensao_mV, indiceUV);
  }

  // Referencia da tabela para o limiar atual
  Serial.println("  ---- TABELA DE REFERENCIA ----");
  Serial.println("  mV threshold : UV index");
  for (int i = 0; i < TAB_SIZE; i++) {
    char marker = (tensao_mV >= TABELA_MV[i] &&
                   (i == TAB_SIZE-1 || tensao_mV < TABELA_MV[i+1])) ? '*' : ' ';
    Serial.printf("  %c %4d mV    -> UV %2d\n", marker, TABELA_MV[i], TABELA_UV[i]);
  }
  Serial.println("=======================================================\n");
}

void setup() {
  Serial.begin(115200);
  delay(300);

  Serial.println("\n=======================================================");
  Serial.println("  GUVA-S12SD UV DIAGNOSTIC TEST - ESP32");
  Serial.println("  GPIO 32 | ADC1_CH4 | 3.3V");
  Serial.println("=======================================================");
  Serial.printf("  RAW_ZERO_THRESHOLD : %d\n", RAW_ZERO_THRESHOLD);
  Serial.printf("  Amostras por leitura: 1000 x 2ms = ~2s\n");
  Serial.printf("  Intervalo entre leituras: 5s\n");
  Serial.println("-------------------------------------------------------");
  Serial.println("  GUIA DE DIAGNOSTICO:");
  Serial.println("  1. Leitura normal -> anote o raw e mV");
  Serial.println("  2. Cubra o sensor com a mao -> deve cair para raw~0");
  Serial.println("  3. Apague as lampadas -> se cair, e a lampada CFL/UV");
  Serial.println("  4. Afaste de janelas -> se cair, e luz solar UV-A");
  Serial.println("=======================================================\n");

  int diagRaw = analogRead(UV_PIN);
  Serial.printf("[DIAG] analogRead(32) imediato = %d\n\n", diagRaw);

  Serial.println("Iniciando leituras a cada 5 segundos...\n");
}

void loop() {
  lerUV();
  delay(5000);
}
