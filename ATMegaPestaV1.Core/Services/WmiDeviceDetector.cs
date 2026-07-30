using System.Management;
using System.Text.RegularExpressions;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Detecção real, por WMI, sobre os dispositivos PnP enumerados pelo Windows.
///
/// Falhas de WMI não são propagadas: uma consulta que rebenta é indistinguível, para quem
/// está à bancada, de um dispositivo que não está ligado — e o ecrã de verificação já diz
/// o que fazer nesse caso.
/// </summary>
public class WmiDeviceDetector : IDeviceDetector
{
    public Task<DispositivosDetectados> DetectarAsync(CancellationToken ct = default) =>
        Task.Run(() => new DispositivosDetectados(DetectarCH340(), DetectarUsbAsp()), ct);

    private static string? DetectarCH340()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%CH340%' OR Name LIKE '%CH34%'");

            foreach (var device in searcher.Get())
            {
                var name = device["Name"]?.ToString() ?? "";
                var match = Regex.Match(name, @"\(COM(\d+)\)");
                if (match.Success)
                    return $"COM{match.Groups[1].Value}";
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool DetectarUsbAsp()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%USBasp%' OR Name LIKE '%USBAsp%' " +
                "OR DeviceID LIKE '%VID_16C0&PID_05DC%'");

            return searcher.Get().Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
