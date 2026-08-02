using Microsoft.Extensions.Configuration;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Supplies the bench's services. It exists so the screens can ask for what they need
/// without repeating the service construction in every place: whoever needs to talk to
/// the master or to the target chip asks here and gets it with the port and the timeouts
/// already configured.
/// </summary>
public interface IServiceFactory
{
    /// <summary>The USB scan done at startup.</summary>
    IDeviceDetector CreateDetector();

    /// <summary>Link to the master firmware, on the port where the CH340 was found.</summary>
    IBusManager CreateBusManager(string comPort);

    /// <summary>ISP access to the target chip, through avrdude.</summary>
    IUsbAspService CreateUsbAspService();
}

/// <summary>
/// The bench: hardware plugged into the PC. This is the only implementation — the app always
/// reads the real devices and the real fuses, over WMI, serial port and avrdude.
/// </summary>
public class RealServiceFactory(int baudRate, int timeoutMs) : IServiceFactory
{
    public IDeviceDetector CreateDetector() => new WmiDeviceDetector();

    public IBusManager CreateBusManager(string comPort) => new BusManager(comPort, baudRate, timeoutMs);

    public IUsbAspService CreateUsbAspService() => new UsbAspService();
}

/// <summary>
/// The app's composition root: reads the serial link parameters from appsettings.json and
/// assembles the bench's services.
/// </summary>
public static class ServiceFactory
{
    public static IServiceFactory From(IConfiguration config)
    {
        var baudRate = config.GetValue<int>("BaudRate");
        var timeoutMs = config.GetValue<int>("SerialTimeoutMs");

        return new RealServiceFactory(
            baudRate > 0 ? baudRate : 9600,
            timeoutMs > 0 ? timeoutMs : 2000);
    }
}
