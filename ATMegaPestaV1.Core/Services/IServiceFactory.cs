using Microsoft.Extensions.Configuration;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Fornece os serviços da bancada. Existe para que os ecrãs peçam o que precisam sem
/// repetir a construção do serviço em cada sítio: quem precisa de falar com o master ou
/// com o chip-alvo pede aqui e recebe já com a porta e os tempos de espera configurados.
/// </summary>
public interface IServiceFactory
{
    /// <summary>Varredura ao USB do arranque.</summary>
    IDeviceDetector CriarDetector();

    /// <summary>Ligação ao firmware master, na porta onde o CH340 foi encontrado.</summary>
    IBusManager CriarBusManager(string portaCom);

    /// <summary>Acesso ISP ao chip-alvo, via avrdude.</summary>
    IUsbAspService CriarUsbAspService();
}

/// <summary>
/// A bancada: hardware ligado ao PC. É a única implementação — a app lê sempre os
/// dispositivos e os fuses reais, por WMI, porta série e avrdude.
/// </summary>
public class RealServiceFactory(int baudRate, int timeoutMs) : IServiceFactory
{
    public IDeviceDetector CriarDetector() => new WmiDeviceDetector();

    public IBusManager CriarBusManager(string portaCom) => new BusManager(portaCom, baudRate, timeoutMs);

    public IUsbAspService CriarUsbAspService() => new UsbAspService();
}

/// <summary>
/// Composição da app: lê do appsettings.json os parâmetros da ligação série e monta
/// os serviços da bancada.
/// </summary>
public static class ServiceFactory
{
    public static IServiceFactory APartirDe(IConfiguration config)
    {
        var baudRate = config.GetValue<int>("BaudRate");
        var timeoutMs = config.GetValue<int>("SerialTimeoutMs");

        return new RealServiceFactory(
            baudRate > 0 ? baudRate : 9600,
            timeoutMs > 0 ? timeoutMs : 2000);
    }
}
