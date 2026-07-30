using System.Globalization;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Fixed characteristics of a microcontroller (set by the silicon, not read off the chip).
/// </summary>
public record McuInfo(string Name, int FlashBytes, int EepromBytes, int SramBytes);

/// <summary>
/// A fuse bit reading translated into human language.
/// Decodes only — never writes anything to the chip.
///
/// The description strings stay in Portuguese: they are shown to the student on screen and
/// written into the backup sheet, so they are content, not code.
/// </summary>
public record DecodedFuses(
    string Clock,
    bool Ckdiv8Enabled,
    string BrownOut,
    bool SpiEnabled,
    bool ResetEnabled,
    bool EepromPreserved,
    int BootloaderBytes,
    bool BootRstEnabled,
    int ApplicationFlash,
    string LockLevel,
    bool ReadEnabled)
{
    public string LowDescription => Clock;

    public string HighDescription => BootRstEnabled
        ? $"bootloader {FormatBytes(BootloaderBytes)} · reset vector no boot"
        : "sem bootloader · reset vector na aplicação";

    public string ExtendedDescription => $"brown-out {BrownOut}";

    public string LockDescription => LockLevel;

    public static string FormatBytes(int bytes) =>
        bytes >= 1024 ? $"{bytes / 1024} KB" : $"{bytes} B";
}

/// <summary>
/// Decodes the ATmega328P fuse bits (datasheet, tables 8-1/8-3 and 28-5..28-8).
/// </summary>
public static class FuseDecoder
{
    /// <summary>
    /// Table of known MCUs. The app only programs m328p; add here if that changes.
    /// </summary>
    private static readonly Dictionary<string, McuInfo> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ATmega328P"] = new("ATmega328P", 32768, 1024, 2048),
        ["ATmega328"]  = new("ATmega328",  32768, 1024, 2048)
    };

    public static McuInfo Default => Known["ATmega328P"];

    public static McuInfo Identify(string? avrdudeName)
    {
        if (string.IsNullOrWhiteSpace(avrdudeName))
            return Default;

        foreach (var (name, info) in Known)
            if (avrdudeName.Contains(name, StringComparison.OrdinalIgnoreCase))
                return info;

        return Default;
    }

    public static DecodedFuses? Decode(FusesResult fuses, McuInfo mcu)
    {
        if (!fuses.Success)
            return null;

        if (!TryHex(fuses.Low, out var lfuse) ||
            !TryHex(fuses.High, out var hfuse) ||
            !TryHex(fuses.Extended, out var efuse))
            return null;

        // The lock byte is optional — if it does not come, no value is invented.
        int? lockByte = TryHex(fuses.Lock, out var lb) ? lb : null;

        var bootBytes = ((hfuse >> 1) & 0x03) switch
        {
            3 => 512,
            2 => 1024,
            1 => 2048,
            _ => 4096
        };

        var bootRst = (hfuse & 0x01) == 0;          // active low
        var flashApp = bootRst ? mcu.FlashBytes - bootBytes : mcu.FlashBytes;

        return new DecodedFuses(
            Clock:            DescribeClock(lfuse),
            Ckdiv8Enabled:    (lfuse & 0x80) == 0,  // active low
            BrownOut:         DescribeBrownOut(efuse),
            SpiEnabled:       (hfuse & 0x20) == 0,  // SPIEN active low
            ResetEnabled:     (hfuse & 0x80) != 0,  // RSTDISBL at 1 = normal reset
            EepromPreserved:  (hfuse & 0x08) == 0,  // EESAVE active low
            BootloaderBytes:  bootBytes,
            BootRstEnabled:   bootRst,
            ApplicationFlash: flashApp,
            LockLevel:        DescribeLock(lockByte),
            ReadEnabled:      lockByte is null || (lockByte.Value & 0x03) == 0x03);
    }

    private static string DescribeClock(int lfuse)
    {
        var cksel = lfuse & 0x0F;

        return cksel switch
        {
            0 => "clock externo",
            1 => "reservado",
            2 => "RC interno 8 MHz",
            3 => "RC interno 128 kHz",
            4 or 5 => "cristal 32.768 kHz",
            6 or 7 => "cristal full swing",
            _ => $"cristal externo {CrystalRange(cksel)}"
        };
    }

    private static string CrystalRange(int cksel) => ((cksel >> 1) & 0x07) switch
    {
        4 => "0.4–0.9 MHz",
        5 => "0.9–3.0 MHz",
        6 => "3.0–8.0 MHz",
        7 => "8–16 MHz",
        _ => "gama desconhecida"
    };

    private static string DescribeBrownOut(int efuse) => (efuse & 0x07) switch
    {
        7 => "desactivado",
        6 => "1.8 V",
        5 => "2.7 V",
        4 => "4.3 V",
        _ => "reservado"
    };

    private static string DescribeLock(int? lockByte)
    {
        if (lockByte is null)
            return "não lido";

        return (lockByte.Value & 0x03) switch
        {
            3 => "sem bloqueio — leitura livre",
            2 => "modo 2 — programação bloqueada",
            0 => "modo 3 — programação e verificação bloqueadas",
            _ => $"invulgar (LB={lockByte.Value & 0x03})"
        };
    }

    private static bool TryHex(string? value, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var cleaned = value.Trim();
        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[2..];

        return int.TryParse(cleaned, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }
}
