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

#define UV_PIN  32

// Tabela GUVA-S12SD oficial (mV -> UV index)
const int TABELA_MV[] = {  50, 227, 318, 408, 503, 606, 696, 795, 881,  976, 1079, 1170 };
const int TABELA_UV[] = {   0,   1,   2,   3,   4,   5,   6,   7,   8,    9,   10,   11 };
const int TAB_SIZE    = 12;

int leituraNum = 0;

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

  // Descartar 20 leituras iniciais (estabilizar ADC)
  for (int d = 0; d < 20; d++) { analogRead(UV_PIN); delay(2); }

  const int N = 64;  // 64 x 2ms = ~130ms por leitura
  long soma_mV = 0;
  int  soma_raw = 0, minRaw = 4095, maxRaw = 0;

  for (int i = 0; i < N; i++) {
    int raw = analogRead(UV_PIN);
    int mV  = analogReadMilliVolts(UV_PIN);
    soma_raw += raw;
    soma_mV  += mV;
    if (raw < minRaw) minRaw = raw;
    if (raw > maxRaw) maxRaw = raw;
    delay(2);
  }

  float media_raw = (float)soma_raw / N;
  float tensao_mV = (float)soma_mV  / N;
  int   spread    = maxRaw - minRaw;

  // Pino flutuante: ADC oscila em quase toda a faixa (0-4095)
  bool flutuando = (spread > 3500);
  if (flutuando) { tensao_mV = 0.0f; media_raw = 0.0f; }

  // Sem UV: tensao abaixo do minimo do sensor (50mV)
  bool sem_sinal = (!flutuando && tensao_mV < 50.0f);

  float indiceUV = mVparaIndiceUV(tensao_mV);
  if (indiceUV > 15) indiceUV = 15;

  bool suspeita_lampada = (!flutuando && !sem_sinal && tensao_mV < 500.0f);
  bool sinal_solar      = (!flutuando && !sem_sinal && tensao_mV >= 500.0f);

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
  Serial.printf( "  Tensao     : %.1f mV  (analogReadMilliVolts)\n", tensao_mV);
  Serial.printf( "  Indice UV  : %.2f  [%s]\n", indiceUV, risco);

  // Linha de diagnostico
  Serial.println("  ---- DIAGNOSTICO ----");
  if (flutuando) {
    Serial.printf("  [!] PINO FLUTUANTE (spread=%d) - GPIO32 sem sinal estavel\n", spread);
    Serial.println("      Verifique: OUT do GUVA-S12SD no GPIO 32?");
    Serial.println("      Verifique: VCC em 3.3V (nao 5V)?");
  } else if (sem_sinal) {
    Serial.printf( "      Sem UV detectado (mV=%.1f < 50mV) - normal em ambiente interno.\n", tensao_mV);
    Serial.println("      Teste: aponte para luz solar ou lampada UV.");
  } else if (suspeita_lampada) {
    Serial.println("  [?] POSSIVEL LAMPADA FLUORESCENTE/CFL");
    Serial.printf( "      mV=%.1f -> UV index %.2f\n", tensao_mV, indiceUV);
    Serial.println("      Cubra o sensor com a mao: se cair = sinal real da lampada.");
  } else if (sinal_solar) {
    Serial.println("  [!] UV FORTE - luz solar ou UV direto");
    Serial.printf( "      mV=%.1f -> UV index %.2f\n", tensao_mV, indiceUV);
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

  // Configurar ADC: 12-bit, 11dB (0-3.3V), API Arduino pura (arduino-esp32 v3.x)
  analogReadResolution(12);
  analogSetPinAttenuation(UV_PIN, ADC_11db);

  Serial.println("\n=======================================================");
  Serial.println("  GUVA-S12SD UV DIAGNOSTIC TEST - ESP32");
  Serial.println("  API: analogReadMilliVolts (arduino-esp32 v3.x)");
  Serial.println("  GPIO 32 | ADC1_CH4 | 3.3V");
  Serial.println("=======================================================");
  Serial.printf("  Amostras por leitura: 64 x 2ms = ~130ms\n");
  Serial.printf("  Intervalo entre leituras: 5s\n");
  Serial.println("-------------------------------------------------------");
  Serial.println("  GUIA DE DIAGNOSTICO:");
  Serial.println("  1. Cubra o sensor com a mao -> deve cair para ~0mV");
  Serial.println("  2. Apague as lampadas -> se cair, e a lampada CFL/UV");
  Serial.println("  3. Aponte para o sol -> deve subir para >500mV");
  Serial.println("=======================================================\n");

  int diagRaw = analogRead(UV_PIN);
  int diagMv  = analogReadMilliVolts(UV_PIN);
  Serial.printf("[DIAG] raw=%d  mV=%d\n\n", diagRaw, diagMv);

  Serial.println("Iniciando leituras a cada 5 segundos...\n");
}

void loop() {
  lerUV();
  delay(5000);
}
