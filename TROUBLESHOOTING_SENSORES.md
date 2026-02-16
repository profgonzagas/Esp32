# 🔧 Guia de Troubleshooting - Sensores com Leitura Zero

## ❌ Problema: App mostra 0 nos sensores BME280 e DHT22

---

## 🔍 **PASSO 1: Verificar Hardware**

### ✓ Checklist de Conexões:

**BME280 (I2C):**
- [ ] ESP32 GPIO21 (SDA) → BME280 SDA
- [ ] ESP32 GPIO22 (SCL) → BME280 SCL
- [ ] ESP32 3.3V → BME280 VCC (⚠️ NUNCA 5V!)
- [ ] ESP32 GND → BME280 GND

**DHT22 (Digital):**
- [ ] ESP32 GPIO4 → DHT22 DATA
- [ ] ESP32 3.3V → DHT22 + (⚠️ NUNCA 5V!)
- [ ] ESP32 GND → DHT22 -
- [ ] **IMPORTANTE**: Resistor pull-up 4.7kΩ entre GPIO4 e 3.3V

**UV GUVA-S12SD:**
- [ ] ESP32 GPIO34 (ADC) → GUVA-S12SD OUT
- [ ] ESP32 3.3V → GUVA-S12SD VCC
- [ ] ESP32 GND → GUVA-S12SD GND

---

## 🚀 **PASSO 2: Testar Sensores Isoladamente**

### Método: Usar Sketch de Diagnóstico

1. **Abra Arduino IDE**
2. **File → Open:**
   ```
   c:\Users\quimi\Desktop\DEV\Maui\Esp32\ESP32_Arduino_Code\
   DIAGNOSTICO_SENSORES.ino
   ```

3. **Faça Upload** (Ctrl+U)

4. **Abra Serial Monitor** (Ctrl+Shift+M)

5. **Você verá:**
   ```
   ========================================
       DIAGNÓSTICO DE SENSORES ESP32
   ========================================

   [1] Testando BME280...
   ✓ BME280 detectado!
      Temperatura: 24.56 °C
      Umidade: 45.23 %
      Pressão: 1013.25 hPa

   [2] Testando DHT22...
   ✓ DHT22 funcionando!
      Temperatura: 24.30 °C
      Umidade: 46.10 %

   [3] Testando Sensor UV...
   ✓ Sensor UV funcionando!
      Valor ADC: 450
   ```

---

## ❌ Se Aparecer Erro:

### **Caso 1: "BME280 NÃO ENCONTRADO"**

```
❌ BME280 NÃO ENCONTRADO!
   Verifique:
   - Conexões I2C (GPIO 21=SDA, GPIO 22=SCL)
   - Voltagem 3.3V
   - Endereço: 0x77
```

**Soluções:**
1. Verifique voltagem com multímetro
2. Verifique conexões com o sensor
3. Tente trocar a posição dos fios (SDA/SCL invertidos?)
4. Verifique se o sensor não está danificado

---

### **Caso 2: "DHT22 NÃO RESPONDEU"**

```
❌ DHT22 NÃO RESPONDEU!
   Verifique:
   - Conexão GPIO 4
   - Resistor pull-up 4.7kΩ
   - Voltagem 3.3V
```

**Soluções:**
1. **CRÍTICO**: Verifique resistor pull-up 4.7kΩ entre GPIO4 e 3.3V
2. Verifique voltagem
3. DHT22 pode estar danificado (tente trocar)

---

## 🚀 **PASSO 3: Se Tudo OK, Upload do Código Principal**

Se o diagnóstico mostra ✓ em todos os sensores:

1. **Abra Arduino IDE**
2. **File → Open:**
   ```
   c:\Users\quimi\Desktop\DEV\Maui\Esp32\ESP32_Arduino_Code\
   ESP32_WiFi_Controller_Complete.ino
   ```

3. **Altere credenciais WiFi (linhas 21-22):**
   ```cpp
   const char* ssid = "SEU_SSID";
   const char* password = "SUA_SENHA";
   ```

4. **Faça Upload** (Ctrl+U)

5. **Abra Serial Monitor e anote:**
   - ✓ BME280 inicializado
   - ✓ DHT22 inicializado
   - ✓ WiFi conectado
   - **IP: 192.168.X.X** ← Anote este IP!

---

## 📱 **PASSO 4: Configurar App**

1. **Abra ESP32Controller** no celular
2. **Configurações ⚙️**
3. **Cole o IP** que apareceu no Serial Monitor
4. **Clique em Conectar**
5. **Vá para Dashboard**

---

## 🌐 **PASSO 5: Testar via Browser (Opcional)**

Abra no navegador do PC:
```
http://192.168.X.X/status
```

Você verá um JSON como:
```json
{
  "sensores": {
    "bme280": {
      "temperatura": 24.56,
      "umidade": 45.23,
      "pressao": 1013.25
    },
    "dht22": {
      "temperatura": 24.30,
      "umidade": 46.10
    },
    "uv": {
      "nivel": 450,
      "indice": 7.25
    }
  }
}
```

---

## 🔄 **Checklist Final**

- [ ] Hardware verificado e conexões corretas
- [ ] Sketch de diagnóstico mostra ✓ em todos os sensores
- [ ] Código principal (ESP32_WiFi_Controller_Complete.ino) foi feito upload
- [ ] Serial Monitor mostra WiFi conectado e IP
- [ ] IP foi inserido corretamente no app
- [ ] App mostra dados dos sensores no Dashboard

---

## 💡 **Dicas Extra**

1. **Reset do ESP32**: Pressione botão RESET no ESP32
2. **Reboot do WiFi**: Desligue e ligue o roteador
3. **Reboot do App**: Feche e abra novamente
4. **Verificar Conexão**: Ping no ESP32 pelo PC
   ```
   ping 192.168.X.X
   ```

---

**Qual foi o resultado do diagnóstico?** Verifique e me avisa! 🚀
