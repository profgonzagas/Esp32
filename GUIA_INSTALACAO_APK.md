# 📱 Guia de Instalação do App ESP32Controller no Celular

## 📥 Onde Está o APK?

```
📍 Caminho no PC:
c:\Users\quimi\Desktop\ESP32Controller.apk (27.71 MB)

📍 Também está em:
c:\Users\quimi\Desktop\DEV\Maui\Esp32\bin\Release\net9.0-android\
   ├─ com.companyname.esp32controller-Signed.apk (versão assinada)
   └─ com.companyname.esp32controller.aab (para Play Store)
```

---

## 🔧 Pré-requisitos

### No Celular Android
1. **Ativar Instalação de Fontes Desconhecidas**
   - Ir para: **Configurações → Segurança**
   - Procure por: "Aplicativos de fontes desconhecidas" ou "Instalar aplicativos com fontes desconhecidas"
   - Ative para o seu navegador ou gerenciador de arquivos
   - ⚠️ Alguns celulares Android 10+ têm essa opção em: **Configurações → Privacidade**

2. **Espaço em Disco**
   - Mínimo: ~50 MB livres

3. **Versão Android Mínima**
   - Android 9.0 ou superior (21+)

---

## 📤 Método 1: Via USB (Recomendado)

### Passo 1: Conectar Celular via USB
1. Conecte o cabo USB do celular ao PC
2. No celular, ative o **Modo de Transferência de Arquivos** ou **MTP**
   - Se aparecer notificação: Toque em "Usar USB para" → Selecione "Transferência de arquivos"

### Passo 2: Copiar APK
1. No PC, abra o Explorador de Arquivos
2. Navegue para: `c:\Users\quimi\Desktop\`
3. Você verá: `ESP32Controller.apk`
4. Copie este arquivo (Ctrl+C)
5. Cole no celular em qualquer pasta (Ctrl+V)
   - Recomendado: **Downloads** ou **Documentos**

### Passo 3: Instalar no Celular
1. Abra o Gerenciador de Arquivos do celular
2. Navegue até a pasta onde copiou o APK
3. Toque no arquivo: `ESP32Controller.apk`
4. O celular perguntará: "Deseja instalar este aplicativo?"
5. Toque em: **INSTALAR**
6. Aguarde...
7. Quando vir "Aplicativo instalado", toque em **ABRIR** ou **Fechar**

### ✅ Pronto!
O app agora está instalado no seu celular! 

---

## 📤 Método 2: Via Bluetooth

### Passo 1: Ativar Bluetooth
1. Ligue o Bluetooth no PC em: **Configurações → Bluetooth**
2. Ligue o Bluetooth no celular
3. Parear o celular com o PC

### Passo 2: Enviar Arquivo
1. No PC, abra o Explorador: `c:\Users\quimi\Desktop\`
2. Clique com botão direito em: `ESP32Controller.apk`
3. Selecione: **Enviar Para → Dispositivo Bluetooth → [Seu Celular]**

### Passo 3: Instalar
1. O celular receberá uma notificação
2. Aceite a transferência
3. Quando terminar, abra o arquivo e instale

---

## 📤 Método 3: Via E-mail ou Cloud

### Gmail
1. Acesse: https://mail.google.com
2. Novo Email
3. Clique em Anexar 📎
4. Selecione: `ESP32Controller.apk`
5. Envie para seu e-mail
6. Abra o e-mail no celular
7. Baixe o anexo
8. Instale o APK

### Google Drive
1. Acesse: https://drive.google.com
2. Clique em **Novo** → **Upload de arquivo**
3. Selecione: `ESP32Controller.apk`
4. No celular, abra Google Drive
5. Toque no arquivo
6. Toque em "Fazer Download" (⬇️)
7. Abra e instale

### OneDrive / Dropbox
- Processe similar ao Google Drive

---

## 📤 Método 4: Via WhatsApp / Telegram

1. Abra o PC Web do WhatsApp: https://web.whatsapp.com
2. Envie o arquivo para si mesmo (em um chat pessoal ou grupo)
3. No celular, abra o WhatsApp
4. Localize a mensagem com o arquivo
5. Toque em baixar ⬇️
6. Abra o APK e instale

---

## ✅ Verificar Instalação

### Após instalar:
1. Procure o app: **ESP32Controller** 
2. Procure no menu de apps
3. Deve haver um ícone com a cor padrão MAUI

### Primeira Execução:
1. Abra o app
2. Será pedido permissões (Local Network, Bluetooth opcional)
3. Aceite as permissões
4. Vá para **Configurações**
5. Insira o **IP do seu ESP32** (ex: 192.168.1.100)
6. Clique em **Conectar**
7. Se conectou, vá para **Dashboard**

---

## 🔌 Conectar ao ESP32

1. **Primeiro**: Upload do código Arduino no ESP32 (veja UPLOAD_ESP32.md)
2. **Anotar**: O IP que aparece no Serial Monitor
3. **No App**: Configurações → IP do dispositivo → Cole o IP
4. **Clique**: Conectar
5. **Pronto**: Dashboard mostra dados dos sensores

---

## ⚠️ Problemas Comuns

### "Não consegui instalar" ou "Arquivo danificado"
```
→ O arquivo APK pode estar corrupto
→ Tente transferir novamente pelo Método 1 (USB)
→ Verifique se a cópia terminou (27.71 MB)
```

### "Instalação bloqueada - Fontes desconhecidas"
```
→ Ative em: Configurações → Segurança → Fontes Desconhecidas
→ OU em: Configurações → Privacidade → Aplicativos Desconhecidos
→ Tente novamente
```

### App abre mas não carrega dados
```
→ Verifique IP do ESP32 em Configurações
→ Verifique se ESP32 está ligado e conectado ao WiFi
→ Verifique se celular está na mesma rede WiFi
→ Tente reiniciar o app
```

### "Permissão negada"
```
→ Na primeira abertura, aceite TODAS as permissões solicitadas
→ Se recusou, vá em Configurações → Permissões e ative manualmente
```

---

## 📊 Informações do APK

| Propriedade | Valor |
|-------------|-------|
| Nome | ESP32Controller |
| Versão | 1.0 |
| Tamanho | 27.71 MB |
| Plataforma | Android 9.0+ |
| Package ID | com.companyname.esp32controller |
| Status | ✅ Assinado e pronto para instalar |

---

## 🔄 Atualizar o App

Se você modificar o código .NET MAUI e quiser atualizar:

1. No PC: `dotnet publish ESP32Controller.csproj -f net9.0-android -c Release`
2. Cópia novo APK da pasta `bin\Release\net9.0-android\`
3. Desinstale a versão antiga do celular
4. Instale o novo APK

---

## 💡 Dicas

1. **Guarde o APK em local seguro** - Você pode reinstalar quando quiser
2. **Faça backup**:
   ```
   c:\Users\quimi\Desktop\DEV\Maui\Esp32\bin\Release\net9.0-android\
   ```
3. **Compartilhe com outras pessoas** - O app é fácil de distribuir
4. **Monitore o espaço** - App ocupa ~50MB no celular

---

## 📞 Atualizar App ID (Opcional)

Se quiser mudar o nome do app:
- Edite: `ESP32Controller.csproj`
- Procure por: `<ApplicationId>`
- Mude: `com.companyname.esp32controller` para `seu.nome.appname`
- Recompile

---

**Status**: ✅ Pronto para Instalar  
**Arquivo**: ESP32Controller.apk (27.71 MB)  
**Localização**: Desktop ou `bin\Release\net9.0-android\`
