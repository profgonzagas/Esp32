import serial, time

s = serial.Serial('COM5', 115200, timeout=2)
time.sleep(1)
print("=== Leituras UV (30 linhas) ===")
for _ in range(50):
    raw = s.readline()
    try:
        line = raw.decode('utf-8').strip()
    except:
        line = raw.decode('latin-1').strip()
    if line:
        print(line)
s.close()
