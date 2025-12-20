using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace ESP32Controller.Services;

public class PermissionsService
{
    public static async Task<bool> RequestBluetoothPermissionsAsync()
    {
        try
        {
            // Android 12+ (API 31+) requires these permissions
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                var status = await Permissions.CheckStatusAsync<BluetoothPermission>();
                
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<BluetoothPermission>();
                }
                
                System.Diagnostics.Debug.WriteLine($"[Permissions] Bluetooth: {status}");
                
                return status == PermissionStatus.Granted;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Permissions] Erro ao solicitar permissões: {ex.Message}");
            return false;
        }
    }
}

// Custom permission classes for BLE
public class BluetoothPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[]
        {
            ("android.permission.BLUETOOTH_SCAN", true),
            ("android.permission.BLUETOOTH_CONNECT", true),
            ("android.permission.ACCESS_FINE_LOCATION", true),
        };
}
