namespace ATMegaPestaV1.Services;

/// <summary>
/// What the USB scan found on the rig. These are the two devices that have to be present
/// before the app lets you move on: the CH340 (serial port to the master) and the USBAsp
/// (ISP programmer).
/// </summary>
/// <param name="Ch340Port">Port where the CH340 showed up (e.g. "COM3"), or null if absent.</param>
/// <param name="UsbAspConnected">The USBAsp programmer is enumerated on the USB.</param>
public record DetectedDevices(string? Ch340Port, bool UsbAspConnected)
{
    /// <summary>The rig has everything it needs to proceed.</summary>
    public bool Complete => Ch340Port is not null && UsbAspConnected;

    /// <summary>A rig where nothing showed up — the starting state of a failed scan.</summary>
    public static DetectedDevices None => new(null, false);
}

/// <summary>
/// Scans the USB for the rig's hardware. This is the first step at startup: until it
/// comes back with something there is no serial port to talk to the master and no
/// programmer to reach the target chip.
/// </summary>
public interface IDeviceDetector
{
    Task<DetectedDevices> DetectAsync(CancellationToken ct = default);
}
