# Script para fazer upload no ESP32
# Uso: .\upload_esp32.ps1 -Port COM3

param(
    [string]$Port = "COM3",
    [string]$BaudRate = "921600"
)

$python = "C:/Users/quimi/Desktop/DEV/Maui/Esp32/.venv/Scripts/python.exe"
$inoFile = "c:\Users\quimi\Desktop\DEV\Maui\Esp32\ESP32_Arduino_Code\ESP32_WiFi_Controller_Complete.ino"

Write-Host "╔════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Upload para ESP32 via esptool    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "❗ IMPORTANTE:" -ForegroundColor Yellow
Write-Host "1. Este script requer que você tenha compilado o .ino via Arduino IDE primeiro"
Write-Host "2. Procure o arquivo .bin em: Arduino IDE → Sketch → Show Sketch Folder"
Write-Host "3. Ou use o Arduino IDE para fazer o upload diretamente (Ctrl+U)"
Write-Host ""

Write-Host "Porta COM: $Port" -ForegroundColor Green
Write-Host "Baud Rate: $BaudRate" -ForegroundColor Green
Write-Host ""

# Alternativa: Usar Arduino IDE via linha de comando
Write-Host "💡 Recomendado: Abra Arduino IDE e faça Ctrl+U para upload direto" -ForegroundColor Cyan
