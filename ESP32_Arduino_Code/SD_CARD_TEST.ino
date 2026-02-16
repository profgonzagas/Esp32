/*
 * ESP32 Cartão SD - Teste Simples
 * 
 * Use este código para testar se o cartão SD está funcionando corretamente
 * antes de usar no código completo.
 * 
 * Bibliotecas necessárias:
 * - SD.h (já vem com Arduino IDE)
 * - SPI.h (já vem com Arduino IDE)
 */

#include <SD.h>
#include <SPI.h>

// Definir pinos SPI para cartão SD
#define SD_CS_PIN 5        // Chip Select (CS)
#define SPI_CLK 18         // Clock (SCK/CLK)
#define SPI_MOSI 23        // Master Out Slave In (MOSI/DIN)
#define SPI_MISO 19        // Master In Slave Out (MISO/DO)

File dataFile;

void setup() {
  // Inicializar Serial
  Serial.begin(115200);
  delay(100);
  
  Serial.println("\n\n╔════════════════════════════════════╗");
  Serial.println("║   Teste Cartão SD - ESP32         ║");
  Serial.println("║   Adaptador SPI da Imagem          ║");
  Serial.println("╚════════════════════════════════════╝");
  debug_info();
  
  // Inicializar SPI
  Serial.println("\n[1/3] Inicializando SPI...");
  SPI.begin(SPI_CLK, SPI_MISO, SPI_MOSI, SD_CS_PIN);
  delay(100);
  Serial.println("✓ SPI inicializado");
  Serial.printf("      CLK:  GPIO %d\n", SPI_CLK);
  Serial.printf("      MISO: GPIO %d\n", SPI_MISO);
  Serial.printf("      MOSI: GPIO %d\n", SPI_MOSI);
  Serial.printf("      CS:   GPIO %d\n", SD_CS_PIN);
  
  // Tentar montar cartão SD
  Serial.println("\n[2/3] Montando cartão SD...");
  if (!SD.begin(SD_CS_PIN)) {
    Serial.println("✗ ERRO: Não foi possível montar o cartão SD!");
    Serial.println("\nVerifique:");
    Serial.println("  1. Se o cartão SD está inserido corretamente");
    Serial.println("  2. Se o cartão SD não está danificado");
    Serial.println("  3. Conexões:");
    Serial.println("     - VCC → 3.3V");
    Serial.println("     - GND → GND");
    Serial.println("     - CLK → GPIO 18");
    Serial.println("     - MOSI (DIN) → GPIO 23");
    Serial.println("     - MISO (DO) → GPIO 19");
    Serial.println("     - CS → GPIO 5");
    Serial.println("\nAbortando...");
    while (1) delay(100);
  }
  
  Serial.println("✓ Cartão SD montado com sucesso!");
  
  // Obter informações do cartão
  Serial.println("\n[3/3] Obtendo informações do cartão...");
  listarArquivos("/");
  
  Serial.println("\n=== TESTE DE ESCRITA/LEITURA ===\n");
  testeEscrita();
  
  Serial.println("\n=== TESTE DE APPEND ===\n");
  testeAppend();
  
  delay(1000);
}

void loop() {
  Serial.println("\n╔════════ CARTÃO SD ATIVO ════════╗");
  
  // Tamanho do cartão
  uint64_t cardSize = SD.cardSize() / (1024 * 1024);
  Serial.printf("║ 💾 Tamanho Total: %llu MB       ║\n", cardSize);
  
  // Espaço usedo e disponível (estimado)
  Serial.println("║ 📂 Arquivos:                     ║");
  File root = SD.open("/");
  int fileCount = 0;
  
  while (File entry = root.openNextFile()) {
    if (!entry.isDirectory()) {
      fileCount++;
      Serial.printf("║    - %s (%d bytes)\n", entry.name(), entry.size());
    }
    entry.close();
  }
  root.close();
  
  Serial.printf("║    Total: %d arquivos           ║\n", fileCount);
  Serial.println("╚═════════════════════════════════╝");
  
  // Aguardar 5 segundos
  delay(5000);
}

/**
 * Listar todos os arquivos em um diretório
 */
void listarArquivos(const char * dirname) {
  Serial.printf("\n📁 Listando arquivos em: %s\n", dirname);
  
  File root = SD.open(dirname);
  if (!root) {
    Serial.println("✗ Não foi possível abrir o diretório");
    return;
  }
  
  File entry = root.openNextFile();
  int count = 0;
  
  while (entry) {
    count++;
    if (entry.isDirectory()) {
      Serial.printf("   📂 %s/\n", entry.name());
    } else {
      Serial.printf("   📄 %s (%d bytes)\n", entry.name(), entry.size());
    }
    entry = root.openNextFile();
  }
  
  entry.close();
  root.close();
  
  if (count == 0) {
    Serial.println("   (nenhum arquivo encontrado)");
  }
}

/**
 * Testar escrita no cartão
 */
void testeEscrita() {
  Serial.println("Escrevendo arquivo de teste: /teste.txt");
  
  File file = SD.open("/teste.txt", FILE_WRITE);
  if (!file) {
    Serial.println("✗ Erro ao criar arquivo");
    return;
  }
  
  // Escrever dados
  file.println("=== Teste de Escrita Cartao SD ===");
  file.printf("Data/Hora (timestamp): %lu\n", millis());
  file.println("Este eh um arquivo de teste");
  file.println("Linha 1: Teste 1");
  file.println("Linha 2: Teste 2");
  file.println("Linha 3: Teste 3");
  file.println("=== Fim do Teste ===");
  
  size_t bytesWritten = file.size();
  file.close();
  
  Serial.printf("✓ Arquivo criado com sucesso!\n");
  Serial.printf("  Bytes escritos: %d\n", bytesWritten);
  
  // Ler arquivo
  Serial.println("\nLendo arquivo de teste:");
  file = SD.open("/teste.txt", FILE_READ);
  
  while (file.available()) {
    Serial.write(file.read());
  }
  file.close();
}

/**
 * Testar append (adicionar ao arquivo)
 */
void testeAppend() {
  String filename = "/dados_sensores.csv";
  Serial.printf("Adicionando dados a: %s\n", filename.c_str());
  
  // Criar arquivo se nao existir
  File file = SD.open(filename, FILE_APPEND);
  if (!file) {
    Serial.println("✗ Erro ao abrir arquivo");
    return;
  }
  
  // Adicionar cabeçalho se arquivo esta vazio
  if (file.size() == 0) {
    file.println("Timestamp,Temperatura,Umidade,Pressao,Indice_UV");
  }
  
  // Adicionar dados de exemplo
  static int contador = 0;
  for (int i = 0; i < 5; i++) {
    file.printf("%lu,%.2f,%.2f,%.2f,%.2f\n", 
                millis() + (i * 1000),
                25.5 + i,
                45.3 + (i * 2),
                1013.25 - i,
                5.5 + (i * 0.5));
  }
  
  file.close();
  Serial.printf("✓ %d linhas adicionadas!\n", 5);
  
  // Ler arquivo
  Serial.println("\nConteúdo do arquivo CSV:");
  file = SD.open(filename, FILE_READ);
  
  int linhas = 0;
  while (file.available()) {
    String linha = file.readStringUntil('\n');
    Serial.println(linha);
    linhas++;
  }
  file.close();
  
  Serial.printf("\n✓ Total de %d linhas no arquivo\n", linhas);
}

void debug_info() {
  Serial.println("\n📋 Informações de Debug:");
  Serial.printf("   Velocidade Serial: 115200 baud\n");
  Serial.printf("   CS (Chip Select): GPIO %d\n", SD_CS_PIN);
  Serial.printf("   CLK (Clock): GPIO %d\n", SPI_CLK);
  Serial.printf("   MOSI (Data In): GPIO %d\n", SPI_MOSI);
  Serial.printf("   MISO (Data Out): GPIO %d\n", SPI_MISO);
  Serial.println("   Tipo: SPI (Serial Peripheral Interface)");
}

/*
 * TROUBLESHOOTING:
 * 
 * 1. Cartão SD não é detectado:
 *    - Verificar se está bem inserido
 *    - Tentar reformatar o cartão em FAT32
 *    - Se usar adaptador, verificar conexões
 *    
 * 2. Erro "SD.begin() failed":
 *    - Verificar voltagem (3.3V não 5V!)
 *    - Verificar pino CS (GPIO 5)
 *    - Algum pino SPI pode estar ocupado
 *    
 * 3. Cartão não grava dados:
 *    - Verificar se o cartão não está protegido
 *    - Verificar espaço disponível no cartão
 *    - Tentar outra classe/marca do cartão
 *    
 * 4. Leitura lenta:
 *    - Normal para cartão SD via SPI
 *    - Usar arquivo.flush() para forçar escrita
 *    - Considerar usar SD.h buffering
 * 
 * 5. Conexões do adaptador SD:
 *    - GND (preto) → GND
 *    - VCC (vermelho) → 3.3V
 *    - MOSI (DIN, Data In) → GPIO 23
 *    - MISO (DO, Data Out) → GPIO 19
 *    - CLK (Clock) → GPIO 18
 *    - CS (Chip Select) → GPIO 5
 */
