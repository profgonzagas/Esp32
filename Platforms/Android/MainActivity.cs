using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android;

namespace ESP32Controller;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int RequestCodePermissions = 1;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        // Solicitar permissões necessárias
        SolicitarPermissoes();
    }

    private void SolicitarPermissoes()
    {
        var permissoesNecessarias = new List<string>
        {
            Manifest.Permission.Internet,
            Manifest.Permission.AccessNetworkState,
            Manifest.Permission.AccessFineLocation,
            Manifest.Permission.AccessCoarseLocation
        };

        // Android 12+ (API 31+) requer novas permissões BLE
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            permissoesNecessarias.Add(Manifest.Permission.BluetoothScan);
            permissoesNecessarias.Add(Manifest.Permission.BluetoothConnect);
        }
        else
        {
            permissoesNecessarias.Add(Manifest.Permission.Bluetooth);
            permissoesNecessarias.Add(Manifest.Permission.BluetoothAdmin);
        }

        var permissoesPendentes = permissoesNecessarias
            .Where(p => ContextCompat.CheckSelfPermission(this, p) != Permission.Granted)
            .ToArray();

        if (permissoesPendentes.Length > 0)
        {
            ActivityCompat.RequestPermissions(this, permissoesPendentes, RequestCodePermissions);
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == RequestCodePermissions)
        {
            var negadas = new List<string>();
            for (int i = 0; i < permissions.Length; i++)
            {
                if (grantResults[i] != Permission.Granted)
                {
                    negadas.Add(permissions[i]);
                }
            }

            if (negadas.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Permissões] Negadas: {string.Join(", ", negadas)}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Permissões] Todas concedidas!");
            }
        }
    }
}
