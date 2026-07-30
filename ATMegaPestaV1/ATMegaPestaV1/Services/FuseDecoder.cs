using System.Globalization;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Características fixas de um microcontrolador (definidas pelo silício, não lidas do chip).
/// </summary>
public record McuInfo(string Nome, int FlashBytes, int EepromBytes, int SramBytes);

/// <summary>
/// Leitura dos fuse bits traduzida para linguagem humana.
/// Só descodifica — nunca escreve nada no chip.
/// </summary>
public record FusesDecodificados(
    string Relogio,
    bool Ckdiv8Activo,
    string BrownOut,
    bool SpiActivo,
    bool ResetActivo,
    bool EepromPreservada,
    int BootloaderBytes,
    bool BootRstActivo,
    int FlashAplicacao,
    string Bloqueio,
    bool LeituraLivre)
{
    public string DescricaoLow => Relogio;

    public string DescricaoHigh => BootRstActivo
        ? $"bootloader {FormatarBytes(BootloaderBytes)} · reset vector no boot"
        : "sem bootloader · reset vector na aplicação";

    public string DescricaoExtended => $"brown-out {BrownOut}";

    public string DescricaoLock => Bloqueio;

    public static string FormatarBytes(int bytes) =>
        bytes >= 1024 ? $"{bytes / 1024} KB" : $"{bytes} B";
}

/// <summary>
/// Descodifica os fuse bits do ATmega328P (datasheet, tabelas 8-1/8-3 e 28-5..28-8).
/// </summary>
public static class FuseDecoder
{
    /// <summary>
    /// Tabela de MCUs conhecidos. A app só programa m328p; acrescente aqui se mudar.
    /// </summary>
    private static readonly Dictionary<string, McuInfo> Conhecidos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ATmega328P"] = new("ATmega328P", 32768, 1024, 2048),
        ["ATmega328"]  = new("ATmega328",  32768, 1024, 2048)
    };

    public static McuInfo PorDefeito => Conhecidos["ATmega328P"];

    public static McuInfo Identificar(string? nomeAvrdude)
    {
        if (string.IsNullOrWhiteSpace(nomeAvrdude))
            return PorDefeito;

        foreach (var (nome, info) in Conhecidos)
            if (nomeAvrdude.Contains(nome, StringComparison.OrdinalIgnoreCase))
                return info;

        return PorDefeito;
    }

    public static FusesDecodificados? Descodificar(FusesResult fuses, McuInfo mcu)
    {
        if (!fuses.Success)
            return null;

        if (!TentarHex(fuses.Low, out var lfuse) ||
            !TentarHex(fuses.High, out var hfuse) ||
            !TentarHex(fuses.Extended, out var efuse))
            return null;

        // O lock byte é opcional — se não vier, não se inventa um valor.
        int? lockByte = TentarHex(fuses.Lock, out var lb) ? lb : null;

        var bootBytes = ((hfuse >> 1) & 0x03) switch
        {
            3 => 512,
            2 => 1024,
            1 => 2048,
            _ => 4096
        };

        var bootRst = (hfuse & 0x01) == 0;          // activo a zero
        var flashApp = bootRst ? mcu.FlashBytes - bootBytes : mcu.FlashBytes;

        return new FusesDecodificados(
            Relogio:          DescreverRelogio(lfuse),
            Ckdiv8Activo:     (lfuse & 0x80) == 0,  // activo a zero
            BrownOut:         DescreverBrownOut(efuse),
            SpiActivo:        (hfuse & 0x20) == 0,  // SPIEN activo a zero
            ResetActivo:      (hfuse & 0x80) != 0,  // RSTDISBL a 1 = reset normal
            EepromPreservada: (hfuse & 0x08) == 0,  // EESAVE activo a zero
            BootloaderBytes:  bootBytes,
            BootRstActivo:    bootRst,
            FlashAplicacao:   flashApp,
            Bloqueio:         DescreverBloqueio(lockByte),
            LeituraLivre:     lockByte is null || (lockByte.Value & 0x03) == 0x03);
    }

    private static string DescreverRelogio(int lfuse)
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
            _ => $"cristal externo {GamaCristal(cksel)}"
        };
    }

    private static string GamaCristal(int cksel) => ((cksel >> 1) & 0x07) switch
    {
        4 => "0.4–0.9 MHz",
        5 => "0.9–3.0 MHz",
        6 => "3.0–8.0 MHz",
        7 => "8–16 MHz",
        _ => "gama desconhecida"
    };

    private static string DescreverBrownOut(int efuse) => (efuse & 0x07) switch
    {
        7 => "desactivado",
        6 => "1.8 V",
        5 => "2.7 V",
        4 => "4.3 V",
        _ => "reservado"
    };

    private static string DescreverBloqueio(int? lockByte)
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

    private static bool TentarHex(string? valor, out int resultado)
    {
        resultado = 0;
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        var limpo = valor.Trim();
        if (limpo.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            limpo = limpo[2..];

        return int.TryParse(limpo, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out resultado);
    }
}
