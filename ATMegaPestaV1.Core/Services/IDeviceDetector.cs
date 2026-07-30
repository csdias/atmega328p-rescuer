namespace ATMegaPestaV1.Services;

/// <summary>
/// O que a varredura ao USB encontrou na bancada. São os dois dispositivos que têm de
/// estar presentes antes de a app deixar avançar: o CH340 (porta série para o master) e
/// o USBAsp (programador ISP).
/// </summary>
/// <param name="PortaCH340">Porta onde o CH340 apareceu (ex.: "COM3"), ou null se não está lá.</param>
/// <param name="UsbAspLigado">O programador USBAsp está enumerado no USB.</param>
public record DispositivosDetectados(string? PortaCH340, bool UsbAspLigado)
{
    /// <summary>A bancada tem tudo o que é preciso para prosseguir.</summary>
    public bool Completa => PortaCH340 is not null && UsbAspLigado;

    /// <summary>Bancada onde não apareceu nada — o estado de arranque de uma varredura falhada.</summary>
    public static DispositivosDetectados Nenhum => new(null, false);
}

/// <summary>
/// Varre o USB à procura do hardware da bancada. É o primeiro passo do arranque: até isto
/// dar resultado não há porta série para falar com o master nem programador para chegar
/// ao chip-alvo.
/// </summary>
public interface IDeviceDetector
{
    Task<DispositivosDetectados> DetectarAsync(CancellationToken ct = default);
}
