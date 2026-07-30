namespace ATMegaPestaV1.Services;

/// <summary>
/// Options in the serial menu of the Prog_Tester V1.2 firmware (ATMega2560 Pro Mini /
/// MASTER). The signature became option 1 and the rest shifted down one position.
/// </summary>
public static class MenuTester
{
    public const char Signature        = '1';
    public const char EnableUsbAsp     = '2';
    public const char EnableMegaMaster = '3';
    public const char IsolateBus       = '4';
    public const char TestSerial2      = '5';
    public const char SpiTest          = '6';

    /// <summary>Expected answer to option 1 (<c>exibirAssinatura</c>).</summary>
    public const string ExpectedSignature = "ATmega2560_Pro_ON";
}

/// <summary>
/// Result of reading the rig's signature (menu option 1).
/// </summary>
public record SignatureResult(bool Valid, string? Signature, string? Response);

/// <summary>
/// Represents the BusManager (master firmware on the ATMega2560 Pro Mini, over CH340).
/// Responsible for identifying the rig and for switching the ISP bus between USBAsp and
/// Mega, isolating it, or running the Serial2/SPI diagnostics.
/// </summary>
public interface IBusManager
{
    /// <summary>Sends "1" → returns the rig's signature, already validated.</summary>
    Task<SignatureResult> VerifySignatureAsync(CancellationToken ct = default);

    /// <summary>Sends "2" → puts the USBAsp on the ISP bus (USBASP_SPI).</summary>
    Task<string?> SelectUsbAspAsync(CancellationToken ct = default);

    /// <summary>Sends "3" → switches the bus over to the ATMega2560 (uC_MASTER_SPI).</summary>
    Task<string?> SwitchToMegaAsync(CancellationToken ct = default);

    /// <summary>Sends "4" → isolates the bus completely into Hi-Z (SPI_CANCEL_ACCESS).</summary>
    Task<string?> IsolateBusAsync(CancellationToken ct = default);

    /// <summary>Sends "5" → tests the Serial2 link to the Nano (TEST_SERIAL2_COM).</summary>
    Task<string?> TestSerial2Async(CancellationToken ct = default);

    /// <summary>Sends "6" → runs the full SPI test sequence (SPI_TEST).</summary>
    Task<string?> RunSpiTestAsync(CancellationToken ct = default);
}
