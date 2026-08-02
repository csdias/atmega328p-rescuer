using System.Management;
using System.Text.RegularExpressions;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Real detection, over WMI, against the PnP devices Windows enumerates.
///
/// WMI failures are not propagated: a query that blows up is indistinguishable, to whoever
/// is standing at the bench, from a device that is not plugged in — and the verification
/// screen already says what to do in that case.
/// </summary>
public class WmiDeviceDetector : IDeviceDetector
{
    public Task<DetectedDevices> DetectAsync(CancellationToken ct = default) =>
        Task.Run(() => new DetectedDevices(DetectCh340(), DetectUsbAsp()), ct);

    private static string? DetectCh340()
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

    private static bool DetectUsbAsp()
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
