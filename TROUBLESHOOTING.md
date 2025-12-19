## 🔧 Troubleshooting WiFi - Passos para Debug

### 1. Verificar configuração na app

Na tela **Configurações**, confirme:
- ✅ IP: **192.168.1.128**
- ✅ Porta: **80**
- ✅ SSID: **TIM ULTRAFIBRA_B4D2**

### 2. Testar conectividade

**Windows (PowerShell):**
```powershell
# Ping
ping 192.168.1.128

# HTTP Status
curl http://192.168.1.128/status
curl http://192.168.1.128/led/on
curl http://192.168.1.128/sensores
```

**Android:**
Abra qualquer navegador e acesse:
```
http://192.168.1.128/status
```

Deve retornar JSON como:
```json
{"status":"conectado","dispositivo":"NecroSENSE ESP32","ip":"192.168.1.128",...}
```

### 3. Verificar permissões Android

A app precisa dessas permissões:
- ✅ INTERNET (pedida automaticamente)
- ✅ ACCESS_NETWORK_STATE (pedida automaticamente)

**Comando para verificar:**
```bash
adb shell pm list permissions | grep INTERNET
```

### 4. Logs da app

Se ainda não conectar:

**Option A - Debug Logs:**
```bash
# Ver logs da app em tempo real
adb logcat | grep ESP32
adb logcat | grep WiFi
```

**Option B - Por dentro da app:**
- Abra Output/Última Resposta
- Deve mostrar erro específico

### 5. Forçar Reconexão

1. Abra **Configurações**
2. Salve o dispositivo novamente
3. Volte para Dashboard
4. Clique **Testar Conexão**

### Possíveis Problemas

| Problema | Solução |
|----------|---------|
| "Timeout" | Aumentar Timeout em ESP32HttpService (já feito: 10s) |
| "Connection refused" | ESP32 pode estar em outro IP - confira com `ping 192.168.1.128` |
| "SSL Error" | Já ignoramos certificados - não deve ser problema |
| Sem internet na app | Verificar se Android está na mesma rede WiFi |

### Checklist Final

- [ ] Android na mesma rede WiFi (TIM ULTRAFIBRA_B4D2)
- [ ] ESP32 respondendo (curl funciona no PC)
- [ ] IP 192.168.1.128 correto
- [ ] Porta 80 aberta
- [ ] App com permissão INTERNET
- [ ] Timeout definido em 10 segundos

---

**Se ainda não funcionar:**
Envie os logs do adb para análise:
```bash
adb logcat > logs.txt
```
