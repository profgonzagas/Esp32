# Guia de Conexão ESP32 - Troubleshooting

## ✅ Checklist de Verificação

### 1. ESP32 (Hardware)
- [ ] ESP32 está ligado e energizado
- [ ] LED embutido está piscando (indica que o código está rodando)
- [ ] Serial Monitor mostra mensagens de inicialização

### 2. WiFi (ESP32)
- [ ] Verificar credenciais WiFi em `secrets.h`:
  - SSID: Configure em `secrets.h` (veja `secrets.h.example`)
  - Password: Configure em `secrets.h` (veja `secrets.h.example`)
- [ ] ESP32 conseguiu conectar ao WiFi (verificar Serial Monitor)
- [ ] IP do ESP32 é: `???` (aparece no Serial Monitor na inicialização)
- [ ] Testar ping do PC para o ESP32: `ping 192.168.1.XXX`

### 3. Configuração do App (Android)
- [ ] Atualizar IP correto no app (Configurações)
- [ ] Porta está configurada como: `80`
- [ ] Permissões do Android foram concedidas:
  - Internet ✓
  - Bluetooth ✓  
  - Localização ✓
- [ ] Celular está na mesma rede WiFi que o ESP32

### 4. BLE (Bluetooth)
- [ ] Bluetooth está ativado no celular
- [ ] Localização está ativada (necessário para BLE no Android)
- [ ] ESP32 está advertising (Serial Monitor mostra "BLE iniciado")
- [ ] Nome do dispositivo BLE: `NecroSENSE`

### 5. Testes de Conectividade

#### Testar HTTP (WiFi):
```bash
# Do PC ou celular na mesma rede:
curl http://192.168.1.XXX/status
# Ou abrir no navegador do celular:
http://192.168.1.XXX/status
```

#### Testar BLE:
1. Abrir aba "Bluetooth" no app
2. Clicar em "Escanear Dispositivos"
3. Deve aparecer "NecroSENSE" na lista
4. Selecionar e clicar "Conectar"

## 🔧 Como Obter o IP do ESP32

### Opção 1: Serial Monitor (PlatformIO)
1. Conectar ESP32 via USB
2. Abrir Terminal no VS Code
3. Executar:
```bash
# Se PlatformIO CLI estiver disponível:
platformio device monitor

# Ou usar comando direto da porta COM:
# Ver portas disponíveis primeiro:
mode
```

### Opção 2: Router
1. Acessar interface web do router (geralmente 192.168.1.1)
2. Procurar dispositivos conectados
3. Buscar por "ESP32" ou "NecroSENSE"

### Opção 3: App de Scan de Rede
- Baixar app "Fing" ou similar no celular
- Escanear rede local
- Procurar por dispositivo ESP32

## 🐛 Problemas Comuns

### "Bluetooth está desligado"
**Solução:**
1. Ativar Bluetooth no celular
2. Ativar Localização (GPS) - obrigatório para BLE no Android
3. Conceder permissões de Localização para o app

### "Não foi possível conectar ao ESP32" (HTTP)
**Soluções:**
1. Verificar se ESP32 e celular estão na mesma rede WiFi
2. Confirmar IP correto do ESP32
3. Testar conexão HTTP no navegador do celular primeiro
4. Verificar se firewall não está bloqueando

### "Nenhum dispositivo encontrado" (BLE)
**Soluções:**
1. Verificar se ESP32 está ligado
2. Verificar Serial Monitor - deve mostrar "BLE iniciado"
3. Aproximar celular do ESP32 (< 5 metros)
4. Desligar e ligar Bluetooth do celular
5. Reiniciar ESP32
6. Verificar se Localização está ativada

### "Timeout ao conectar"
**Soluções:**
1. ESP32 pode ter travado - reiniciar
2. Verificar se código está rodando (LED piscando)
3. Verificar logs no Serial Monitor
4. Tentar desconectar e conectar novamente

## 📱 Como Ver Logs do App (Android)

### Usar Logcat:
```bash
# Conectar celular via USB com Debug USB ativado
adb logcat | Select-String "ESP32|BLE|WiFi"
```

### Ver no VS Code:
- Os logs aparecem na janela Debug do VS Code durante desenvolvimento
- Procurar por mensagens com `[BLE]` ou `[WiFi]`

## 🔄 Fluxo de Conexão Normal

### WiFi (HTTP):
1. ESP32 conecta ao WiFi na inicialização
2. Servidor HTTP inicia na porta 80
3. App testa conexão: GET /status
4. Se bem-sucedido: ícone verde, "Conectado"

### BLE (Bluetooth):
1. ESP32 inicia BLE advertising
2. App escaneia dispositivos BLE próximos
3. Encontra "NecroSENSE"
4. Conecta e descobre serviços/características
5. Se bem-sucedido: ícone verde, "Conectado via BLE"

## 💡 Dicas

1. **Sempre começar verificando o Serial Monitor do ESP32** - ele mostra tudo que está acontecendo
2. **Testar HTTP primeiro no navegador** antes de usar o app
3. **BLE tem alcance limitado** - manter dispositivos próximos
4. **Reiniciar o ESP32 resolve muitos problemas** - ele é pequeno e pode travar
5. **Permissões no Android são críticas** - sempre conceder todas quando solicitadas

## 📞 Informações de Rede Atual

- **WiFi SSID**: Configure em `secrets.h` (veja `secrets.h.example`)
- **IP Padrão Configurado no App**: `192.168.1.128`
- **Porta HTTP**: `80`
- **Nome BLE**: `NecroSENSE`
- **BLE Service UUID**: `4fafc201-1fb5-459e-8fcc-c5c9c331914b`

## 🚀 Próximos Passos

1. Upload do firmware no ESP32 via PlatformIO
2. Verificar IP real do ESP32 no Serial Monitor
3. Atualizar IP no app (se diferente de 192.168.1.128)
4. Testar conexão HTTP no navegador
5. Testar conexão BLE no app
6. Se tudo funcionar, testar comandos (LED, relés, etc)
