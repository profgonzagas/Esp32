# Guia de Conexão: Adaptador SD SPI → ESP32

## 📌 Identificar os Pinos no Adaptador

Seu adaptador tem 5 pinos em cada fileira com rótulos claros:

```
┌─────────────────────────────────────┐
│  ADAPTADOR SD COM PINOS SOLDADOS    │
├─────────────────────────────────────┤
│                                     │
│   ○  ○  ○  ○  ○        ← Slot SD   │
│                                     │
│  Fileira com os rótulos:            │
│  ┌─────────────────────────────┐   │
│  │ GND │ CLK │ MOSI│ MISO│ VCC│   │ ← Rótulos
│  │  ○  │  ○  │  ○  │  ○  │  ○  │   │ ← Pinos
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

**Use os rótulos que vê com seus olhos, não numerações!**

## ✅ CONEXÕES CORRETAS (SIMPLES)

Seu adaptador tem os rótulos. Aqui estão os nomes que podem aparecer:

```
NOMES POSSÍVEIS:
────────────────────────────────────────
GND             = GND (sempre assim)
CLK             = CLK ou SCK ou CLOCK
MOSI ou DI      = Data In / DIN / SDI
MISO ou DO      = Data Out / DOUT / SDO
VCC             = VCC ou 3.3V (sempre assim)
```

Se vê algo diferente na placa, consulte esta tabela:

| Rótulo na Placa | Significa | GPIO ESP32 | Cor |
|---|---|---|---|
| **GND** | Ground | GND | Preto |
| **CLK** ou **SCK** | Clock | GPIO 18 | Amarelo |
| **MOSI** ou **DI** ou **DIN** ou **DATA IN** | Entrada de Dados | GPIO 23 | Azul |
| **MISO** ou **DO** ou **DOUT** ou **DATA OUT** | Saída de Dados | GPIO 19 | Verde |
| **VCC** ou **3.3V** | Alimentação | 3.3V | Vermelho |

**O que está escrito na sua placa?**

## 🎯 Conexão Pronta para Copiar-Colar

Pegue seu adaptador, pegue o ESP32, e conecte assim:

```
PEGA NO ADAPTADOR    CONECTA NO ESP32
─────────────────────────────────────
GND            →     qualquer GND
CLK            →     GPIO 18
MOSI           →     GPIO 23
MISO           →     GPIO 19
VCC            →     3.3V (não 5V!)
```

**É isso! Pronto!**

## 🚨 ATENÇÃO: VOLTAGEM

```
⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️
    
  ❌ NUNCA CONECTAR EM 5V
  
  ✅ SEMPRE USAR 3.3V
  
  Conectar em 5V pode:
  - Queimar o adaptador SD
  - Danificar o cartão SD
  - Destruir o ESP32
  
⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️  ⚠️
```

## 📋 Checklist de Conexão

Procure na placa pelos rótulos (podem ter nomes diferentes):

- [ ] **GND** do adaptador → GND do ESP32 (Preto)
- [ ] **CLK** (ou SCK) do adaptador → GPIO 18 do ESP32 (Amarelo)
- [ ] **MOSI** (ou DI/DIN) do adaptador → GPIO 23 do ESP32 (Azul)
- [ ] **MISO** (ou DO/DOUT) do adaptador → GPIO 19 do ESP32 (Verde)
- [ ] **VCC** do adaptador → 3.3V do ESP32 (Vermelho) ⚠️
- [ ] Cartão SD inserido corretamente no adaptador
- [ ] Todas as solagens com contato firme
- [ ] Fios não soltos

## 🎯 Conexão Pronta para Copiar-Colar

Pegue seu adaptador, pegue o ESP32, e conecte assim:

```
PEGA NA PLACA               CONECTA NO ESP32
───────────────────────────────────────────
GND                    →     GND
CLK (pode ser SCK)     →     GPIO 18
MOSI ou DI ou DIN      →     GPIO 23
MISO ou DO ou DOUT     →     GPIO 19
VCC                    →     3.3V (não 5V!)
```

## 📸 O que Procurar na Placa

Se a placa está assim:
```
GND  ← OK (sempre assim)
CLK  ← ou pode estar "SCK"
???  ← pode estar "MOSI", "DI", "DIN", "SI" ou "DATA_IN"
???  ← pode estar "MISO", "DO", "DOUT", "SO" ou "DATA_OUT"
VCC  ← OK (ou pode estar "3.3V")
```

**Qual desses você vê na sua placa?**

## ✅ Pronto para Testar

1. Conecte conforme acima
2. Insira cartão SD no adaptador
3. Carregue **SD_CARD_TEST.ino**
4. Abra Serial Monitor (115200)
5. Veja a mensagem:
   ```
   ✓ Cartão SD montado com sucesso!
   ```

## 🐛 Se não funcionar

| Problema | Solução |
|----------|---------|
| "Cartão SD não detectado" | Verificar se é 3.3V, não 5V! |
| Fios soltos | Refazer as conexões com cuidado |
| Cartão não monta | Tentar reformatar em Windows |
| Pinos não soldam | Usar fio com sensores soldados 

## 🎨 Esquema com Cores e Nomes Alternativos

```
Adaptador SD               ESP32
════════════════════════════════════════

GND (Preto) ──────────→ GND (Preto)

VCC (Vermelho) ───────→ 3.3V (Vermelho)

CLK / SCK (Amarelo) ──→ GPIO 18 (Amarelo)

MOSI / DI / DIN (Azul)──→ GPIO 23 (Azul)

MISO / DO / DOUT (Verde)→ GPIO 19 (Verde)

Legenda de Nomes:
- MOSI = Master Out Slave In = DI = DIN = Data In
- MISO = Master In Slave Out = DO = DOUT = Data Out
```

## 📸 Forma Simples de Ver

```
Seu Adaptador tem estes rótulos:

GND
CLK
MOSI
MISO
VCC

Conecte EXATAMENTE como os rótulos dizem!
```
